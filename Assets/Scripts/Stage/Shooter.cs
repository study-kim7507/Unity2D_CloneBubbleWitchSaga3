using System.Collections;
using UnityEngine;


public class Shooter : MonoBehaviour
{
    [SerializeField] private Transform m_CurrentShootingBubbleSpawnPos;
    [SerializeField] private Transform m_NextShootingBubbleSpawnPos;
    private GameObject m_CurrentShootingBubble = null;
    private GameObject m_NextShootingBubble = null;

    private bool m_CanShoot = false;

    private Vector2 m_TouchStartPos;
    private Vector2 m_TouchCurrentPos;
    private bool m_IsDragging = false;

    private void Awake()
    {
        Init();
    }

    private void Update()
    {
        ProcessInput();
    }

    private void Init()
    {
        m_CurrentShootingBubble = StageManager.Instance.SpawnShootingBubble(m_CurrentShootingBubbleSpawnPos.position, transform);
        m_NextShootingBubble = StageManager.Instance.SpawnShootingBubble(m_NextShootingBubbleSpawnPos.position, transform);
        m_CanShoot = true;
    }

    private void ProcessInput()
    {
        if (m_CanShoot == false || m_CurrentShootingBubble == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition); worldPos.z = 0.0f;
            float dist = 0.0f;

            dist = Vector3.Distance(worldPos, m_NextShootingBubbleSpawnPos.position);
            if (dist < 0.5f)
            {
                SwapBubble();
                return;
            }

            dist = Vector3.Distance(worldPos, m_CurrentShootingBubbleSpawnPos.position);
            if (dist < 0.5f)
            {
                m_TouchStartPos = worldPos;
                m_IsDragging = true;
            }
        }
        else if (Input.GetMouseButton(0) && m_IsDragging)
        {
            m_TouchCurrentPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && m_IsDragging)
        {
            m_IsDragging = false;
            m_TouchCurrentPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Vector2 direction = (m_TouchStartPos - m_TouchCurrentPos).normalized;
            if (direction.magnitude < 0.2f)
                return;

            if (CanShootDirection(m_CurrentShootingBubbleSpawnPos.position, direction))
                StartCoroutine(ShootBubble(direction));
        }
    }

    private bool CanShootDirection(Vector2 startPos, Vector2 direction)
    {
        RaycastHit2D firstHit = Physics2D.Raycast(startPos + direction * (1.705f / 2.0f), direction, Mathf.Infinity);
        Debug.DrawRay(startPos, direction * 20f, Color.yellow, 10f);

        // 아무것도 안부딪히는 경로인 경우
        if (firstHit.collider == null)
            return false;

        // 바로 그리드의 버블에 부딪히는 경우
        if (firstHit.collider.CompareTag("OnGridBubble"))
            return true;


        // 보더에 1차 부딪히는 경우
        else if (firstHit.collider.CompareTag("Border"))
        {
            Vector2 reflectDir = Vector2.Reflect(direction, firstHit.normal);

            RaycastHit2D secondHit = Physics2D.Raycast(firstHit.point + reflectDir * 0.01f, reflectDir, Mathf.Infinity);

            // 보더에 부딪힌 후 반사되어 나가는 경로가 아무것도 안 부딪히는 경로인 경우
            if (secondHit.collider == null)
                return false;

            // 보더에 부딪힌 후 반사되어 나가는 경로에 그리드의 버블이 있는 경우 발사 가능한 경로로 판정
            if (secondHit.collider.CompareTag("OnGridBubble"))
                return true;
        }

        return false;
    }

    private IEnumerator ShootBubble(Vector2 direction)
    {
        m_CanShoot = false;

        Rigidbody2D rb = m_CurrentShootingBubble.GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction * 10.0f;

        yield return new WaitForSeconds(1.0f);

        m_NextShootingBubble.transform.position = m_CurrentShootingBubbleSpawnPos.position;
        m_CurrentShootingBubble = m_NextShootingBubble;
        m_NextShootingBubble = StageManager.Instance.SpawnShootingBubble(m_NextShootingBubbleSpawnPos.position, transform);

        m_CanShoot = true;
    }

    private void SwapBubble()
    {
        if (m_CurrentShootingBubble == null || m_NextShootingBubble == null)
            return;
            
        m_CurrentShootingBubble.transform.position = m_NextShootingBubbleSpawnPos.position;
        m_NextShootingBubble.transform.position = m_CurrentShootingBubbleSpawnPos.position;

        GameObject tempGO = m_CurrentShootingBubble;
        m_CurrentShootingBubble = m_NextShootingBubble;
        m_NextShootingBubble = tempGO;
    }    
}
