// Reference : https://www.redblobgames.com/grids/hexagons/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class GridMaker : MonoBehaviour
{
    [SerializeField] private List<GameObject> ColorBubblePrefabs;
    private Dictionary<BubbleColor, GameObject> m_ColorBubblePrefabsDict = new Dictionary<BubbleColor, GameObject>();
    private Dictionary<BubbleColor, ObjectPool<GameObject>> m_ColorBubblePool = new Dictionary<BubbleColor, ObjectPool<GameObject>>();

    [SerializeField] private GameObject m_WildCardBubblePrefab;
    [SerializeField] private GameObject m_SkeletonBubblePrefab;
    
    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        foreach (GameObject bubblePrefab in ColorBubblePrefabs)
        {
            Bubble bubble = bubblePrefab.GetComponent<Bubble>();
            BubbleColor bubbleColor = bubble.BubbleColor;
            if (!m_ColorBubblePrefabsDict.ContainsKey(bubbleColor))
                m_ColorBubblePrefabsDict.Add(bubbleColor, bubblePrefab);
        }

        foreach (var kvp in m_ColorBubblePrefabsDict)
        {
            BubbleColor bubbleColor = kvp.Key;
            GameObject bubblePrefab = kvp.Value;

            m_ColorBubblePool[bubbleColor] = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    GameObject go = Instantiate(bubblePrefab);
                    go.transform.SetParent(transform);
                    go.SetActive(false);
                    return go;
                },
                actionOnGet: (go) => go.SetActive(true),
                actionOnRelease: (go) => go.SetActive(false),
                collectionCheck: true,              // 이미 풀로 반환된 오브젝트가 다시 반환되는 경우 에러
                defaultCapacity: 10
                );
        }
    }

    public void GenerateGrid(List<Row> grid)
    {
        Logger.Log($"{GetType()}::GenerateGrid(List<Row> grid)");

        List<Row> gridData = StageManager.Instance.CurrentStageStat.GridData;
        if (gridData == null || gridData.Count == 0)
            return;

        float yOffset = 0.5f; 
        float xOffset = 0.55f;  

        float startY = 0f;
        float startX = 0f;

        for (int row = 0; row < gridData.Count; row++)
        {
            float rowXOffset = (row % 2 != 0) ? 0f : xOffset * 0.5f; // 짝수 행이면 오른쪽으로 살짝 밀기 (even-r offset)

            Row currentRow = new Row();
            currentRow.Columns = new List<GridCell>();
            for (int col = 0; col < gridData[row].Columns.Count; col++)
            {
                GridCell currentCellData = gridData[row].Columns[col];

                Vector3 spawnPos = new Vector3(startX + col * xOffset + rowXOffset, startY - row * yOffset, 0);

                GridCell currentCell = new GridCell();
                switch (currentCellData.CellType)
                {
                    case GridCellType.EMPTY:
                        break;

                    case GridCellType.BUBBLE:
                        currentCell.CellGO = SpawnBubble(spawnPos, currentCellData);
                        currentCell.CellPosition = spawnPos;
                        currentCell.CellType = GridCellType.BUBBLE;
                        break;

                    // 특수 버블 처리
                    case GridCellType.BUBBLE_WILDCARD:
                        currentCell.CellGO = SpawnBubble(spawnPos, currentCellData);
                        currentCell.CellPosition = spawnPos;
                        currentCell.CellType = GridCellType.BUBBLE_WILDCARD;
                        break;

                    case GridCellType.BUBBLE_SKELTON:
                        currentCell.CellGO = SpawnBubble(spawnPos, currentCellData);
                        currentCell.CellPosition = spawnPos;
                        currentCell.CellType = GridCellType.BUBBLE_SKELTON;
                        break;
                }
                currentRow.Columns.Add(currentCell);
            }
            grid.Add(currentRow);
        }
    }

    private GameObject SpawnBubble(Vector3 position, GridCell gridCell)
    {
        GameObject go = null;

        BubbleColor bubbleColor = BubbleColor.NONE;
        if (gridCell.CellType == GridCellType.BUBBLE) 
        {
            bubbleColor = (BubbleColor)Random.Range(0, 3);
            go = m_ColorBubblePool[bubbleColor].Get();
            go.transform.position = position;
            go.transform.SetParent(transform);
        }
        else if (gridCell.CellType == GridCellType.BUBBLE_WILDCARD)
        {
            go = Instantiate(m_WildCardBubblePrefab, position, Quaternion.identity);
            go.transform.SetParent(transform);
        }
        else if (gridCell.CellType == GridCellType.BUBBLE_SKELTON)
        {
            go = Instantiate(m_SkeletonBubblePrefab, position, Quaternion.identity);
            go.transform.SetParent(transform);
        }
        
        return go;
    }
}
