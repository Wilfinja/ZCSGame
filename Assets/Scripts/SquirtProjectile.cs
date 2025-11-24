using UnityEngine;

public class SquirtProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f;    // How long the projectile exists
    [SerializeField] private float hitAnimDuration = 1f; // How long the hit animation lasts

    public int damageAmount = 5;
    public Collider2D hitCollider;
    public GameObject hitAnim;

    private Animator animator;

    void Start()
    {
        hitAnim.SetActive(false);

        // Destroy the projectile after lifetime seconds
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Robot"))
        {
            // Deal damage and flash
            other.GetComponent<EnemyStats>().TakeDamage(damageAmount / 2);
            other.GetComponent<DamageFlash>().Flash();

            // Play hit animation
            PlayHitAnimation();

            // Destroy projectile (but animation will survive)
            Destroy(gameObject, .2f);
        }
        else if (other.CompareTag("Environment"))
        {
            // Play hit animation
            PlayHitAnimation();

            // Destroy projectile (but animation will survive)
            Destroy(gameObject, .2f);
        }
    }

    private void PlayHitAnimation()
    {
        if (hitAnim != null)
        {
            // Disable the collider so projectile doesn't hit multiple times
            hitCollider.enabled = false;

            // Unparent the hit animation so it survives when projectile is destroyed
            hitAnim.transform.SetParent(null);

            // Activate the animation GameObject
            hitAnim.SetActive(true);

            // Get the SquirtSplode component and call splode()
            SquirtSplode splodeScript = hitAnim.GetComponent<SquirtSplode>();
            if (splodeScript != null)
            {
                splodeScript.splode();
            }

            // Destroy the hit animation after it finishes playing
            Destroy(hitAnim, hitAnimDuration);
        }
    }
}
