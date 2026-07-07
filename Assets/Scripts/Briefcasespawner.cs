using System.Collections;
using UnityEngine;

/// <summary>
/// Attached at runtime to each briefcase the boss throws.
/// On landing it waits a few seconds then spawns a random enemy.
/// If the player picks it up and throws it back at the boss it deals damage.
/// </summary>
public class BriefcaseSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject vacuumPrefab;
    public GameObject vacuumShooterPrefab;
    public GameObject grabberRobotPrefab;

    [Header("Settings")]
    public float spawnDelay = 2.5f;
    public float briefcaseDamage = 15f;     // Damage when thrown back at boss
    public int damageAmount = 8;            // Damage to player on direct hit

    [HideInInspector] public Transform bossTransform;

    private bool hasLanded = false;
    private bool hasSpawned = false;
    private Rigidbody2D rb;

    private bool collisionEnabled = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Small delay so the briefcase clears the boss before collisions matter
        Invoke(nameof(EnableCollision), 0.15f);
    }

    private void EnableCollision() => collisionEnabled = true;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collisionEnabled) return;

        // ── Hit the player directly ──────────────────────────────────────────
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerStats ps = collision.gameObject.GetComponent<PlayerStats>();
            DamageFlash df = collision.gameObject.GetComponent<DamageFlash>();
            if (ps != null) ps.TakeDamage(damageAmount);
            if (df != null) df.Flash();
            return;
        }

        // ── Hit the boss (thrown back by player) ────────────────────────────
        if (bossTransform != null && collision.gameObject.transform == bossTransform)
        {
            RobotCEOBoss boss = bossTransform.GetComponent<RobotCEOBoss>();
            if (boss != null)
            {
                boss.TakeBriefcaseDamage(briefcaseDamage);
                Destroy(gameObject);
                return;
            }
        }

        // ── Hit the environment (floor / wall) ──────────────────────────────
        if (!hasLanded && collision.gameObject.CompareTag("Environment"))
        {
            hasLanded = true;

            // Stop moving
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Static;
            }

            if (!hasSpawned)
                StartCoroutine(SpawnEnemyAfterDelay());
        }
    }

    private IEnumerator SpawnEnemyAfterDelay()
    {
        hasSpawned = true;

        // Visual warning — pulse the briefcase
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        while (elapsed < spawnDelay)
        {
            if (sr != null)
                sr.color = elapsed % 0.4f < 0.2f ? Color.red : Color.white;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Pick a random enemy to spawn
        GameObject prefabToSpawn = PickRandomEnemyPrefab();

        if (prefabToSpawn != null)
        {
            Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("BriefcaseSpawner: No enemy prefabs assigned!");
        }

        Destroy(gameObject);
    }

    private GameObject PickRandomEnemyPrefab()
    {
        // Build a list of only the prefabs that were actually assigned
        System.Collections.Generic.List<GameObject> available =
            new System.Collections.Generic.List<GameObject>();

        if (vacuumPrefab != null) available.Add(vacuumPrefab);
        if (vacuumShooterPrefab != null) available.Add(vacuumShooterPrefab);
        if (grabberRobotPrefab != null) available.Add(grabberRobotPrefab);

        if (available.Count == 0) return null;

        return available[Random.Range(0, available.Count)];
    }
}
