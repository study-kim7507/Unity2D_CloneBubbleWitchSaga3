using System.Collections.Generic;
using UnityEngine;

public class GridManager : SingletonBehaviour<GridManager>
{
    private GridMaker m_GridMaker;
    private List<Row> m_Grid = new List<Row>();

    [SerializeField] private GameObject m_GlowBubblePrefab;
    private GameObject m_GlowBubble;

    private int m_TargetRowIdx = 0;
    private int m_TargetColIdx = 0;

    [HideInInspector] public float XOffset = 0.45f;
    [HideInInspector] public float YOffset = 0.45f;

    [HideInInspector] public float MinBubbleXPos = float.MaxValue;
    [HideInInspector] public float MaxBubbleXPos = float.MinValue;
    [HideInInspector] public float MinBubbleYPos = float.MaxValue;
    [HideInInspector] public float CenterXPos => (MinBubbleXPos + MaxBubbleXPos) / 2.0f;

    private void Awake()
    {
        m_IsDestroyOnLoad = true;           // 씬 전환 시 삭제
        Init();
    }

    protected override void Init()
    {
        Logger.Log($"{GetType()}::Initialize()");
        base.Init();

        m_GridMaker = GetComponent<GridMaker>();

        m_GlowBubble = Instantiate(m_GlowBubblePrefab, Vector3.zero, Quaternion.identity, transform);
        m_GlowBubble.SetActive(false);
    }

    public void GenerateGrid()
    {
        m_GridMaker.GenerateGrid(m_Grid);
    }

    public void AttachToGrid(GameObject shootingBubbleGO)
    {
        GridCell targetCell = m_Grid[m_TargetRowIdx].Columns[m_TargetColIdx];

        shootingBubbleGO.transform.SetParent(transform);
        shootingBubbleGO.transform.position = targetCell.CellPosition;
        shootingBubbleGO.tag = "OnGridBubble";

        Bubble bubble = shootingBubbleGO.GetComponent<Bubble>();
        bubble.colIdx = m_TargetColIdx;
        bubble.rowIdx = m_TargetRowIdx;

        targetCell.CellGO = shootingBubbleGO;
        targetCell.CellType = GridCellType.BUBBLE;

        if (m_TargetRowIdx == m_Grid.Count - 1)
        {
            m_GridMaker.GenerateNewRow(m_Grid);
            StageManager.Instance.SetCameraAndShooterPos();
        }
    }

    public Vector2 SpawnGlowBubble(int rowIdx, int colIdx, Vector3 hitPoint)
    {
        int[, ,] idxOffset = new int[2, 6, 2]
        {
            { { 0, 1 }, { 1, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 }, { -1, 1 } },
            { { 0, 1 }, { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, -1 }, { -1, 0 } }
        };


        float minDist = float.MaxValue; 
        for (int i = 0; i < 6; i++)
        {
            int curRowIdx = rowIdx + idxOffset[rowIdx % 2, i, 0];
            int curColIdx = colIdx + idxOffset[rowIdx % 2, i, 1];

            if (curRowIdx < 0 || curColIdx < 0) continue;
            if (curRowIdx >= m_Grid.Count || curColIdx >= m_Grid[curRowIdx].Columns.Count) continue;

            if (m_Grid[curRowIdx].Columns[curColIdx].CellType != GridCellType.EMPTY) continue;

            if (minDist > Vector3.Distance(m_Grid[curRowIdx].Columns[curColIdx].CellPosition, hitPoint))
            {
                minDist = Vector3.Distance(m_Grid[curRowIdx].Columns[curColIdx].CellPosition, hitPoint);
                m_TargetRowIdx = curRowIdx;
                m_TargetColIdx = curColIdx;
            }
        }

        Vector3 spawnPosition = m_Grid[m_TargetRowIdx].Columns[m_TargetColIdx].CellPosition;
        m_GlowBubble.transform.position = spawnPosition;
        m_GlowBubble.SetActive(true);

        return new Vector2(spawnPosition.x, spawnPosition.y);
    }

    public void DespawnGlowBubble()
    {
        m_GlowBubble.SetActive(false);
    }
}
