using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class StageManager : SingletonBehaviour<StageManager>
{
    [Header("현재 스테이지 상태")]
    [SerializeField] private int m_CurrentStageLevel = 10;
    [HideInInspector] public StageStat CurrentStageStat = null;
    private float m_RemainingBossHealth = 0.0f;
    private int m_RemainingBubbleAmount = 0;
    public event Action OnRemaingBossHealthChanged;
    public event Action OnRemainingBubbleAmountChanged;
    public float RemainingBossHealth
    {
        get => m_RemainingBossHealth;
        set
        {
            m_RemainingBossHealth = Mathf.Max(0f, value);
            OnRemaingBossHealthChanged?.Invoke();
            if (m_RemainingBossHealth <= 0.0f) StageEnd(true);
        }
    }

    public int RemainingBubbleAmount
    {
        get => m_RemainingBubbleAmount;
        set
        {
            m_RemainingBubbleAmount = Mathf.Max(0, value);
            OnRemainingBubbleAmountChanged?.Invoke();
            if (m_RemainingBubbleAmount <= 0 && m_RemainingBossHealth > 0.0f) StageEnd(false);
        }
    }

    [Header("버블 프리팹")]
    [SerializeField] private List<GameObject> m_ColorBubblePrefabs;
    private Dictionary<BubbleColor, GameObject> m_ColorBubblePrefabsDict = new Dictionary<BubbleColor, GameObject>();
    private Dictionary<BubbleColor, ObjectPool<GameObject>> m_ColorBubblePool = new Dictionary<BubbleColor, ObjectPool<GameObject>>();
    [SerializeField] private GameObject m_SpawnerBubblePrefab;

    [Header("버블 폭발 이펙트")]
    [SerializeField] private GameObject m_BubblePopVFXPrefab;
    private ObjectPool<GameObject> m_BubblePopVFXPool;

    [Header("카메라 및 슈터")]
    [SerializeField] private GameObject m_ShooterPrefab; 
    private GameObject m_MainCamera;
    private GameObject m_Shooter;

    [HideInInspector] public bool CanShoot;

    private void Awake()
    {
        m_IsDestroyOnLoad = true;       // 씬 전환 시 삭제되도록
        Init();

        m_MainCamera = Camera.main.gameObject;
    }

    private void Start()
    {
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

        m_BubblePopVFXPool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject go = Instantiate(m_BubblePopVFXPrefab);
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

        m_RemainingBossHealth = CurrentStageStat.RemaingBossHealth;
        m_RemainingBubbleAmount = CurrentStageStat.RemainingBubbleAmount;

        GridManager.Instance.GenerateGrid();
        CanShoot = true;

        // TODO : UIManager FadeOut

    }


    public GameObject BarrowFromPoolOnGridBubble(Vector3 position, GridCellType gridCellType, Transform parent, BubbleColor bubbleColor = BubbleColor.NONE)
    {
        GameObject go = null;
        
        if (gridCellType == GridCellType.BUBBLE)
        {
            if (bubbleColor == BubbleColor.NONE)
            {
                float rand = UnityEngine.Random.value;
                if (rand <= 0.98f) bubbleColor = (BubbleColor)UnityEngine.Random.Range(0, 3);
                else bubbleColor = BubbleColor.WILDCARD;
            }
            go = m_ColorBubblePool[bubbleColor].Get();
            go.transform.position = position;
            go.transform.SetParent(parent);
        }
        else if (gridCellType == GridCellType.BUBBLE_SPAWNER)
        {
            go = Instantiate(m_SpawnerBubblePrefab, position, Quaternion.identity);
            go.transform.SetParent(parent);
        }
        go.tag = "OnGridBubble";

        return go;
    }

    public GameObject BarrowFromPoolShootingBubble(Vector3 position, Transform parent)
    {
        GameObject go = null;

        BubbleColor bubbleColor = (BubbleColor)UnityEngine.Random.Range(0, 3);
        go = m_ColorBubblePool[bubbleColor].Get();
        go.transform.position = position;
        go.transform.SetParent(parent);
        go.tag = "ShootingBubble";

        Bubble bubble = go.GetComponent<Bubble>();
        bubble.ActivateGlowEffect();
     
        return go;
    }

    public void ReturnToPoolBubbleGO(GameObject go)
    {
        Bubble bubble = go.GetComponent<Bubble>();
        BubbleColor bubbleColor = bubble.BubbleColor;

        m_ColorBubblePool[bubbleColor].Release(go);
    }

    public void SpawnBubblePopVFX(Vector3 position)
    {
        GameObject vfx = m_BubblePopVFXPool.Get();
        vfx.transform.position = position;

        // 0.3초 후 자동으로 풀로 반환
        StartCoroutine(ReturnVFXAfterDelay(vfx, 0.3f));
    }

    private IEnumerator ReturnVFXAfterDelay(GameObject vfx, float delay)
    {
        yield return new WaitForSeconds(delay);
        m_BubblePopVFXPool.Release(vfx);
    }

    public void UpdateCameraAndShooterPos(List<Row> grid, bool isInit = false)
    {
        float minY = float.MaxValue;

        for (int row = grid.Count - 1; row >= 0; row--)
        {
            foreach (var cell in grid[row].Columns)
            {
                if (cell.CellType == GridCellType.BUBBLE || cell.CellType == GridCellType.BUBBLE_SPAWNER)
                {
                    minY = cell.CellPosition.y;
                    break; 
                }
            }
            if (minY != float.MaxValue)
                break;
        }

        Vector3 targetCameraPos = new Vector3(GridManager.Instance.CenterXPos, minY, m_MainCamera.transform.position.z);
        Vector3 targetShooterPos = new Vector3(GridManager.Instance.CenterXPos, minY - 4f, m_Shooter != null ? m_Shooter.transform.position.z : 0f);
        m_MainCamera.transform.DOMove(targetCameraPos, isInit ? 0.0f : 0.3f).SetEase(Ease.OutQuad);

        // 슈터 위치 설정
        Vector3 shooterPos = new Vector3(GridManager.Instance.CenterXPos, minY - 4f, m_Shooter != null ? m_Shooter.transform.position.z : 0f);
        if (m_Shooter == null) m_Shooter = Instantiate(m_ShooterPrefab, shooterPos, Quaternion.identity);
        else m_Shooter.transform.DOMove(targetShooterPos, 0.3f).SetEase(Ease.OutQuad);
    }

    public void StageEnd(bool isWin)
    {
        CanShoot = false;

        var confirmUIData = new ConfirmUIData();
        confirmUIData.ConfirmType = ConfirmType.OK;

        if (isWin)
        {
            confirmUIData.TitleText = "스테이지 클리어!";
            confirmUIData.DesciptionText = "적을 무찔렀습니다!";
        }
        else
        {
            confirmUIData.TitleText = "클리어 실패!";
            confirmUIData.DesciptionText = "더 많은 버블을 부셔 공격해내세요!";
        }
        
        confirmUIData.OKButtonText = "로비";
        confirmUIData.OnClickOKButton = () => { SceneLoader.Instance.LoadScene(SceneType.Lobby); };
        UIManager.Instance.OpenUI<ConfirmUI>(confirmUIData);
    }
}
