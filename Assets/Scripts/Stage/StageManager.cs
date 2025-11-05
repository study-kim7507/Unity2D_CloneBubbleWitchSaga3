using UnityEngine;

public class StageManager : SingletonBehaviour<StageManager>
{
    // 현재 스테이지 상태
    [SerializeField] private int CurrentStageLevel = 10;
    [HideInInspector] public StageStat CurrentStageStat = null;
    private int m_RemainingBubbleAmount = 0;

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

    private void StartStage()
    {
        Logger.Log($"{GetType()}::StartStage");

        string path = $"Stage/Level{CurrentStageLevel}";
        CurrentStageStat = Resources.Load<StageStat>(path);

        if (CurrentStageStat == null)
        {
            Logger.LogError("스테이지 데이터를 찾을 수 없음.");
            return;
        }

        m_RemainingBubbleAmount = CurrentStageStat.RemainingBubbleAmount;

        GridManager.Instance.Initialize();
    }
}
