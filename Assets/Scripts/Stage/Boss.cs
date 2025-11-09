using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Boss : MonoBehaviour
{
    private Animator m_Animator;

    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
    }

    // 애니메이션을 재생하고, 해당 애니메이션이 끝날 때까지 대기하도록 하는 코루틴 함수
    public IEnumerator PlayAnim(string triggerName)
    {
        m_Animator.SetTrigger(triggerName);

        yield return null;

        AnimatorStateInfo stateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);

        while (m_Animator.GetCurrentAnimatorStateInfo(0).IsName(triggerName) &&
               m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            yield return null;
        }
    }
}
