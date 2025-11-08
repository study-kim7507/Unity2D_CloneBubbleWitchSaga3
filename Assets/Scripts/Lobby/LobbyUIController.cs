using UnityEngine;

public class LobbyUIController : SingletonBehaviour<LobbyUIController>
{
    private void Awake()
    {
        m_IsDestroyOnLoad = true;
        Init();
    }

    protected override void Init()
    {
        base.Init();

        UIManager.Instance.Fade(Color.black, 1.0f, 0.0f, 0.5f, 0.0f, true, () => AudioManager.Instance.PlayBGM(BGM.LOBBY, 0.15f));
    }

    public void OnClickStartButton()
    {
        Logger.Log($"{GetType()}::OnClickStartButton");

        AudioManager.Instance.PlaySFX(SFX.UI_BUTTON_CLICK);
        AudioManager.Instance.StopBGM();
        UIManager.Instance.Fade(Color.black, 0.0f, 1.0f, 0.5f, 0.0f, false, () =>
        {
            UIManager.Instance.CloseAllOpenUI();
            LobbyManager.Instance.StartInStage();
        });
    }
}
