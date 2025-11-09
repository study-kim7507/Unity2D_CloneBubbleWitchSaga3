// 해당 클래스는 스테이지 시작 후 초기 그리드 생성과 새로운 열의 생성을 담당
// Reference : https://www.redblobgames.com/grids/hexagons/

using System;
using System.Collections.Generic;
using UnityEngine;

public class GridMaker : MonoBehaviour
{
    // 새로 버블이 스폰 될 위치, 경로를 저장하기 위함
    private List<Vector2Int> m_SpawnPositions = new List<Vector2Int>();                                             
    private List<List<Tuple<Vector2Int, Vector2Int>>> m_Paths = new List<List<Tuple<Vector2Int, Vector2Int>>>();
    [HideInInspector] public List<Vector2Int> GetSpawnPositions() => m_SpawnPositions;
    [HideInInspector] public List<List<Tuple<Vector2Int, Vector2Int>>> GetPaths() => m_Paths;

    public void GenerateGrid(List<Row> grid)
    {
        List<Row> gridData = StageManager.Instance.CurrentStageStat.GridData;
        if (gridData == null || gridData.Count == 0)
            return;

        for (int rowIdx = 0; rowIdx < gridData.Count; rowIdx++)
        {
            // 짝수 행이면 오른쪽으로 살짝 밀기 (even-r offset)
            float rowXOffset = (rowIdx % 2 != 0) ? 0f : GridManager.Instance.XOffset * 0.5f; 

            Row currentRow = new Row();
            currentRow.Columns = new List<GridCell>();
            for (int colIdx = 0; colIdx < gridData[rowIdx].Columns.Count; colIdx++)
            {
                GridCell currentCellData = gridData[rowIdx].Columns[colIdx];                    // 스크립터블 오브젝트에서 데이터를 읽어옴

                Vector3 spawnPos = new Vector3(colIdx * GridManager.Instance.XOffset + rowXOffset, (gridData.Count - 1 - rowIdx) * GridManager.Instance.YOffset, 0);

                GridCell currentCell = new GridCell();                                          // 런타임에 사용한 Grid를 채울 셀을 생성
                if (currentCellData.CellType != GridCellType.EMPTY)
                {
                    // 그리드를 화면 가운데로 정렬하기 위해 포지션 수집
                    GridManager.Instance.MinBubbleXPos = Mathf.Min(GridManager.Instance.MinBubbleXPos, spawnPos.x);
                    GridManager.Instance.MaxBubbleXPos = Mathf.Max(GridManager.Instance.MaxBubbleXPos, spawnPos.x);

                    if (currentCellData.CellType == GridCellType.BUBBLE || currentCellData.CellType == GridCellType.BUBBLE_SPAWNER)
                        currentCell.CellGO = StageManager.Instance.BarrowFromPoolOnGridBubble(spawnPos, currentCellData.CellType, transform);
                    else if (currentCellData.CellType == GridCellType.BOSS)
                        currentCell.CellGO = StageManager.Instance.SpawnBoss(spawnPos);
                    currentCell.CellPosition = spawnPos;
                    currentCell.CellType = currentCellData.CellType;

                    if (currentCellData.CellType == GridCellType.BUBBLE || currentCellData.CellType == GridCellType.BUBBLE_SPAWNER)
                    {
                        Bubble bubble = currentCell.CellGO.GetComponent<Bubble>();
                        bubble.ColIdx = colIdx;
                        bubble.RowIdx = rowIdx;
                    }

                    if (currentCellData.CellType == GridCellType.BUBBLE_SPAWNER)
                        m_SpawnPositions.Add(new Vector2Int(rowIdx, colIdx));
                }
                else currentCell.CellPosition = spawnPos;

                // 새로운 열의 생성, 카메라, 슈터 위치 조정을 위해 가장 낮은 위치의 버블의 좌표를 저장 
                GridManager.Instance.MinBubbleYPos = Mathf.Min(GridManager.Instance.MinBubbleYPos, spawnPos.y);
                currentRow.Columns.Add(currentCell);
            }
            grid.Add(currentRow);
        }
    }

    // 가장 마지막 열 아래에 버블이 놓이게 되는 경우 새로운 열을 생성해주어야 함
    public void GenerateNewRow(List<Row> grid)
    {
        int columnCount = grid[0].Columns.Count;
        Row newRow = new Row { Columns = new List<GridCell>() };

        int newRowIdx = grid.Count;
        float rowXOffset = (newRowIdx % 2 != 0) ? 0f : GridManager.Instance.XOffset * 0.5f;
        float curYPos = GridManager.Instance.MinBubbleYPos - GridManager.Instance.YOffset;

        for (int colIdx = 0; colIdx < columnCount; colIdx++)
        {
            Vector3 spawnPos = new Vector3(colIdx * GridManager.Instance.XOffset + rowXOffset, curYPos, 0);
            newRow.Columns.Add(new GridCell
            {
                CellGO = null,
                CellType = GridCellType.EMPTY,
                CellPosition = spawnPos
            });
        }

        grid.Add(newRow);
        GridManager.Instance.MinBubbleYPos = curYPos;
    }

    // 새로운 버블이 생성될 때, 어떤 경로를 따라 생성되어야 할 지 결정하기 위해 해당 경로를 생성하는 함수
    public void GeneratePath(List<Row> grid)
    {
        int[,,] idxOffset = new int[2, 4, 2]
        {
            { { 1, 1 }, { 1, 0 }, { 0, -1 }, { 0, 1 } },
            { { 1, 0 }, { 1, -1 }, { 0, -1 }, { 0, 1 } }
        };

        foreach (Vector2Int spawnPosition in m_SpawnPositions)
        {
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            queue.Enqueue(spawnPosition);
            visited.Add(spawnPosition);

            // 현재 스폰 포인트에서 출발하는 path
            List<Tuple<Vector2Int, Vector2Int>> path = new List<Tuple<Vector2Int, Vector2Int>>();

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                for (int i = 0; i < 4; i++)
                {
                    int nxtRowIdx = current.x + idxOffset[current.x % 2, i, 0];
                    int nxtColIdx = current.y + idxOffset[current.x % 2, i, 1];

                    if (nxtRowIdx < 0 || nxtColIdx < 0) continue;
                    if (nxtRowIdx >= grid.Count || nxtColIdx >= grid[nxtRowIdx].Columns.Count) continue;

                    Vector2Int next = new Vector2Int(nxtRowIdx, nxtColIdx);
                    if (visited.Contains(next)) continue;

                    if (grid[nxtRowIdx].Columns[nxtColIdx].CellType == GridCellType.BUBBLE)
                    {
                        path.Add(new Tuple<Vector2Int, Vector2Int>(current, next));

                        queue.Enqueue(next);
                        visited.Add(next);
                        break; 
                    }
                }
            }

            if (path.Count > 0)
                m_Paths.Add(path);
        }
    }
}
