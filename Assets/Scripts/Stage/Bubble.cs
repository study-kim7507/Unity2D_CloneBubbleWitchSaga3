using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum BubbleColor
{
    NONE = -1,

    RED = 0,
    YELLOW,
    BLUE,

    WILDCARD,
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bubble : MonoBehaviour
{
    // 그리드 위에서의 위치
    [HideInInspector] public int rowIdx;
    [HideInInspector] public int colIdx;

    public BubbleColor BubbleColor;

    private Rigidbody2D m_Rigidbody2D;
    private Collider2D m_Collider2D;

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_Collider2D = GetComponent<Collider2D>();  
    }

    public IEnumerator MoveAlongPath(List<Vector2> path, float speed = 10.0f)
    {
        foreach (var targetPos in path)
        {
            Vector2 startPos = m_Rigidbody2D.position;
            float distance = Vector2.Distance(startPos, targetPos);
            float travelTime = distance / speed;
            float elapsed = 0f;

            while (elapsed < travelTime)
            {
                elapsed += Time.deltaTime;
                m_Rigidbody2D.MovePosition(Vector2.Lerp(startPos, targetPos, elapsed / travelTime));
                yield return null;
            }

            m_Rigidbody2D.MovePosition(targetPos);
        }

        // 이동 완료 후 그리드에 부착
        GridManager.Instance.AttachToGrid(gameObject);
    }
}
