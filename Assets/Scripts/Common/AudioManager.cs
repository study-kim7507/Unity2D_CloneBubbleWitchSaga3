using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public enum BGM
{
    LOBBY,
    STAGE,
}
public enum SFX
{
    UI_BUTTON_CLICK,

    STAGE_BUBBLE_POP,
    STAGE_BUBBLE_ATTACK,
    STAGE_BUBBLE_SPAWN,
}

public class AudioManager : SingletonBehaviour<AudioManager>
{
    [Header("BGM Audio Clips")]
    [SerializeField] private List<AudioClip> m_BgmAudioClips;
    private Dictionary<BGM, AudioClip> m_BgmAudioClipsDict = new Dictionary<BGM, AudioClip>();
    private ObjectPool<GameObject> m_BgmAudioSourcePool;
    private GameObject m_CurrentPlayingBGM;

    [Header("SFX Audio Clips")]
    [SerializeField] private List<AudioClip> m_SfxAudioClips = new List<AudioClip>();
    private Dictionary<SFX, AudioClip> m_SfxAudioClipsDict = new Dictionary<SFX, AudioClip>();
    private ObjectPool<GameObject> m_SfxAudioSourcePool;

    private void Awake()
    {
        Init();
    }

    protected override void Init()
    {
        base.Init();

        m_BgmAudioSourcePool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject go = new GameObject("BGM AudioSource");
                go.transform.SetParent(transform);
                go.AddComponent<AudioSource>();
                go.SetActive(false);
                return go;
            },
            actionOnGet: (go) => go.SetActive(true),
            actionOnRelease: (go) => go.SetActive(false),
            defaultCapacity: 1
            );

        for (int i = 0; i < m_BgmAudioClips.Count && i < System.Enum.GetValues(typeof(BGM)).Length; i++)
            m_BgmAudioClipsDict[(BGM)i] = m_BgmAudioClips[i];

        m_SfxAudioSourcePool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject go = new GameObject("SFX AudioSource");
                go.transform.SetParent(transform);
                go.AddComponent<AudioSource>();
                go.SetActive(false);
                return go;
            },
            actionOnGet: (go) => go.SetActive(true),
            actionOnRelease: (go) => go.SetActive(false),
            defaultCapacity: 10
            );

        for (int i = 0; i < m_SfxAudioClips.Count && i < System.Enum.GetValues(typeof(SFX)).Length; i++)
            m_SfxAudioClipsDict[(SFX)i] = m_SfxAudioClips[i];
    }

    public void PlayBGM(BGM bgm, float volume = 1.0f)
    {
        GameObject audioSourceGO = m_BgmAudioSourcePool.Get();
        AudioSource audioSource = audioSourceGO.GetComponent<AudioSource>();

        if (audioSource != null && m_BgmAudioClipsDict.TryGetValue(bgm, out AudioClip clip))
        {
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.volume = volume;
            audioSource.Play();
            m_CurrentPlayingBGM = audioSourceGO;
        }
    }

    public void StopBGM()
    {
        m_BgmAudioSourcePool.Release(m_CurrentPlayingBGM);
    }

    public void PlaySFX(SFX sfx)
    {
        GameObject audioSourceGO = m_SfxAudioSourcePool.Get();
        AudioSource audioSource = audioSourceGO.GetComponent<AudioSource>();

        if (audioSource != null && m_SfxAudioClipsDict.TryGetValue(sfx, out AudioClip clip))
        {
            audioSource.PlayOneShot(clip);
            StartCoroutine(StopSFX(audioSourceGO, clip.length));
        }
    }

    public IEnumerator StopSFX(GameObject audioSourceGO, float audioClipLength)
    {
        yield return new WaitForSeconds(audioClipLength);
        m_SfxAudioSourcePool.Release(audioSourceGO);
    }
}
