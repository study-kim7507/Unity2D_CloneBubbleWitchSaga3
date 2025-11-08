using UnityEngine;
using UnityEngine.UI;

public class StageUIController : SingletonBehaviour<StageUIController>
{
    [HideInInspector] public float InitailBossHealth;
    public Image BossHealthBarImage;

    private void Awake()
    {
        m_IsDestroyOnLoad = true;
        Init();
    }

    protected override void Init()
    {
        base.Init();

        StageManager.Instance.OnRemainingBossHealthChanged += UpdateBossHealthBarImage;
    }

    private void UpdateBossHealthBarImage()
    {
        BossHealthBarImage.fillAmount = StageManager.Instance.RemainingBossHealth / InitailBossHealth;
    }
}
