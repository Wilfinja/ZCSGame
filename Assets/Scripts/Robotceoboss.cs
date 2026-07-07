using System.Collections;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;

public class RobotCEOBoss : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  Health
    // ─────────────────────────────────────────
    [Header("Boss Health")]
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isEnraged = false;
    private bool isDead = false;

    // ─────────────────────────────────────────
    //  Attack Timing
    // ─────────────────────────────────────────
    [Header("Attack Timing")]
    public float telegraphDuration = 1.5f;   // Warning time before each attack
    public float attackCooldown = 2f;        // Gap between attacks
    [Tooltip("Multiplier applied to all timings when enraged (< 1 = faster)")]
    public float enrageSpeedMultiplier = 0.65f;

    // ─────────────────────────────────────────
    //  Laser
    // ─────────────────────────────────────────
    [Header("Laser Attack")]
    [Tooltip("SpriteLaserRenderer component on the LaserPivot child object.")]
    public SpriteLaserRenderer spriteLaser;
    public Transform laserPivot;             // Child object that rotates; laser fires from here
    public float laserRange = 20f;
    public float laserRotationSpeed = 90f;   // Degrees per second
    public float laserDuration = 4f;
    public int laserDamagePerTick = 5;
    public float laserDamageTickRate = 0.3f;
    public LayerMask laserBlockingLayers;    // Environment + columns

    [Header("Boss Sprite Frames")]
    [Tooltip("The SpriteRenderer on the boss itself (uses Boss.ase).")]
    public SpriteRenderer bossSpriteRenderer;
    [Tooltip("Frame 0 of Boss.ase — idle / normal.")]
    public Sprite bossIdleSprite;
    [Tooltip("Frame 1 of Boss.ase — laser firing pose.")]
    public Sprite bossLaserSprite;

    // ─────────────────────────────────────────
    //  Briefcase
    // ─────────────────────────────────────────
    [Header("Briefcase Attack")]
    public GameObject briefcasePrefab;
    public int briefcasesPerAttack = 2;
    public float briefcaseInterval = 0.8f;   // Time between each briefcase throw
    public float briefcaseForce = 12f;

    // ─────────────────────────────────────────
    //  Dart
    // ─────────────────────────────────────────
    [Header("Dart Attack")]
    public GameObject bouncingDartPrefab;
    public Transform firePoint;
    public int dartsPerBurst = 3;
    public float dartBurstInterval = 0.3f;
    public float dartSpeed = 14f;
    public DartLaserSight dartLaserSightSprite;  // Add this — assign a LineRenderer child of DartShootPoint

        // ─────────────────────────────────────────
        //  Enemy Spawning (briefcase landing)
        // ─────────────────────────────────────────
        [Header("Enemy Spawning")]
    public GameObject vacuumPrefab;
    public GameObject vacuumShooterPrefab;
    public GameObject grabberRobotPrefab;
    public float enemySpawnDelay = 2.5f;     // Seconds after briefcase lands

    // ─────────────────────────────────────────
    //  References
    // ─────────────────────────────────────────
    [Header("References")]
    public Transform player;
    public LayerMask playerLayer;

    // ─────────────────────────────────────────
    //  Visual / Audio Feedback
    // ─────────────────────────────────────────
    [Header("Feedback")]
    public SpriteRenderer spriteRenderer;
    public Color normalColor = Color.white;
    public Color enragedColor = Color.red;
    public Color telegraphColor = Color.yellow;
    public AudioSource audioSource;
    public AudioClip laserWindupClip;
    public AudioClip briefcaseWindupClip;
    public AudioClip dartWindupClip;
    public AudioClip deathClip;

    private Animator animator;

    // ─────────────────────────────────────────
    //  Internal State
    // ─────────────────────────────────────────
    private enum AttackPhase { Laser, Briefcase, Dart }
    private AttackPhase currentPhase = AttackPhase.Laser;
    private bool isAttacking = false;
    private bool laserActive = false;
    private float laserDamageTimer = 0f;

    public float CurrentHealthNormalized => maxHealth > 0f ?Mathf.Clamp01(currentHealth /  maxHealth) : 0f;

    [Header("Death")]
    public CreditsScreen creditsScreen;
    public ExplosionSequence explosionSequence;


    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Laser starts hidden
        if (spriteLaser != null)
            spriteLaser.HideBeam();

        // Boss starts in idle pose
        if (bossSpriteRenderer != null && bossIdleSprite != null)
            bossSpriteRenderer.sprite = bossIdleSprite;
    }

    private void Start()
    {
        FindPlayer();
        StartCoroutine(BossCycle());
    }

    private void FindPlayer()
    {
        if (player != null) return;

        // Try by tag first
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            return;
        }

        // Fallback: find ClickToMove since PlayerStats uses DontDestroyOnLoad
        // and may not be the scene's actual player transform
        ClickToMove ctm = FindFirstObjectByType<ClickToMove>();
        if (ctm != null)
            player = ctm.transform;
    }

    private void Update()
    {
        if (isDead) return;

        // Don't rotate the boss body while the laser is spinning —
        // the laserPivot handles its own rotation during that attack
        if (!laserActive)
            FacePlayer();

        if (laserActive)
        {
            if (spriteLaser != null)
                spriteLaser.UpdateBeam();

            laserDamageTimer -= Time.deltaTime;
            if (laserDamageTimer <= 0f)
            {
                CheckLaserHitPlayer();
                laserDamageTimer = isEnraged
                    ? laserDamageTickRate * enrageSpeedMultiplier
                    : laserDamageTickRate;
            }
        }
    }

    private void FacePlayer()
    {
        if (player == null) return;

        Vector2 direction = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Adjust the offset to match your sprite's natural facing direction:
        // Sprite faces UP   → angle - 90f
        // Sprite faces DOWN → angle + 90f
        // Sprite faces RIGHT → angle
        // Sprite faces LEFT  → angle + 180f
        transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Main Attack Cycle
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator BossCycle()
    {
        // Brief pause before the fight starts
        yield return new WaitForSeconds(1f);

        while (!isDead)
        {
            float speed = isEnraged ? enrageSpeedMultiplier : 1f;

            switch (currentPhase)
            {
                case AttackPhase.Laser:
                    yield return StartCoroutine(TelegraphAttack(laserWindupClip));
                    yield return StartCoroutine(LaserAttack());
                    break;

                case AttackPhase.Briefcase:
                    yield return StartCoroutine(TelegraphAttack(briefcaseWindupClip));
                    yield return StartCoroutine(BriefcaseAttack());
                    break;

                case AttackPhase.Dart:
                    // No separate TelegraphAttack call — DartAttack handles it
                    yield return StartCoroutine(DartAttack());
                    break;
            }

            // Advance to next phase
            currentPhase = (AttackPhase)(((int)currentPhase + 1) % 3);

            yield return new WaitForSeconds(attackCooldown * speed);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Telegraph (warning flash before every attack)
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator TelegraphAttack(AudioClip windupClip)
    {
        float duration = isEnraged
            ? telegraphDuration * enrageSpeedMultiplier
            : telegraphDuration;

        if (windupClip != null && audioSource != null)
            audioSource.PlayOneShot(windupClip);

        // Flash yellow to warn the player
        float elapsed = 0f;
        bool flash = false;
        while (elapsed < duration)
        {
            flash = !flash;
            if (spriteRenderer != null)
                spriteRenderer.color = flash ? telegraphColor : normalColor;

            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = isEnraged ? enragedColor : normalColor;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Attack: Rotating Laser
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator LaserAttack()
    {
        if (laserPivot == null) yield break;

        if (bossSpriteRenderer == null)
            bossSpriteRenderer = GetComponent<SpriteRenderer>();

        if (bossSpriteRenderer != null && bossLaserSprite != null)
            bossSpriteRenderer.sprite = bossLaserSprite;

        laserActive = true;
        laserDamageTimer = 0f;

        float rotSpeed = isEnraged ? laserRotationSpeed / enrageSpeedMultiplier : laserRotationSpeed;

        // Spin exactly 2 full cycles (720 degrees)
        float totalRotation = 0f;
        float targetRotation = 720f;
         
        while (totalRotation < targetRotation && !isDead)
        {
            float step = rotSpeed * Time.deltaTime;
            laserPivot.Rotate(0f, 0f, step);
            totalRotation += Mathf.Abs(step);
            yield return null;
        }

        laserActive = false;
        if (spriteLaser != null) spriteLaser.HideBeam();
        if (bossSpriteRenderer != null && bossIdleSprite != null)
            bossSpriteRenderer.sprite = bossIdleSprite;
    }

    private void CheckLaserHitPlayer()
    {
        if (laserPivot == null) return;

        FindPlayer(); // re-find if null
        if (player == null) return;

        Vector2 origin = laserPivot.position;
        Vector2 direction = -laserPivot.up;

        // Cast against walls first to get the actual beam endpoint
        RaycastHit2D wallHit = Physics2D.Raycast(origin, direction, laserRange, laserBlockingLayers);
        float actualLength = wallHit.collider != null ? wallHit.distance : laserRange;

        // Now check if the player is within that length
        RaycastHit2D playerHit = Physics2D.Raycast(origin, direction, actualLength, playerLayer);

        if (playerHit.collider != null && playerHit.collider.CompareTag("Player"))
        {
            PlayerStats ps = playerHit.collider.GetComponent<PlayerStats>();
            DamageFlash df = playerHit.collider.GetComponent<DamageFlash>();
            if (ps != null) ps.TakeDamage(laserDamagePerTick);
            if (df != null) df.Flash();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Attack: Briefcase Throw
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator BriefcaseAttack()
    {
        FindPlayer();
        if (briefcasePrefab == null || player == null) yield break;

        float interval = isEnraged
            ? briefcaseInterval * enrageSpeedMultiplier
            : briefcaseInterval;

        for (int i = 0; i < briefcasesPerAttack; i++)
        {
            ThrowBriefcase();
            yield return new WaitForSeconds(interval);
        }
    }

    private void ThrowBriefcase()
    {
        float force = isEnraged ? briefcaseForce * (1f / enrageSpeedMultiplier) : briefcaseForce;

        // Pick the random direction first so we can use it for both the throw and the rotation
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 randomDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));

        // Rotate boss to face the throw direction
        // Adjust the -90f offset to match your sprite's natural facing direction
        float facingAngle = Mathf.Atan2(randomDirection.y, randomDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, facingAngle + 90f);

        GameObject bc = Instantiate(briefcasePrefab, transform.position, Quaternion.identity);

        Rigidbody2D rb = bc.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.AddForce(randomDirection * force, ForceMode2D.Impulse);

        BriefcaseSpawner spawner = bc.GetComponent<BriefcaseSpawner>();
        if (spawner != null)
            spawner.bossTransform = transform;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Attack: Rebounding Darts
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator DartAttack()
    {
        FindPlayer();
        if (bouncingDartPrefab == null || firePoint == null || player == null)
            yield break;

        float interval = isEnraged
            ? dartBurstInterval * enrageSpeedMultiplier
            : dartBurstInterval;

        // Show laser sight during the telegraph window (boss is already flashing yellow)
        if (dartLaserSightSprite != null)
            dartLaserSightSprite.Show(firePoint.position,
                (player.position - firePoint.position).normalized);

        // Track the sight each frame toward the player before firing
        float telegraphTime = isEnraged
            ? telegraphDuration * enrageSpeedMultiplier
            : telegraphDuration;

        float sightElapsed = 0f;
        while (sightElapsed < telegraphTime)
        {
            if (dartLaserSightSprite != null)
                dartLaserSightSprite.Show(firePoint.position,
                    (player.position - firePoint.position).normalized);
            sightElapsed += Time.deltaTime;
            yield return null;
        }

        // Hide sight and fire
        if (dartLaserSightSprite != null)
            dartLaserSightSprite.Hide();

        for (int i = 0; i < dartsPerBurst; i++)
        {
            FireBouncingDart();
            yield return new WaitForSeconds(interval);
        }
    }

    private void FireBouncingDart()
    {
        if (player == null) return;

        Vector2 direction = (player.position - firePoint.position).normalized;
        float speed = isEnraged ? dartSpeed * (1f / enrageSpeedMultiplier) : dartSpeed;

        GameObject dart = Instantiate(bouncingDartPrefab, firePoint.position, Quaternion.identity);

        BouncingDartProjectile bdp = dart.GetComponent<BouncingDartProjectile>();
        if (bdp != null)
        {
            bdp.Initialize(direction, speed);
        }
        else
        {
            // Fallback: just shoot it with a rigidbody
            Rigidbody2D rb = dart.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = direction * speed;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Damage & Death
    // ─────────────────────────────────────────────────────────────────────────

    // Called by SquirtProjectile when it hits the boss
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        StartCoroutine(HitFlash());

        // Check for enrage at 50% health
        if (!isEnraged && currentHealth <= maxHealth * 0.5f)
        {
            TriggerEnrage();
        }

        if (currentHealth <= 0f)
        {
            StartCoroutine(Die());
        }
    }

    // Called by BriefcaseSpawner when player throws the briefcase back
    public void TakeBriefcaseDamage(float amount)
    {
        TakeDamage(amount);
    }

    private IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;
        Color prev = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        if (!isDead) spriteRenderer.color = prev;
    }

    private void TriggerEnrage()
    {
        isEnraged = true;

        if (spriteRenderer != null)
            spriteRenderer.color = enragedColor;

        Debug.Log("Boss enraged!");

        // Could play an animation or sound here
    }

    private IEnumerator Die()
    {
        isDead = true;
        isAttacking = false;
        StopCoroutine(BossCycle());

        laserActive = false;
        if (spriteLaser != null) spriteLaser.HideBeam();

        if (deathClip != null && audioSource != null)
            audioSource.PlayOneShot(deathClip);

        if (animator != null)
            animator.Play("Death");

        // Hand off to explosion sequence — it destroys the GameObject at the end
        if (explosionSequence != null)
        {
            explosionSequence.Play();

            // Wait for the sequence to finish before showing credits
            // startDelay + cameraPanDuration + holdBeforeExplosion
            // + (smallExplosionCount * smallExplosionInterval) + bigExplosionLifetime
            float sequenceDuration = explosionSequence.startDelay
                + explosionSequence.cameraPanDuration
                + explosionSequence.holdBeforeExplosion
                + (explosionSequence.smallExplosionCount * explosionSequence.smallExplosionInterval)
                + 0.5f; // buffer

            yield return new WaitForSeconds(sequenceDuration);
        }

        if (creditsScreen != null)
            creditsScreen.Show();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Gizmos
    // ─────────────────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);

        if (firePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(firePoint.position, 0.2f);
        }
    }
}
