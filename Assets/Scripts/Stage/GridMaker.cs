// Reference : https://www.redblobgames.com/grids/hexagons/

using System.Collections.Generic;
using UnityEngine;

public class GridMaker : MonoBehaviour
{
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

                Vector3 spawnPos = new Vector3(colIdx * GridManager.Instance.XOffset + rowXOffset, rowIdx * GridManager.Instance.YOffset, 0);

                GridCell currentCell = new GridCell();
                if (currentCellData.CellType != GridCellType.EMPTY)
                {
                    // 화면 가운데로 정렬하기 위해 포지션 수집
                    GridManager.Instance.MinBubbleXPos = Mathf.Min(GridManager.Instance.MinBubbleXPos, spawnPos.x);
                    GridManager.Instance.MaxBubbleXPos = Mathf.Max(GridManager.Instance.MaxBubbleXPos, spawnPos.x);
                    GridManager.Instance.MinBubbleYPos = Mathf.Min(GridManager.Instance.MinBubbleYPos, spawnPos.y);
                 
                    currentCell.CellGO = StageManager.Instance.SpawnOnGridBubble(spawnPos, currentCellData.CellType, transform);
                    currentCell.CellPosition = spawnPos;
                    currentCell.CellType = currentCellData.CellType;

                    Bubble bubble = currentCell.CellGO.GetComponent<Bubble>();
                    bubble.colIdx = colIdx;
                    bubble.rowIdx = rowIdx;
                }
                else currentCell.CellPosition = spawnPos;

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
}
