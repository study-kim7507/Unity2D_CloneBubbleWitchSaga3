using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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
        m_GridMaker.GeneratePath(m_Grid);

        StageManager.Instance.UpdateCameraAndShooterPos(m_Grid, true);
    }

    public void AttachToGrid(GameObject shootingBubbleGO)
    {
        StartCoroutine(AttachToGridCo(shootingBubbleGO));
    }
    
    private IEnumerator AttachToGridCo(GameObject shootingBubbleGO)
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
            m_GridMaker.GenerateNewRow(m_Grid);

        PopBubble(m_TargetRowIdx, m_TargetColIdx);
        PopFloatingBubbles();
        yield return StartCoroutine(MoveRemainingBubblesAlongPath());
        yield return StartCoroutine(SpawnAndMoveNewBubblesAlongPath());

        StageManager.Instance.CanShoot = true;
        StageManager.Instance.UpdateCameraAndShooterPos(m_Grid);
    }

    private void PopBubble(int startRowIdx, int startColIdx)
    {
        int[,,] idxOffset = new int[2, 6, 2]
        {
            { { 0, 1 }, { 1, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 }, { -1, 1 } },
            { { 0, 1 }, { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, -1 }, { -1, 0 } }
        };

        GridCell startCell = m_Grid[startRowIdx].Columns[startColIdx];
        BubbleColor startBubbleColor = startCell.CellGO.GetComponent<Bubble>().BubbleColor;

        // BFS를 통해 인접 셀 확인
        Queue<(int row, int col, bool fromWildCard)> queue = new Queue<(int, int, bool)>();
        HashSet<(int, int)> visited = new HashSet<(int, int)>();

        queue.Enqueue((startRowIdx, startColIdx, false));
        visited.Add((startRowIdx, startColIdx));

        while (queue.Count > 0)
        {
            (int curRowIdx, int curColIdx, bool fromWildCard) = queue.Dequeue();
            GridCell curCell = m_Grid[curRowIdx].Columns[curColIdx];
            Bubble curBubble = curCell.CellGO.GetComponent<Bubble>();
            bool curIsWildCard = curBubble.BubbleColor == BubbleColor.WILDCARD;

            // WildCard 주변 6방향 즉시 큐에 넣기
            if (curIsWildCard)
            {
                for (int i = 0; i < 6; i++)
                {
                    int nxtRowIdx = curRowIdx + idxOffset[curRowIdx % 2, i, 0];
                    int nxtColIdx = curColIdx + idxOffset[curRowIdx % 2, i, 1];

                    if (nxtRowIdx < 0 || nxtColIdx < 0 || nxtRowIdx >= m_Grid.Count || nxtColIdx >= m_Grid[nxtRowIdx].Columns.Count) continue;
                    if (visited.Contains((nxtRowIdx, nxtColIdx))) continue;

                    GridCell neighbor = m_Grid[nxtRowIdx].Columns[nxtColIdx];
                    if (neighbor.CellType == GridCellType.EMPTY || neighbor.CellType == GridCellType.BUBBLE_SPAWNER) continue;

                    queue.Enqueue((nxtRowIdx, nxtColIdx, true)); // WildCard에서 들어온 경우
                    visited.Add((nxtRowIdx, nxtColIdx));
                }
            }

            // 일반 BFS 확장
            if (!fromWildCard)
            {
                for (int i = 0; i < 6; i++)
                {
                    int nxtRowIdx = curRowIdx + idxOffset[curRowIdx % 2, i, 0];
                    int nxtColIdx = curColIdx + idxOffset[curRowIdx % 2, i, 1];

                    if (nxtRowIdx < 0 || nxtColIdx < 0 || nxtRowIdx >= m_Grid.Count || nxtColIdx >= m_Grid[nxtRowIdx].Columns.Count) continue;
                    if (visited.Contains((nxtRowIdx, nxtColIdx))) continue;

                    GridCell neighbor = m_Grid[nxtRowIdx].Columns[nxtColIdx];
                    if (neighbor.CellType == GridCellType.EMPTY || neighbor.CellType == GridCellType.BUBBLE_SPAWNER) continue;

                    Bubble neighborBubble = neighbor.CellGO.GetComponent<Bubble>();
                    if (neighborBubble.BubbleColor == startBubbleColor || neighborBubble.BubbleColor == BubbleColor.WILDCARD)
                    {
                        queue.Enqueue((nxtRowIdx, nxtColIdx, false));
                        visited.Add((nxtRowIdx, nxtColIdx));
                    }
                }
            }
        }
        

        if (visited.Count >= 3)
        {
            foreach ((int rowIdx, int colIdx) in visited)
            {
                GridCell curCell = m_Grid[rowIdx].Columns[colIdx];
                StageManager.Instance.ReturnToPoolBubbleGO(curCell.CellGO);
                curCell.CellGO = null;
                curCell.CellType = GridCellType.EMPTY;
                StageManager.Instance.SpawnBubblePopVFX(curCell.CellPosition);
            }
        }
    }

    private void PopFloatingBubbles()
    {
        int[,,] idxOffset = new int[2, 6, 2]
        {
            { { 0, 1 }, { 1, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 }, { -1, 1 } },
            { { 0, 1 }, { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, -1 }, { -1, 0 } }
        };

        Queue<(int row, int col)> queue = new Queue<(int, int)>();
        HashSet<(int, int)> visited = new HashSet<(int, int)>();

        // 위쪽 행에 있는 모든 버블을 BFS 시작점으로
        for (int col = 0; col < m_Grid[0].Columns.Count; col++)
        {
            GridCell cell = m_Grid[0].Columns[col];
            if (cell.CellType == GridCellType.BUBBLE)
            {
                queue.Enqueue((0, col));
                visited.Add((0, col));
            }
        }

        // BFS로 연결된 버블 모두 방문
        while (queue.Count > 0)
        {
            (int curRowIdx, int curColIdx) = queue.Dequeue();

            for (int i = 0; i < 6; i++)
            {
                int nxtRowIdx = curRowIdx + idxOffset[curRowIdx % 2, i, 0];
                int nxtColIdx = curColIdx + idxOffset[curRowIdx % 2, i, 1];

                if (nxtRowIdx < 0 || nxtColIdx < 0 || nxtRowIdx >= m_Grid.Count || nxtColIdx >= m_Grid[nxtRowIdx].Columns.Count) continue;
                if (visited.Contains((nxtRowIdx, nxtColIdx))) continue;

                GridCell neighbor = m_Grid[nxtRowIdx].Columns[nxtColIdx];
                if (neighbor.CellType != GridCellType.BUBBLE) continue;

                queue.Enqueue((nxtRowIdx, nxtColIdx));
                visited.Add((nxtRowIdx, nxtColIdx));
            }
        }
        // BFS에 포함되지 않은 버블은 떨어뜨리기
        for (int rowIdx = 0; rowIdx < m_Grid.Count; rowIdx++)
        {
            for (int colIdx = 0; colIdx < m_Grid[rowIdx].Columns.Count; colIdx++)
            {
                GridCell cell = m_Grid[rowIdx].Columns[colIdx];
                if (cell.CellType == GridCellType.BUBBLE && !visited.Contains((rowIdx, colIdx)))
                {
                    // 파괴
                    StageManager.Instance.ReturnToPoolBubbleGO(cell.CellGO);
                    cell.CellGO = null;
                    cell.CellType = GridCellType.EMPTY;
                    StageManager.Instance.SpawnBubblePopVFX(cell.CellPosition);
                }
            }
        }
    }

    private IEnumerator MoveRemainingBubblesAlongPath()
    {
        List<List<Tuple<Vector2Int, Vector2Int>>> pathes = m_GridMaker.GetPaths();

        Sequence allSequence = DOTween.Sequence();
        foreach (List<Tuple<Vector2Int, Vector2Int>> path in pathes)
        {
            // 경로 순서 정리 [스폰지점, 다음1, 다음2, ...]
            List<Vector2Int> ordered = new List<Vector2Int>();
            ordered.Add(path[0].Item1);
            foreach (var tuple in path)
                ordered.Add(tuple.Item2);

            Sequence mainSequence = DOTween.Sequence();
            for (int i = ordered.Count - 1; i >= 1; i--)
            {
                GridCell curCell = m_Grid[ordered[i].x].Columns[ordered[i].y];
                if (curCell.CellType != GridCellType.EMPTY) continue;

                GridCell movingCell = null;
                List<Vector3> movePositions = new List<Vector3>();
                movePositions.Add(curCell.CellPosition);

                for (int j = i - 1; j >= 1; j--)
                {
                    GridCell prevCell = m_Grid[ordered[j].x].Columns[ordered[j].y];

                    if (prevCell.CellType == GridCellType.EMPTY)
                        movePositions.Add(prevCell.CellPosition);
                    else if (prevCell.CellType == GridCellType.BUBBLE)
                    {
                        movingCell = prevCell;
                        movePositions.Reverse();
                        break;
                    }
                }

                if (movingCell == null) continue; // 이동할 버블 없음

                curCell.CellGO = movingCell.CellGO;
                curCell.CellType = movingCell.CellType;
                movingCell.CellGO = null;
                movingCell.CellType = GridCellType.EMPTY;

                Bubble bubble = curCell.CellGO.GetComponent<Bubble>();
                bubble.rowIdx = ordered[i].x;
                bubble.colIdx = ordered[i].y;

                Sequence subSequence = DOTween.Sequence();
                foreach (var targetPos in movePositions)
                    subSequence.Append(curCell.CellGO.transform.DOMove(targetPos, 0.1f).SetEase(Ease.Linear));
                mainSequence.Insert(0.1f * ((ordered.Count - 1) - i), subSequence);
            }
            allSequence.Join(mainSequence);
        }
        yield return allSequence.WaitForCompletion();
    }


    private IEnumerator SpawnAndMoveNewBubblesAlongPath()
    {
        List<List<Tuple<Vector2Int, Vector2Int>>> pathes = m_GridMaker.GetPaths();

        Sequence allSequence = DOTween.Sequence();
        foreach (List<Tuple<Vector2Int, Vector2Int>> path in pathes)
        {
            if (path == null || path.Count == 0)
                continue;

            // 경로 순서 정리 [스폰지점, 다음1, 다음2, ...]
            List<Vector2Int> ordered = new List<Vector2Int>();
            ordered.Add(path[0].Item1);
            foreach (var tuple in path)
                ordered.Add(tuple.Item2);

            // 경로 내 빈 셀 개수 확인
            int emptyCellCount = 0;
            for (int i = 0; i < ordered.Count; i++)
            {
                if (m_Grid[ordered[i].x].Columns[ordered[i].y].CellType == GridCellType.EMPTY)
                    emptyCellCount++;
            }

            List<GameObject> newBubbles = new List<GameObject>();
            for (int i = 0; i < emptyCellCount; i++)
            {
                Vector2Int spawnCellIdx = ordered[0];
                GridCell spawnCell = m_Grid[spawnCellIdx.x].Columns[spawnCellIdx.y];

                GameObject newBubbleGO = StageManager.Instance.BarrowFromPoolOnGridBubble(spawnCell.CellPosition, GridCellType.BUBBLE, transform);
                newBubbles.Add(newBubbleGO);
            }

            Sequence mainSequence = DOTween.Sequence();
            for (int i = 0; i < newBubbles.Count; i++)
            {
                GameObject bubbleGO = newBubbles[i];
                Bubble bubble = bubbleGO.GetComponent<Bubble>();

                int targetCellRowIdx = 0;
                int targetCellColIdx = 0;

                List<Vector3> movePositions = new List<Vector3>();
                for (int j = 1; j < ordered.Count; j++)
                {
                    Vector2Int toCellIdx = ordered[j];
                    GridCell toCell = m_Grid[toCellIdx.x].Columns[toCellIdx.y];

                    if (toCell.CellType != GridCellType.EMPTY) break;
                    movePositions.Add(toCell.CellPosition);

                    targetCellRowIdx = toCellIdx.x;
                    targetCellColIdx = toCellIdx.y;
                }

                GridCell targetCell = m_Grid[targetCellRowIdx].Columns[targetCellColIdx];
                targetCell.CellGO = bubbleGO;
                targetCell.CellType = GridCellType.BUBBLE;

                bubble.rowIdx = targetCellRowIdx;
                bubble.colIdx = targetCellColIdx;

                Sequence subSequence = DOTween.Sequence();
                foreach (var targetPosition in movePositions)
                    subSequence.Append(bubbleGO.transform.DOMove(targetPosition, 0.1f).SetEase(Ease.Linear));
                mainSequence.Insert(0.1f * i, subSequence); 
            }
            allSequence.Join(mainSequence);
        }
        yield return allSequence.WaitForCompletion();
    }

    public Vector2 ActivateGlowBubble(int rowIdx, int colIdx, Vector3 hitPoint)
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

    public void DeactivateGlowBubble()
    {
        m_GlowBubble.SetActive(false);
    }
}
