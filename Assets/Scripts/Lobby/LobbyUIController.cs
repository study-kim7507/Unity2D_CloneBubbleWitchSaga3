using UnityEngine;

public class LobbyUIController : MonoBehaviour
{
    public void Init()
    {

    }

    public void OnClickStartButton()
    {
        Logger.Log($"{GetType()}::OnClickStartButton");

        LobbyManager.Instance.StartInStage();
    }
}
