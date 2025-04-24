using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class JsonLayout
{
    public Vector2 multiplier;
    public List<JsonLayoutSlot> slots;
    public JsonLayoutPile drawPile;
    public JsonLayoutPile discardPile;
    public JsonLayoutPile targetPile; // Added for Golf Solitaire
}

[System.Serializable]
public class JsonLayoutSlot : ISerializationCallbackReceiver
{
    public int id;
    public int x;
    public int y;
    public bool faceUp;
    public string layer;
    public string hiddenByString;

    [System.NonSerialized]
    public List<int> hiddenBy;

    public void OnAfterDeserialize()
    {
        hiddenBy = new List<int>();
        if (string.IsNullOrEmpty(hiddenByString)) return;

        string[] bits = hiddenByString.Split(',');
        foreach (string bit in bits)
            hiddenBy.Add(int.Parse(bit));
    }

    public void OnBeforeSerialize() { }
}

[System.Serializable]
public class JsonLayoutPile
{
    public int x;
    public int y;
    public string layer;
    public float xStagger;
}

public class JsonParseLayout : MonoBehaviour
{
    public static JsonParseLayout S { get; private set; }
    public TextAsset jsonLayoutFile;
    public JsonLayout layout;

    void Awake()
    {
        S = this;
        layout = JsonUtility.FromJson<JsonLayout>(jsonLayoutFile.text);
    }
}