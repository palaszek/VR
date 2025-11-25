using UnityEngine;

public class OuchOnHit : MonoBehaviour
{
    [Header("Ustaw w Inspectorze (opcjonalne)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string triggerName = "Ouch";

    private void Reset()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
    }
}
