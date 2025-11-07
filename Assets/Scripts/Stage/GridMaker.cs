// Reference : https://www.redblobgames.com/grids/hexagons/

using System;
using System.Collections.Generic;
using UnityEngine;

public class GridMaker : MonoBehaviour
{
    private List<Vector2Int> m_SpawnPositions = new List<Vector2Int>();
    private List<List<Tuple<Vector2Int, Vector2Int>>> m_Paths = new List<List<Tuple<Vector2Int, Vector2Int>>>();

    public List<Vector2Int> GetSpawnPositions() => m_SpawnPositions;
    public List<List<Tuple<Vector2Int, Vector2Int>>> GetPaths() => m_Paths;

    public void GenerateGrid(List<Row> grid)
    {
        Logger.Log($"{GetType()}::GenerateGrid(List<Row> grid)");

        List<Row> gridData = StageManager.Instance.CurrentStageStat.GridData;
        if (gridData == null || gridData.Count == 0)
            return;

        for (int rowIdx = 0; rowIdx < gridData.Count; rowIdx++)
        {
            float rowXOffset = (rowIdx % 2 != 0) ? 0f : GridManager.Instance.XOffset * 0.5f; // 짝수 행이면 오른쪽으로 살짝 밀기 (even-r offset)

            Row currentRow = new Row();
            currentRow.Columns = new List<GridCell>();
            for (int colIdx = 0; colIdx < gridData[rowIdx].Columns.Count; colIdx++)
            {
                GridCell currentCellData = gridData[rowIdx].Columns[colIdx];

                Vector3 spawnPos = new Vector3(colIdx * GridManager.Instance.XOffset + rowXOffset, (gridData.Count - 1 - rowIdx) * GridManager.Instance.YOffset, 0);

                GridCell currentCell = new GridCell();

                if (currentCellData.CellType != GridCellType.EMPTY)
                {
                    // 화면 가운데로 정렬하기 위해 포지션 수집
                    GridManager.Instance.MinBubbleXPos = Mathf.Min(GridManager.Instance.MinBubbleXPos, spawnPos.x);
                    GridManager.Instance.MaxBubbleXPos = Mathf.Max(GridManager.Instance.MaxBubbleXPos, spawnPos.x);

                    Logger.Log($"{spawnPos}, {currentCellData.CellType}, {transform}");

                    currentCell.CellGO = StageManager.Instance.SpawnOnGridBubble(spawnPos, currentCellData.CellType, transform);
                    currentCell.CellPosition = spawnPos;
                    currentCell.CellType = currentCellData.CellType;

                    Bubble bubble = currentCell.CellGO.GetComponent<Bubble>();
                    bubble.colIdx = colIdx;
                    bubble.rowIdx = rowIdx;

                    if (currentCellData.CellType == GridCellType.SKELETON)
                        m_SpawnPositions.Add(new Vector2Int(rowIdx, colIdx));
                }
                else currentCell.CellPosition = spawnPos;

                GridManager.Instance.MinBubbleYPos = Mathf.Min(GridManager.Instance.MinBubbleYPos, spawnPos.y);
                currentRow.Columns.Add(currentCell);
            }
            grid.Add(currentRow);
        }
    }

    public void GenerateNewRow(List<Row> grid)
    {
        if (grid.Count == 0) return;

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
