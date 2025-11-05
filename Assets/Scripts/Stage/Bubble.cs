using UnityEngine;

[System.Serializable]
public enum BubbleColor
{
    NONE = -1,

    RED = 0,
    YELLOW,
    BLUE,
}

public class Bubble : OnGridObject
{
    public BubbleColor BubbleColor;
}
