using UnityEngine;

[System.Serializable]
public enum BubbleColor
{
    NONE = -1,

    RED = 0,
    YELLOW,
    BLUE,
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
    private Vector3 m_LastVelocity;

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_Collider2D = GetComponent<Collider2D>();  
    }

    private void Update()
    {
        m_LastVelocity = m_Rigidbody2D.linearVelocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (gameObject.tag == "ShootingBubble" && collision.gameObject.tag == "Border")
        {
            Vector2 direction = Vector2.Reflect(m_LastVelocity.normalized, collision.contacts[0].normal);
            m_Rigidbody2D.linearVelocity = direction * 10.0f;
        }
        else if (gameObject.tag == "ShootingBubble" && collision.gameObject.tag == "OnGridBubble")
            GridManager.Instance.AttachToGrid(gameObject);
    }
}
