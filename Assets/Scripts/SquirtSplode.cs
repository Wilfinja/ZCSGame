using UnityEngine;

public class SquirtSplode : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void splode()
    {
        animator.Play("Explosion");
    }
}
