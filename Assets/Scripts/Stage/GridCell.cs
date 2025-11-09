using UnityEngine;

[System.Serializable]
public enum GridCellType
{
    EMPTY = 0,
    BUBBLE,
    BUBBLE_SPAWNER,

    BOSS,

    // 그 외 특수 버블, 아이템 등
}

// GridCell은 스크립터블 오브젝트에서 데이터를 설정할 때,
// 런타임에 사용될 실제 그리드를 생성할 때 동일하게 사용함.
[System.Serializable]
public class GridCell
{
    public GameObject CellGO;                   // 현재 셀이 담고 있는 오브젝트 (버블, 스포너, 보스 등..)
    public Vector3 CellPosition;                // 현재 셀의 위치
    public GridCellType CellType;               // 현재 셀의 타입 (빈 셀, 버블, 스포너, 보스 ...)

    public GridCell()
    {
        CellGO = null;
        CellPosition = Vector3.zero;
        CellType = GridCellType.EMPTY;
    }
}
