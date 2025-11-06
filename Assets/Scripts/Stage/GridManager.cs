using System.Collections.Generic;
using UnityEngine;

public class GridManager : SingletonBehaviour<GridManager>
{
    private GridMaker m_GridMaker;
    private List<Row> m_Grid = new List<Row>();

    [HideInInspector] public float XOffset = 0.5f;
    [HideInInspector] public float YOffset = 0.5f;

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
    }

    public void GenerateGrid()
    {
        m_GridMaker.GenerateGrid(m_Grid);
    }

    public void AttachToGrid(GameObject shootingBubbleGO)
    {
        // TODO : 모든 셀을 확인하지 않고 처리할 수 있도록 로직 변경 필요

        if (!shootingBubbleGO.activeSelf)
            return;

        bool isAddedNewRow = false;
        if (shootingBubbleGO.transform.position.y < MinBubbleYPos)
        {
            m_GridMaker.GenerateNewRow(m_Grid);
            isAddedNewRow = true;
        }
            
        
        float minDistance = float.MaxValue;
        Vector2Int targetCellIdx = Vector2Int.zero;

        for (int rowIdx = 0; rowIdx < m_Grid.Count; rowIdx++)
        {
            for (int colIdx = 0; colIdx < m_Grid[rowIdx].Columns.Count; colIdx++)
            {
                if (m_Grid[rowIdx].Columns[colIdx].CellType != GridCellType.EMPTY) continue;

                Vector3 currentCellPosition = m_Grid[rowIdx].Columns[colIdx].CellPosition;
                float dist = Vector3.Distance(currentCellPosition, shootingBubbleGO.transform.position);
                
                if (minDistance > dist)
                {
                    minDistance = Mathf.Min(minDistance, dist);
                    targetCellIdx = new Vector2Int(rowIdx, colIdx);
                }
            }
        }

        BubbleColor bubbleColor = shootingBubbleGO.GetComponent<Bubble>().BubbleColor;
        StageManager.Instance.ReturnToPoolBubbleGO(shootingBubbleGO);

        Vector3 position = m_Grid[targetCellIdx.x].Columns[targetCellIdx.y].CellPosition;
        GridCell newCell = new GridCell();
        newCell.CellGO = StageManager.Instance.SpawnOnGridBubble(position, GridCellType.BUBBLE, transform, bubbleColor);
        newCell.CellPosition = position;
        newCell.CellType = GridCellType.BUBBLE;

        Bubble bubble = newCell.CellGO.GetComponent<Bubble>();
        bubble.rowIdx = targetCellIdx.x;
        bubble.colIdx = targetCellIdx.y;

        m_Grid[targetCellIdx.x].Columns[targetCellIdx.y] = newCell;

        if (isAddedNewRow)
            StageManager.Instance.SetCameraAndShooterPos();
    }
}
