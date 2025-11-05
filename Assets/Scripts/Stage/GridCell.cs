using UnityEngine;

[System.Serializable]
public enum GridCellType
{
    EMPTY = 0,
    BUBBLE,
    BUBBLE_WILDCARD,
    BUBBLE_SKELTON,

    // 그 외 특수 버블, 아이템 등
}

[System.Serializable]
public class GridCell
{
    public GameObject CellGO;
    public Vector3 CellPosition;
    public GridCellType CellType;

    public GridCell()
    {
        CellGO = null;
        CellPosition = Vector3.zero;
        CellType = GridCellType.EMPTY;
    }
}
