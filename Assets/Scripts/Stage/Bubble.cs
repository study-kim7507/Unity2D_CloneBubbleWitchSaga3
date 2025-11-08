using DG.Tweening;
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

[RequireComponent(typeof(Collider2D))]
public class Bubble : MonoBehaviour
{
    // 그리드 위에서의 위치
    [HideInInspector] public int RowIdx;
    [HideInInspector] public int ColIdx;

    // 해당 버블이 보스에게 데미지를 입힐 수 있는지 여부
    [HideInInspector] public bool CanAttackable;
    public GameObject SparkVfxGO;

    public BubbleColor BubbleColor;
    public GameObject GlowEffect;

    private Collider2D m_Collider2D;

    private void Awake()
    {
        m_Collider2D = GetComponent<Collider2D>();  
    }

    private void OnDisable()
    {
        CanAttackable = false;
        SparkVfxGO.SetActive(false);
    }

    public void ActivateGlowEffect()
    {
        GlowEffect.SetActive(true);
    }

    public void DeactivateGlowEffect()
    {
        GlowEffect.SetActive(false);
    }

    public IEnumerator MoveAlongPath(List<Vector2> paths)
    {
        Sequence sequence = DOTween.Sequence();

        foreach (Vector2 path in paths)
        {
            Vector3 currentPos = transform.position;
            Vector3 targetPos = path;

            sequence.Append(transform.DOMove(targetPos, 0.2f).SetEase(Ease.Linear));
        }
        sequence.OnComplete(() => GridManager.Instance.AttachToGrid(gameObject));
        yield return sequence.WaitForCompletion();
    }
}
