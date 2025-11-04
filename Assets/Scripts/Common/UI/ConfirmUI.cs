using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ConfirmType
{
    OK,
    OK_CANCLE,
}

public class ConfirmUIData : BaseUIData
{
    public ConfirmType ConfirmType;
    public string TitleText;
    public string DesciptionText;
    public string OKButtonText;
    public Action OnClickOKButton;
    public string CancelButtonText;
    public Action OnClickCancleButton;
}

public class ConfirmUI : BaseUI
{
    public TMP_Text TitleText;
    public TMP_Text DesciptionText;
    public Button OKButton;
    public Button CancelButton;
    public TMP_Text OKButtonText;
    public TMP_Text CancelButtonText;

    private ConfirmUIData m_ConfirmUIData;
    private Action m_OnClickOKButton;
    private Action m_OnClickCancleButton;

    public override void SetInfo(BaseUIData uiData)
    {
        base.SetInfo(uiData);

        m_ConfirmUIData = uiData as ConfirmUIData;

        TitleText.text = m_ConfirmUIData.TitleText;
        DesciptionText.text = m_ConfirmUIData.DesciptionText;
        OKButtonText.text = m_ConfirmUIData.OKButtonText;
        m_OnClickOKButton = m_ConfirmUIData.OnClickOKButton;
        CancelButtonText.text = m_ConfirmUIData.CancelButtonText;
        m_OnClickCancleButton = m_ConfirmUIData.OnClickCancleButton;

        OKButton.gameObject.SetActive(true);
        CancelButton.gameObject.SetActive(m_ConfirmUIData.ConfirmType == ConfirmType.OK_CANCLE);
    }

    public void OnClickOKButton()
    {
        m_OnClickOKButton?.Invoke();
        m_OnClickOKButton = null;
        CloseUI();
    }

    public void OnClickCancelButton()
    {
        m_OnClickCancleButton?.Invoke();
        m_OnClickCancleButton = null;
        CloseUI();
    }
}
