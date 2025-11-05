using System.Collections.Generic;
using UnityEngine;

public class GridManager : SingletonBehaviour<GridManager>
{
    private GridMaker m_GridMaker;
    private List<Row> m_Grid = new List<Row>();

    private void Awake()
    {
        m_IsDestroyOnLoad = true;           // 씬 전환 시 삭제
        Init();

        m_GridMaker = GetComponent<GridMaker>();
    }

    public void Initialize()
    {
        Logger.Log($"{GetType()}::Initialize()");

        m_GridMaker.GenerateGrid(m_Grid);
    }
}
