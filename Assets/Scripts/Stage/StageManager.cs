using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class StageManager : SingletonBehaviour<StageManager>
{
    [Header("현재 스테이지 상태")]
    [SerializeField] private int m_CurrentStageLevel = 10;
    [HideInInspector] public StageStat CurrentStageStat = null;
    private int m_RemainingBubbleAmount = 0;

    [Header("버블 프리팹")]
    [SerializeField] private List<GameObject> m_ColorBubblePrefabs;
    private Dictionary<BubbleColor, GameObject> m_ColorBubblePrefabsDict = new Dictionary<BubbleColor, GameObject>();
    private Dictionary<BubbleColor, ObjectPool<GameObject>> m_ColorBubblePool = new Dictionary<BubbleColor, ObjectPool<GameObject>>();

    [SerializeField] private GameObject m_WildCardBubblePrefab;
    [SerializeField] private GameObject m_SkeletonBubblePrefab;

    [Header("카메라 및 슈터")]
    [SerializeField] private GameObject m_ShooterPrefab; 
    private GameObject m_MainCamera;
    private GameObject m_Shooter;

    private void Awake()
    {
        m_IsDestroyOnLoad = true;       // 씬 전환 시 삭제되도록
        Init();
    }

    private void Start()
    {
        Logger.Log($"{GetType()}::Start");

        StartStage();
    }

    protected override void Init()
    {
        base.Init();
        foreach (GameObject bubblePrefab in m_ColorBubblePrefabs)
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

        m_MainCamera = Camera.main.gameObject;
    }

    private void StartStage()
    {
        Logger.Log($"{GetType()}::StartStage");

        string path = $"Stage/StageData/Level{m_CurrentStageLevel}";
        CurrentStageStat = Resources.Load<StageStat>(path);

        if (CurrentStageStat == null)
        {
            Logger.LogError("스테이지 데이터를 찾을 수 없음.");
            return;
        }

        m_RemainingBubbleAmount = CurrentStageStat.RemainingBubbleAmount;

        GridManager.Instance.GenerateGrid();
        SetCameraAndShooterPos();
    }


    public GameObject SpawnOnGridBubble(Vector3 position, GridCellType gridCellType, Transform parent, BubbleColor bubbleColor = BubbleColor.NONE)
    {
        GameObject go = null;
        
        if (gridCellType == GridCellType.BUBBLE)
        {
            if (bubbleColor == BubbleColor.NONE) bubbleColor = (BubbleColor)Random.Range(0, 3);
            go = m_ColorBubblePool[bubbleColor].Get();
            go.transform.position = position;
            go.transform.SetParent(parent);
        }
        else if (gridCellType == GridCellType.BUBBLE_WILDCARD)
        {
            go = Instantiate(m_WildCardBubblePrefab, position, Quaternion.identity);
            go.transform.SetParent(parent);
        }
        else if (gridCellType == GridCellType.BUBBLE_SKELTON)
        {
            go = Instantiate(m_SkeletonBubblePrefab, position, Quaternion.identity);
            go.transform.SetParent(parent);
        }

        Rigidbody2D rigidbody2D = go.GetComponent<Rigidbody2D>();
        rigidbody2D.bodyType = RigidbodyType2D.Static;
        go.tag = "OnGridBubble";

        return go;
    }

    public GameObject SpawnShootingBubble(Vector3 position, Transform parent)
    {
        GameObject go = null;

        BubbleColor bubbleColor = (BubbleColor)Random.Range(0, 3);
        go = m_ColorBubblePool[bubbleColor].Get();
        go.transform.position = position;
        go.transform.SetParent(parent);

        Rigidbody2D rigidbody2D = go.GetComponent<Rigidbody2D>();
        rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        go.tag = "ShootingBubble";
     
        return go;
    }

    public void ReturnToPoolBubbleGO(GameObject go)
    {
        Bubble bubble = go.GetComponent<Bubble>();
        BubbleColor bubbleColor = bubble.BubbleColor;

        m_ColorBubblePool[bubbleColor].Release(go);
    }

    public void SetCameraAndShooterPos()
    {
        // 그리드의 위치에 따라 카메라와 슈터의 위치가 이동되도록
        float minY = GridManager.Instance.MinBubbleYPos;
        float cameraY = minY;

        // 카메라 위치 설정
        m_MainCamera.transform.position = new Vector3(GridManager.Instance.CenterXPos, cameraY, m_MainCamera.transform.position.z);

        // 슈터 생성 및 위치 설정
        if (m_Shooter == null)
            m_Shooter = Instantiate(m_ShooterPrefab, new Vector3(GridManager.Instance.CenterXPos, cameraY - 4f, 0f), Quaternion.identity);
        else
            m_Shooter.transform.position = new Vector3(GridManager.Instance.CenterXPos, cameraY - 4f, m_Shooter.transform.position.z);
    }
}
