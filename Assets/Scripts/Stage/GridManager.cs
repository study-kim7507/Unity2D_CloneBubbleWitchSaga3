using System;
using System.Collections;
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
        m_GridMaker.GeneratePath(m_Grid);
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
        {
            m_GridMaker.GenerateNewRow(m_Grid);
            StageManager.Instance.SetCameraAndShooterPos();
        }

        PopBubble(m_TargetRowIdx, m_TargetColIdx);
        PopFloatingBubbles();
        yield return StartCoroutine(MoveRemainingBubblesAlongPath());
        yield return StartCoroutine(SpawnAndMoveNewBubblesAlongPath());
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
                    if (neighbor.CellType == GridCellType.EMPTY || neighbor.CellType == GridCellType.SKELETON) continue;

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
                    if (neighbor.CellType == GridCellType.EMPTY || neighbor.CellType == GridCellType.SKELETON) continue;

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
                }
            }
        }
    }

    private IEnumerator MoveRemainingBubblesAlongPath()
    {
        List<List<Tuple<Vector2Int, Vector2Int>>> pathes = m_GridMaker.GetPaths();

        foreach (List<Tuple<Vector2Int, Vector2Int>> path in pathes)
        {
            if (path == null || path.Count == 0)
                continue;

            // [스폰 지점, 다음1 지점, 다음2 지점, 다음3 지점, ,,,, 끝 지점]
            List<Vector2Int> ordered = new List<Vector2Int>();
            ordered.Add(path[0].Item1);
            foreach (var tuple in path)
                ordered.Add(tuple.Item2);

            Queue<(Vector2Int from, Vector2Int to)> moveQueue = new Queue<(Vector2Int, Vector2Int)>();

            int lastEmptyIndex = -1;
            for (int i = ordered.Count - 1; i >= 1; i--)
            {
                GridCell cell = m_Grid[ordered[i].x].Columns[ordered[i].y];
                if (cell.CellGO == null)
                {
                    if (lastEmptyIndex == -1)
                        lastEmptyIndex = i;
                }
                else if (lastEmptyIndex != -1)
                {
                    moveQueue.Enqueue((ordered[i], ordered[lastEmptyIndex]));
                    lastEmptyIndex--; 
                }
            }

            while (moveQueue.Count > 0)
            {
                var (from, to) = moveQueue.Dequeue();

                GridCell fromCell = m_Grid[from.x].Columns[from.y];
                GridCell toCell = m_Grid[to.x].Columns[to.y];

                if (fromCell.CellGO == null) continue;

                toCell.CellGO = fromCell.CellGO;
                toCell.CellType = fromCell.CellType;
                toCell.CellGO.transform.position = toCell.CellPosition;

                Bubble bubble = toCell.CellGO.GetComponent<Bubble>();
                bubble.rowIdx = to.x;
                bubble.colIdx = to.y;

                fromCell.CellGO = null;
                fromCell.CellType = GridCellType.EMPTY;

                yield return new WaitForSeconds(0.05f);
            }
        }
    }



    private IEnumerator SpawnAndMoveNewBubblesAlongPath()
    {
        List<List<Tuple<Vector2Int, Vector2Int>>> pathes = m_GridMaker.GetPaths();

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
            foreach (var pos in ordered)
            {
                if (m_Grid[pos.x].Columns[pos.y].CellType == GridCellType.EMPTY)
                    emptyCellCount++;
            }

            // 빈 셀 개수만큼 반복적으로 버블 생성 및 이동
            while (emptyCellCount-- > 0)
            {
                Vector2Int spawnPos = ordered[0]; // 스폰 위치
                Vector2Int firstTargetPos = ordered[1]; // 실제 첫 도착 셀

                GridCell spawnCell = m_Grid[spawnPos.x].Columns[spawnPos.y];
                GridCell firstTargetCell = m_Grid[firstTargetPos.x].Columns[firstTargetPos.y];

                // ✅ 스폰셀은 건드리지 않는다.
                // 버블은 단지 스폰셀의 "위치" 위에서 생성만 한다.
                Vector3 spawnWorldPos = spawnCell.CellPosition;

                // 버블 생성 (스폰셀 위 위치에서 생성)
                GameObject newBubbleGO = StageManager.Instance.SpawnOnGridBubble(spawnWorldPos, GridCellType.BUBBLE, transform);
                newBubbleGO.transform.position = spawnWorldPos;
                newBubbleGO.SetActive(true);

                Bubble newBubble = newBubbleGO.GetComponent<Bubble>();
                newBubble.rowIdx = firstTargetPos.x;
                newBubble.colIdx = firstTargetPos.y;

                // ✅ 스폰셀의 CellGO나 CellType은 절대 건드리지 않음

                // 첫 번째 이동 대상 셀에 배치
                firstTargetCell.CellGO = newBubbleGO;
                firstTargetCell.CellType = GridCellType.BUBBLE;

                // 첫 번째 이동: 스폰 위치 → 첫 셀로 자연스럽게 이동
                newBubbleGO.transform.position = firstTargetCell.CellPosition;
                yield return new WaitForSeconds(0.05f);

                // 이후 한 칸씩 순차 이동
                for (int i = 1; i < ordered.Count - 1; i++)
                {
                    Vector2Int from = ordered[i];
                    Vector2Int to = ordered[i + 1];

                    GridCell fromCell = m_Grid[from.x].Columns[from.y];
                    GridCell toCell = m_Grid[to.x].Columns[to.y];

                    if (toCell.CellGO != null)
                        break;

                    // 이동
                    toCell.CellGO = fromCell.CellGO;
                    toCell.CellType = fromCell.CellType;
                    toCell.CellGO.transform.position = toCell.CellPosition;

                    Bubble bubble = toCell.CellGO.GetComponent<Bubble>();
                    bubble.rowIdx = to.x;
                    bubble.colIdx = to.y;

                    // 원래 칸 비움
                    fromCell.CellGO = null;
                    fromCell.CellType = GridCellType.EMPTY;

                    yield return new WaitForSeconds(0.05f);
                }

                // 다음 버블 생성 전 잠깐 대기
                yield return new WaitForSeconds(0.05f);
            }
        }
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
