using UnityEngine;

public class SquirtProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f;    // How long the projectile exists
    [SerializeField] private float hitAnimDuration = 1f; // How long the hit animation lasts

    public int damageAmount = 5;
    public Collider2D hitCollider;
    public GameObject hitAnim;

    private Animator animator;

    void Awake()
    {
        hitAnim.SetActive(false);

        // Destroy the projectile after lifetime seconds
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // ── Boss support ─────────────────────────────────────────────────────
        if (other.CompareTag("Robot"))
        {
            RobotCEOBoss boss = other.GetComponent<RobotCEOBoss>();
            if (boss != null)
            {
                boss.TakeDamage(damageAmount);
                DamageFlash df = other.GetComponent<DamageFlash>();
                if (df != null) df.Flash();
                hitCollider.enabled = false;
                PlayHitAnimation();
                Destroy(gameObject, 0.05f);
                return;
            }

            // Regular robot enemy
            other.GetComponent<EnemyStats>().TakeDamage(damageAmount);
            other.GetComponent<DamageFlash>().Flash();
            hitCollider.enabled = false;
            PlayHitAnimation();
            Destroy(gameObject, .2f); 
            return;
        }
        // ── Grabber robot support ────────────────────────────────────────
        else if (other.CompareTag("Grabber"))
        {
            Grabberrobot grabber = other.GetComponent<Grabberrobot>();
            if (grabber != null)
            {
                grabber.TakeDamage(damageAmount);
            }

            // DamageFlash is called inside GrabberRobot.TakeDamage, but call
            // it here too as a fallback in case the component is missing
            DamageFlash flash = other.GetComponent<DamageFlash>();
            if (flash != null) flash.Flash();

            hitCollider.enabled = false;
            PlayHitAnimation();
            Destroy(gameObject, .05f);
        }
        // ─────────────────────────────────────────────────────────────────────
        else if (other.CompareTag("Vacuum"))
        {
            other.GetComponent<VacuumShooter>().TakeDamage(damageAmount);
            other.GetComponent<DamageFlash>().Flash();
            hitCollider.enabled = false;

            PlayHitAnimation();

            Destroy(gameObject, .05f);
        }
        else if (other.CompareTag("Environment"))
        {
            // Play hit animation
            PlayHitAnimation();

            // Destroy projectile (but animation will survive)
            Destroy(gameObject, .05f);
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
