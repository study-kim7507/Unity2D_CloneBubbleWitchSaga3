using System;
using UnityEngine;

public class BaseUIData
{
    public Action OnShow;
    public Action OnClose;
}

public class BaseUI : MonoBehaviour
{
    public Animation UIOpenAnim;

    private Action m_OnShow;
    private Action m_OnClose;

    public virtual void Init(Transform anchor)
    {
        Logger.Log($"{GetType()}::Init");

        m_OnShow = null;
        m_OnClose = null;

        transform.SetParent(anchor);

        var rectTransform = GetComponent<RectTransform>();
        rectTransform.localPosition = Vector3.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.one;
    }

    public virtual void SetInfo(BaseUIData uiData)
    {
        Logger.Log($"{GetType()}::SetInfo");

        m_OnShow = uiData.OnShow;
        m_OnClose = uiData.OnClose;
    }

    public virtual void ShowUI()
    {
        if (UIOpenAnim)
        {
            UIOpenAnim.Play();
        }

        m_OnShow?.Invoke();
        m_OnShow = null;
    }

    public virtual void CloseUI(bool isCloseAll = false)
    {
        if (!isCloseAll)
        {
            m_OnClose?.Invoke();
        }
        m_OnClose = null;

        UIManager.Instance.CloseUI(this);
    }

    public virtual void OnClickCloseButton()
    {
        // AudioManager.Instance.PlaySFX(SFX.ui_button_click);
        CloseUI();
    }
}
