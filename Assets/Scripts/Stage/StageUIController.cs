using UnityEngine;

public class StageUIController : SingletonBehaviour<StageUIController>
{
    private void Awake()
    {
        m_IsDestroyOnLoad = true;
        Init();
    }

    protected override void Init()
    {
        base.Init();
    }
}
