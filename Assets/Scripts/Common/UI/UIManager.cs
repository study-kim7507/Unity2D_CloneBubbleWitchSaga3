using System.Collections.Generic;
using UnityEngine;

public class UIManager : SingletonBehaviour<UIManager>
{
    public Transform UICanvasTransform;
    public Transform ClosedUITransform;

    private BaseUI m_FrontUI;                                                                                   // 가장 상단에 열려있는 UI화면을 참조
    private Dictionary<System.Type, GameObject> m_OpenUIPool = new Dictionary<System.Type, GameObject>();       // 현재 활성화 되어있는 (열려있는) UI화면을 보관
    private Dictionary<System.Type, GameObject> m_CloseUIPool = new Dictionary<System.Type, GameObject>();      // 현재 비활성화 되어있는 (닫혀있는) UI화면을 보관 

    private BaseUI GetUI<T>(out bool isAlreadyOpen)
    {
        System.Type uiType = typeof(T);

        BaseUI ui = null;
        isAlreadyOpen = false;

        if (m_OpenUIPool.ContainsKey(uiType))
        {
            ui = m_OpenUIPool[uiType].GetComponent<BaseUI>();
            isAlreadyOpen = true;
        }
        else if (m_CloseUIPool.ContainsKey(uiType))
        {
            ui = m_CloseUIPool[uiType].GetComponent<BaseUI>();
            m_CloseUIPool.Remove(uiType);
        }
        else
        {
            var uiGO = Instantiate(Resources.Load($"UI/{uiType}", typeof(GameObject))) as GameObject;
            ui = uiGO.GetComponent<BaseUI>();
        }

        return ui;
    }

    public void OpenUI<T>(BaseUIData uiData)
    {
        System.Type uiType = typeof(T);

        Logger.Log($"{GetType()}::OpenUI({uiType})");

        bool isAlreadyOpen = false;
        var ui = GetUI<T>(out isAlreadyOpen);

        if (ui == null)
        {
            Logger.LogError($"{uiType} does not exist.");
            return;
        }

        if (isAlreadyOpen)
        {
            Logger.LogError($"{uiType} is already open.");
            return;
        }

        var siblingIndex = UICanvasTransform.childCount;
        ui.Init(UICanvasTransform);
        ui.transform.SetAsFirstSibling();
        ui.gameObject.SetActive(true);
        ui.SetInfo(uiData);
        ui.ShowUI();

        m_FrontUI = ui;
        m_OpenUIPool[uiType] = ui.gameObject;
    }

    public void CloseUI(BaseUI ui)
    {
        System.Type uiType = ui.GetType();

        Logger.Log($"{GetType()}::CloseUI ({uiType})");

        ui.gameObject.SetActive(false);
        m_OpenUIPool.Remove(uiType);
        m_CloseUIPool[uiType] = ui.gameObject;
        ui.transform.SetParent(ClosedUITransform);

        m_FrontUI = null;
        var lastChild = UICanvasTransform.GetChild(UICanvasTransform.childCount - 1);
        if (lastChild)
        {
            m_FrontUI = lastChild.gameObject.GetComponent<BaseUI>();  
        }
    }

    public BaseUI GetActiveUI<T>()
    {
        var uiType = typeof(T);
        return m_OpenUIPool.ContainsKey(uiType) ? m_OpenUIPool[uiType].GetComponent<BaseUI>() : null;
    }

    public bool ExistsOpenUI()
    {
        return m_FrontUI != null;
    }

    public BaseUI GetCurrentFrontUI()
    {
        return m_FrontUI;
    }

    public void CloseCurrentFrontUI()
    {
        m_FrontUI.CloseUI();
    }

    public void CloseAllOpenUI()
    {
        while (m_FrontUI)
        {
            m_FrontUI.CloseUI(true);
        }
    }
}
