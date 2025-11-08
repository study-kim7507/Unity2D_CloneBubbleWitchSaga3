using System.Collections;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Boss : MonoBehaviour
{
    private Animator m_Animator;

    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
    }

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
