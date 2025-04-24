using System.Collections.Generic;
using UnityEngine;
public enum eCardState { drawpile, mine, target, discard }
public class CardProspector : Card
{
    [Header("Dynamic: CardProspector")]
    public eCardState state = eCardState.drawpile;
    public int layoutID;
    public JsonLayoutSlot layoutSlot;

    // Track which row this card is in
    public int row = -1;

    public override void OnMouseUpAsButton()
    {
        Prospector.CARD_CLICKED(this);
        base.OnMouseUpAsButton();
    }

    public bool AdjacentTo(Card otherCard)
    {
        return base.AdjacentTo(otherCard, true);
    }
}