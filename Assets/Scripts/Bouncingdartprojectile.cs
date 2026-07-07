using UnityEngine;

/// <summary>
/// A dart fired by the boss that reflects off a wall exactly once,
/// then travels in the reflected direction until it hits something or expires.
/// Uses manual raycast reflection rather than physics bounciness
/// so the single-bounce limit is reliable.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BouncingDartProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float lifetime = 4f;
    public int damageAmount = 8;
    public LayerMask wallLayer;         // Assign your Environment layer here

    private Rigidbody2D rb;
    private Collider2D col;
    private Vector2 currentVelocity;
    private bool hasBounced = false;
    private bool hasHit = false;
    private float spawnTime;

    // ─────────────────────────────────────────────────────────────────────────
    //  Initialisation (called by RobotCEOBoss after Instantiate)
    // ─────────────────────────────────────────────────────────────────────────

    public void Initialize(Vector2 direction, float speed)
    {
        currentVelocity = direction.normalized * speed;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // We drive movement manually so physics won't interfere
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        spawnTime = Time.time;

        // Rotate sprite to face travel direction
        float angle = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        if (hasHit) return;

        Vector2 origin = rb.position;
        float stepDistance = currentVelocity.magnitude * Time.fixedDeltaTime;
        Vector2 direction = currentVelocity.normalized;

        RaycastHit2D wallHit = Physics2D.Raycast(origin, direction, stepDistance + 0.05f, wallLayer);

        if (wallHit.collider != null && !hasBounced)
        {
            rb.position = wallHit.point - direction * 0.02f;
            currentVelocity = Vector2.Reflect(currentVelocity, wallHit.normal);
            hasBounced = true;
        }
        else if (wallHit.collider != null && hasBounced)
        {
            DestroyDart();
            return;
        }

        RaycastHit2D playerHit = Physics2D.Raycast(origin, direction, stepDistance + 0.05f);
        if (playerHit.collider != null && playerHit.collider.CompareTag("Player"))
        {
            HitPlayer(playerHit.collider);
            return;
        }

        // ── Always update rotation to match current travel direction ─────────
        float angle = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        rb.MovePosition(origin + currentVelocity * Time.fixedDeltaTime);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Collision fallback (trigger collider for player overlap)
    // ─────────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        if (other.CompareTag("Player"))
        {
            HitPlayer(other);
        }
        else if (other.CompareTag("Environment") && hasBounced)
        {
            DestroyDart();
        }
    }

    private void HitPlayer(Collider2D playerCol)
    {
        PlayerStats ps = playerCol.GetComponent<PlayerStats>();
        DamageFlash df = playerCol.GetComponent<DamageFlash>();

        if (ps != null) ps.TakeDamage(damageAmount);
        if (df != null) df.Flash();

        DestroyDart();
    }

    private void DestroyDart()
    {
        hasHit = true;
        // Disable collider immediately to prevent double-hits
        if (col != null) col.enabled = false;
        Destroy(gameObject, 0.05f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = hasBounced ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.15f);
    }
}
