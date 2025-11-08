using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class Shooter : MonoBehaviour
{
    [Header("버블 프리팹")]
    [SerializeField] private Transform m_CurrentShootingBubbleSpawnPos;
    [SerializeField] private Transform m_NextShootingBubbleSpawnPos;
    private GameObject m_CurrentShootingBubble = null;
    private GameObject m_NextShootingBubble = null;

    [Header("텍스트")]
    [SerializeField] private TMP_Text m_RemainingBubbleAmount;

    private LineRenderer m_LineRenderer;

    private Vector2 m_TouchStartPos;
    private Vector2 m_TouchCurrentPos;
    private bool m_IsDragging = false;

    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
        UpdateRemainingBubbleAmountText();
        StageManager.Instance.OnRemainingBubbleAmountChanged += UpdateRemainingBubbleAmountText;
    }

    private void Update()
    {
        ProcessInput();
    }

    private void Init()
    {
        m_LineRenderer = GetComponent<LineRenderer>();

        m_CurrentShootingBubble = StageManager.Instance.BarrowFromPoolShootingBubble(m_CurrentShootingBubbleSpawnPos.position, transform);
        m_NextShootingBubble = StageManager.Instance.BarrowFromPoolShootingBubble(m_NextShootingBubbleSpawnPos.position, transform);
    }

    private void ProcessInput()
    {
        if (StageManager.Instance.CanShoot == false)
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

            Vector2 direction = (m_TouchStartPos - m_TouchCurrentPos).normalized;
            List<Vector2> path = GetShootPath(m_CurrentShootingBubbleSpawnPos.position, direction);
            if (path == null)
            {
                ClearShootPathLine();
                GridManager.Instance.DeactivateGlowBubble();
            }
        }
        else if (Input.GetMouseButtonUp(0) && m_IsDragging)
        {
            m_IsDragging = false;
            m_TouchCurrentPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Vector2 direction = (m_TouchStartPos - m_TouchCurrentPos).normalized;
            List<Vector2> path = GetShootPath(m_CurrentShootingBubbleSpawnPos.position, direction);
            if (path != null)
                StartCoroutine(ShootBubble(path));
                
            ClearShootPathLine();
            GridManager.Instance.DeactivateGlowBubble();
        }
    }

    private List<Vector2> GetShootPath(Vector2 startPos, Vector2 direction)
    {
        List<Vector2> path = new List<Vector2>();
        List<Vector3> pathForLineRenderer = new List<Vector3>();

        RaycastHit2D firstHit = Physics2D.Raycast(startPos + direction * (3.0f / 2.0f), direction, Mathf.Infinity);

        // 아무것도 안부딪히는 경로인 경우
        if (firstHit.collider == null)
        {
            ClearShootPathLine();
            return null;
        }
            
        // 바로 그리드의 버블에 부딪히는 경우
        if (firstHit.collider.CompareTag("OnGridBubble"))
        {
            Bubble bubble = firstHit.collider.GetComponent<Bubble>();
            path.Add(GridManager.Instance.ActivateGlowBubble(bubble.rowIdx, bubble.colIdx, firstHit.point));
            pathForLineRenderer.Add(firstHit.point);
            DrawShootPathLine(pathForLineRenderer);
            return path;
        }

        // 보더에 1차 부딪히는 경우
        else if (firstHit.collider.CompareTag("Border"))
        {
            Vector2 reflectDir = Vector2.Reflect(direction, firstHit.normal);
            RaycastHit2D secondHit = Physics2D.Raycast(firstHit.point + reflectDir * 0.01f, reflectDir, Mathf.Infinity);

            if (secondHit.collider == null)
            {
                ClearShootPathLine();
                return null;
            }
                
            path.Add(firstHit.point);
            pathForLineRenderer.Add(firstHit.point);
            if (secondHit.collider.CompareTag("OnGridBubble"))
            {
                Bubble bubble = secondHit.collider.GetComponent<Bubble>();
                path.Add(GridManager.Instance.ActivateGlowBubble(bubble.rowIdx, bubble.colIdx, secondHit.point));
                pathForLineRenderer.Add(secondHit.point);
                DrawShootPathLine(pathForLineRenderer);
                return path;
            }
        }

        GridManager.Instance.DeactivateGlowBubble();
        return null;
    }

    private void DrawShootPathLine(List<Vector3> path)
    {
        if (path == null || path.Count == 0)
        {
            ClearShootPathLine();
            return;
        }

        m_LineRenderer.positionCount = path.Count + 1;
        m_LineRenderer.SetPosition(0, m_CurrentShootingBubble.transform.position);
        for (int i = 0; i < path.Count; i++)
            m_LineRenderer.SetPosition(i + 1, path[i]);

        Bubble bubble = m_CurrentShootingBubble.GetComponent<Bubble>();
        BubbleColor bubbleColor = bubble.BubbleColor;
        SetShootPathLineColor(bubbleColor);
    }

    private void ClearShootPathLine()
    {
        m_LineRenderer.positionCount = 0;
    }

    private void SetShootPathLineColor(BubbleColor bubbleColor)
    {
        m_LineRenderer.material = new Material(Shader.Find("Sprites/Default")); // 필수
        Gradient gradient = new Gradient();
        Color color = Color.white;
        switch (bubbleColor)
        {
            case BubbleColor.RED: color = Color.red; break;
            case BubbleColor.YELLOW: color = Color.yellow; break;
            case BubbleColor.BLUE: color = Color.blue; break;
        }
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        m_LineRenderer.colorGradient = gradient;
    }

    private IEnumerator ShootBubble(List<Vector2> path)
    {
        StageManager.Instance.CanShoot = false;
        StageManager.Instance.RemainingBubbleAmount--;

        Bubble bubble = m_CurrentShootingBubble.GetComponent<Bubble>();
        bubble.DeactivateGlowEffect();
        yield return StartCoroutine(bubble.MoveAlongPath(path));

        m_NextShootingBubble.transform.position = m_CurrentShootingBubbleSpawnPos.position;
        m_CurrentShootingBubble = m_NextShootingBubble;
        m_NextShootingBubble = StageManager.Instance.BarrowFromPoolShootingBubble(m_NextShootingBubbleSpawnPos.position, transform);
    }

    private void SwapBubble()
    {
        if (m_CurrentShootingBubble == null || m_NextShootingBubble == null)
            return;

        float duration = 0.5f;
        float arcHeight = 1f;

        Vector3 start1 = m_CurrentShootingBubble.transform.position;
        Vector3 end1 = m_NextShootingBubbleSpawnPos.position;
        Vector3 mid1 = (start1 + end1) / 2 + Vector3.up * arcHeight;

        Vector3 start2 = m_NextShootingBubble.transform.position;
        Vector3 end2 = m_CurrentShootingBubbleSpawnPos.position;
        Vector3 mid2 = (start2 + end2) / 2 + Vector3.up * arcHeight;

        Sequence seq = DOTween.Sequence();
        seq.Join(m_CurrentShootingBubble.transform.DOPath(new Vector3[] { start1, mid1, end1 }, duration, PathType.CatmullRom));
        seq.Join(m_NextShootingBubble.transform.DOPath(new Vector3[] { start2, mid2, end2 }, duration, PathType.CatmullRom));
        seq.OnComplete(() =>
        {
            GameObject temp = m_CurrentShootingBubble;
            m_CurrentShootingBubble = m_NextShootingBubble;
            m_NextShootingBubble = temp;
        });
    }

    private void UpdateRemainingBubbleAmountText()
    {
        m_RemainingBubbleAmount.text = StageManager.Instance.RemainingBubbleAmount.ToString();
    }
}
