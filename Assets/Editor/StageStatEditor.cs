using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StageStat))]
public class StageStatEditor : Editor
{
    private int m_rowCount = 0;
    private int m_columnCount = 0;

    public override void OnInspectorGUI()
    {
        StageStat stageData = (StageStat)target;

        GUILayout.Label("초기 그리드 설정", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // 행 / 열 개수 조절
        m_rowCount = EditorGUILayout.IntField("Row Count", stageData.GridData.Count > 0 ? stageData.GridData.Count : m_rowCount);
        m_rowCount = Mathf.Clamp(m_rowCount, 0, 15);
        UpdateRowCount(stageData, m_rowCount);

        m_columnCount = EditorGUILayout.IntField("Column Count", stageData.GridData.Count > 0 ? stageData.GridData[0].Columns.Count : m_columnCount);
        m_columnCount = Mathf.Clamp(m_columnCount, 0, 15);
        UpdateColumnCount(stageData, m_columnCount);

        GUILayout.Space(10);
        DrawGrid(stageData);

        GUILayout.Space(10);
        GUILayout.Label("스테이지 정보", EditorStyles.boldLabel);
        stageData.RemaingBossHealth = EditorGUILayout.FloatField("초기 보스 체력", stageData.RemaingBossHealth);
        stageData.RemainingBubbleAmount = EditorGUILayout.IntField("초기 버블 수", stageData.RemainingBubbleAmount);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(stageData);
        }
    }

    private void UpdateRowCount(StageStat currentStageData, int rowCount)
    {
        while (currentStageData.GridData.Count < rowCount)
            currentStageData.GridData.Add(new Row());

        while (currentStageData.GridData.Count > rowCount)
            currentStageData.GridData.RemoveAt(currentStageData.GridData.Count - 1);
    }

    private void UpdateColumnCount(StageStat currentStageData, int columnCount)
    {
        foreach (var row in currentStageData.GridData)
        {
            while (row.Columns.Count < columnCount)
                row.Columns.Add(new GridCell());

            while (row.Columns.Count > columnCount)
                row.Columns.RemoveAt(row.Columns.Count - 1);
        }
    }

    private void DrawGrid(StageStat currentStageData)
    {
        GUILayout.Space(10);
        GUILayout.Label("그리드 미리보기", EditorStyles.boldLabel);

        for (int row = 0; row < currentStageData.GridData.Count; row++)
        {
            GUILayout.BeginHorizontal();

            // even-r 오프셋: 짝수 행은 오른쪽으로 살짝 밀기
            if (row % 2 == 0)
                GUILayout.Space(15);

            for (int col = 0; col < currentStageData.GridData[row].Columns.Count; col++)
            {
                GridCell currentCell = currentStageData.GridData[row].Columns[col];
                
                Color prevColor = GUI.backgroundColor;

                switch (currentCell.CellType)
                {
                    case GridCellType.EMPTY:
                        GUI.backgroundColor = Color.gray;
                        break;

                    case GridCellType.BUBBLE:
                        GUI.backgroundColor = Color.yellow;
                        break;

                    case GridCellType.BUBBLE_SPAWNER:
                        GUI.backgroundColor = Color.white;
                        break;
                    case GridCellType.BOSS:
                        GUI.backgroundColor = Color.black;
                        break;
                }

                // 버튼 클릭 시, CellTpye이 변경
                if (GUILayout.Button("", GUILayout.Width(25), GUILayout.Height(25)))
                {
                    switch (currentCell.CellType)
                    {
                        case GridCellType.EMPTY:
                            currentCell.CellType = GridCellType.BUBBLE;
                            break;

                        case GridCellType.BUBBLE:
                            currentCell.CellType = GridCellType.BUBBLE_SPAWNER;
                            break;

                        case GridCellType.BUBBLE_SPAWNER:
                            currentCell.CellType = GridCellType.BOSS;
                            break;
                        case GridCellType.BOSS:
                            currentCell.CellType = GridCellType.EMPTY;
                            break;
                    }
                }

                GUI.backgroundColor = prevColor;
            }

            GUILayout.EndHorizontal();
        }
    }
}
