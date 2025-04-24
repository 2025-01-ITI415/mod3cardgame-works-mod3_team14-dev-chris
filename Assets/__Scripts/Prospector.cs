using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Deck))]
[RequireComponent(typeof(JsonParseLayout))]
public class Prospector : MonoBehaviour
{
    private static Prospector S;
    [Header("Dynamic")]
    public List<CardProspector> drawPile;
    public List<CardProspector> discardPile;
    public List<CardProspector> mine;
    public CardProspector target;
    private Transform layoutAnchor;
    private Deck deck;
    private JsonLayout jsonLayout;
    private Dictionary<int, CardProspector> mineIdToCardDict;

    // Track the highest active row in each column
    private Dictionary<int, int> highestActiveRowByColumn = new Dictionary<int, int>();
    // Number of columns in the layout
    private int numColumns = 7;

    void Start()
    {
        if (S != null) Debug.LogError("Attempted to set S more than once!");
        S = this;
        // Initialize collections to avoid null reference exceptions
        drawPile = new List<CardProspector>();
        discardPile = new List<CardProspector>();
        mine = new List<CardProspector>();

        jsonLayout = GetComponent<JsonParseLayout>().layout;
        deck = GetComponent<Deck>();
        deck.InitDeck();
        Deck.Shuffle(ref deck.cards);
        drawPile = ConvertCardsToCardProspectors(deck.cards);
        LayoutMine();
        UpdateActiveRows();
        MoveToTarget(Draw());
        UpdateDrawPile();
    }

    List<CardProspector> ConvertCardsToCardProspectors(List<Card> listCard)
    {
        List<CardProspector> listCP = new List<CardProspector>();
        foreach (Card card in listCard)
        {
            listCP.Add(card as CardProspector);
        }
        return listCP;
    }

    CardProspector Draw()
    {
        if (drawPile.Count == 0) return null;

        CardProspector cp = drawPile[0];
        drawPile.RemoveAt(0);
        return cp;
    }

    void LayoutMine()
    {
        if (layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
        }

        mineIdToCardDict = new Dictionary<int, CardProspector>();
        foreach (JsonLayoutSlot slot in jsonLayout.slots)
        {
            CardProspector cp = Draw();
            if (cp == null)
            {
                Debug.LogError("Ran out of cards while laying out mine");
                return;
            }

            cp.faceUp = slot.faceUp;
            cp.transform.SetParent(layoutAnchor);

            // Extract row number from the layer name (e.g., "Row3" -> 3)
            if (slot.layer.StartsWith("Row"))
            {
                string rowStr = slot.layer.Substring(3);
                if (int.TryParse(rowStr, out int rowNum))
                {
                    cp.row = rowNum;
                }
            }

            // Calculate the column index based on the x-position
            int column = (int)((slot.x + 9) / 3); // Maps -9 to 0, -6 to 1, etc.

            // For z-ordering, use 5-row to invert the row numbers (assuming max row is 4)
            // This makes Row4 have the smallest z value, which means it's on top visually
            int z = cp.row;
            cp.SetLocalPos(new Vector3(
                jsonLayout.multiplier.x * slot.x,
                jsonLayout.multiplier.y * slot.y,
                -z));

            cp.layoutID = slot.id;
            cp.layoutSlot = slot;
            cp.state = eCardState.mine;
            cp.SetSpriteSortingLayer(slot.layer);
            mine.Add(cp);
            mineIdToCardDict.Add(slot.id, cp);
        }
    }

    // Update which rows are active in each column
    void UpdateActiveRows()
    {
        highestActiveRowByColumn.Clear();

        // In the new order, the highest row number (Row4) is actually the top row
        // So we need to find the *highest* row number in each column
        foreach (CardProspector cp in mine)
        {
            // Skip cards not in the mine
            if (cp.state != eCardState.mine) continue;

            // Calculate column from x position
            int column = (int)((cp.layoutSlot.x + 9) / 3);

            if (!highestActiveRowByColumn.ContainsKey(column) ||
                cp.row > highestActiveRowByColumn[column])  // Changed < to > to find highest row number
            {
                highestActiveRowByColumn[column] = cp.row;
            }
        }
    }

