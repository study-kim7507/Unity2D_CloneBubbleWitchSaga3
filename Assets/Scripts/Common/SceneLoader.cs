using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneType
{
    Title,
    Lobby,
    Stage,
}

public class SceneLoader : SingletonBehaviour<SceneLoader>
{
    public void LoadScene(SceneType sceneType)
    {
        Logger.Log($"{sceneType} scene loading...");

        Time.timeScale = 1.0f;
        SceneManager.LoadScene(sceneType.ToString());
    }

    public void ReloadScene()
    {
        Logger.Log($"{SceneManager.GetActiveScene().name} scene loading...");

        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public AsyncOperation LoadSceneAsync(SceneType sceneType)
    {
        Logger.Log($"{sceneType} scene async loading...");

        Time.timeScale = 1.0f;
        return SceneManager.LoadSceneAsync(sceneType.ToString());
    }
}
