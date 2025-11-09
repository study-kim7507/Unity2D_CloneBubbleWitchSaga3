using NUnit.Framework.Constraints;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    // 로고
    public Animation LogoAnim;
    public TMP_Text LogoText;

    // 타이틀
    public GameObject Title;
    public Slider LoadingSlider;
    public TMP_Text LoadingProgressText;

    private AsyncOperation m_AsyncOperation;

    private void Awake()
    {
        LogoAnim.gameObject.SetActive(true);
        Title.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(LoadGameCo());
    }

    private IEnumerator LoadGameCo()
    {
        LogoAnim.Play();
        yield return new WaitForSeconds(LogoAnim.clip.length);

        LogoAnim.gameObject.SetActive(false);
        Title.SetActive(true);

        m_AsyncOperation = SceneLoader.Instance.LoadSceneAsync(SceneType.Lobby);
        if (m_AsyncOperation == null)
        {
            Logger.LogError("lobby async loading error.");
            yield break;
        }

        // 비동기 씬 로딩이 완료되어도 자동으로 씬이 넘어가지 않도록 비활성화
        m_AsyncOperation.allowSceneActivation = false;

        // 자연스러운 로딩 효과 구현을 위해 의도적인 지연
        LoadingSlider.value = 0.5f;
        LoadingProgressText.text = $"{(int)(LoadingSlider.value * 100)}%";
        yield return new WaitForSeconds(0.5f);

        // 로딩이 진행 중일 때
        while(!m_AsyncOperation.isDone)
        {
            LoadingSlider.value = m_AsyncOperation.progress < 0.5f ? 0.5f : m_AsyncOperation .progress;
            LoadingProgressText.text = $"{(int)(LoadingSlider.value * 100)}%";

            // 씬 로딩 완료되었다면 로비로 전환하고 코루틴 종료
            if (m_AsyncOperation.progress >= 0.9f)      // allowSceneActivation의 비활성화로 인해 90퍼센트 이후에 멈추므로
            {
                UIManager.Instance.Fade(Color.black, 0.0f, 1.0f, 0.5f, 0.0f, false, () =>
                {
                    m_AsyncOperation.allowSceneActivation = true;
                });
                yield break;
            }

            yield return null;
        }
    }
}