    // Check if a card is playable (i.e., it's in the top row of its column)
    bool IsPlayable(CardProspector cp)
    {
        // Calculate column from x position
        int column = (int)((cp.layoutSlot.x + 9) / 3);

        // Card is playable if it's in the highest row number of its column
        return highestActiveRowByColumn.ContainsKey(column) &&
               cp.row == highestActiveRowByColumn[column];  // Now comparing with highest row number
    }

    void MoveToDiscard(CardProspector cp)
    {
        cp.state = eCardState.discard;
        discardPile.Add(cp);
        cp.transform.SetParent(layoutAnchor);
        cp.SetLocalPos(new Vector3(
            jsonLayout.multiplier.x * jsonLayout.discardPile.x,
            jsonLayout.multiplier.y * jsonLayout.discardPile.y,
            0));
        cp.faceUp = true;
        cp.SetSpriteSortingLayer(jsonLayout.discardPile.layer);
        cp.SetSortingOrder(-200 + (discardPile.Count * 3));
    }

    void MoveToTarget(CardProspector cp)
    {
        if (cp == null) return;

        if (target != null)
            MoveToDiscard(target);

        target = cp;
        cp.state = eCardState.target;
        cp.transform.SetParent(layoutAnchor);
        cp.SetLocalPos(new Vector3(
            jsonLayout.multiplier.x * jsonLayout.targetPile.x,
            jsonLayout.multiplier.y * jsonLayout.targetPile.y,
            0));
        cp.faceUp = true;
        cp.SetSpriteSortingLayer("Target");
        cp.SetSortingOrder(0);
    }

    void UpdateDrawPile()
    {
        for (int i = 0; i < drawPile.Count; i++)
        {
            CardProspector cp = drawPile[i];
            cp.transform.SetParent(layoutAnchor);
            Vector3 cpPos = new Vector3(
                jsonLayout.multiplier.x * jsonLayout.drawPile.x + (jsonLayout.drawPile.xStagger * i),
                jsonLayout.multiplier.y * jsonLayout.drawPile.y,
                0.1f * i);
            cp.SetLocalPos(cpPos);
            cp.faceUp = false;
            cp.state = eCardState.drawpile;
            cp.SetSpriteSortingLayer(jsonLayout.drawPile.layer);
            cp.SetSortingOrder(-10 * i);
        }
    }

    void CheckForGameOver()
    {
        if (mine.Count == 0)
        {
            Debug.Log("You Win!");
            return;
        }

        if (drawPile.Count == 0)
        {
            bool hasMove = false;
            foreach (CardProspector cp in mine)
            {
                if (cp.state == eCardState.mine && IsPlayable(cp) && cp.AdjacentTo(target))
                {
                    hasMove = true;
                    break;
                }
            }
            if (!hasMove)
                Debug.Log("Game Over - No Moves Left");
        }
    }

    static public void CARD_CLICKED(CardProspector cp)
    {
        switch (cp.state)
        {
            case eCardState.mine:
                // Only allow cards in the top row to be played
                if (S.IsPlayable(cp) && cp.AdjacentTo(S.target))
                {
                    S.mine.Remove(cp);
                    S.MoveToTarget(cp);
                    // Update active rows after removing a card
                    S.UpdateActiveRows();

                    if (S.mine.Count == 0)
                        Debug.Log("You Win!");
                }
                else
                {
                    // For debugging - could remove in production
                    Debug.Log("Card not playable: " + cp.name);
                }
                break;

            case eCardState.drawpile:
                // Allow drawing cards at any time, regardless of valid moves
                if (S.drawPile.Count > 0)
                {
                    CardProspector drawnCard = S.Draw();
                    if (drawnCard != null)
                    {
                        S.MoveToTarget(drawnCard);
                        S.UpdateDrawPile();
                    }
                }
                break;
        }
        S.CheckForGameOver();
    }
}