// Reference : https://www.redblobgames.com/grids/hexagons/

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Row
{
    public List<GridCell> Columns = new List<GridCell>();
}

[CreateAssetMenu(fileName = "StageStat", menuName = "Scriptable Objects/StageStat")]
public class StageStat : ScriptableObject
{
    [Header("현재 스테이지의 그리드 생성을 위한 정보")]
    public List<Row> GridData = new List<Row>();

    [Header("스테이지 정보")]
    public float RemaingBossHealth;                         // 남은 보스 체력
    public int RemainingBubbleAmount;                       // 플레이어가 쏠 수 있는 남은 버블 수
}
