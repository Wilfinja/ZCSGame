using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A robot enemy that patrols or stands stationary, grabs the player on contact,
/// carries them to a destination (dealing damage over time), and releases them.
/// Player can escape by mashing a key or shooting the robot with the squirt gun.
/// 
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Grabberrobot : MonoBehaviour
{
    public enum GrabberState { Patrolling, Chasing, Grabbing, Carrying, Returning, Dead }

    // ─── Enemy Properties ──────────────────────────────────────────────────────
    [Header("Enemy Properties")]
    public float maxHealth = 10f;
    public float moveSpeed = 2.5f;
    public float chaseSpeed = 3.5f;
    public float carrySpeed = 2f;
    public int damageAmount = 5;

    // ─── Detection ─────────────────────────────────────────────────────────────
    [Header("Detection Settings")]
    public float detectionRadius = 8f;
    public float grabRange = 0.8f;       // Distance at which grab triggers
    public LayerMask obstacleLayer;
    public LayerMask playerLayer;
    public bool drawDebugLines = true;

    // ─── Grab / Carry ──────────────────────────────────────────────────────────
    [Header("Grab & Carry Settings")]
    [Tooltip("Where the robot carries the player. If null, robot stays put and deals damage.")]
    public Transform dropPoint;

    [Tooltip("Damage dealt to player each tick while being carried.")]
    public int damagePerTick = 5;
    public float damageTickRate = 1f;

    // ─── Escape ────────────────────────────────────────────────────────────────
    [Header("Escape Settings")]
    [Tooltip("How many times the player must press the escape key to break free.")]
    public int mashCountRequired = 10;

    [Tooltip("Key the player mashes to escape. Spacebar matches the Throw action binding.")]
    public KeyCode mashKey = KeyCode.Space;

    [Tooltip("How many squirt gun hits are needed to force a release (0 = disabled, health still applies).")]
    public int hitsToForceRelease = 3;

    // ─── Patrol ────────────────────────────────────────────────────────────────
    [Header("Patrol Settings")]
    [Tooltip("If true, robot stands still until it spots the player.")]
    public bool isStationary = false;

    [Tooltip("Patrol waypoints. Ignored if isStationary is true.")]
    public Transform[] patrolPoints;

    // ─── Visual / Audio ────────────────────────────────────────────────────────
    [Header("Visual & Audio")]
    public float rotationSpeed = 8f;
    public AudioClip grabSound;
    public AudioClip releaseSound;

    // ─── Escape Progress UI (optional) ─────────────────────────────────────────
    [Header("Escape UI (Optional)")]
    [Tooltip("Assign a UI Text or TMP component to show mash progress. Leave null to skip.")]
    public TMPro.TextMeshPro escapeProgressText; 

    // ─── Private State ─────────────────────────────────────────────────────────
    private NavMeshAgent navMeshAgent;
    private Rigidbody2D rb;
    private Animator animator;
    private AudioSource audioSource;
    private DamageFlash damageFlash;
    private Camera mainCamera;

    private Transform player;
    private ClickToMove playerMove;
    private Rigidbody2D playerRb;
    private LookAtCursor playerLook;

    private float currentHealth;
    private GrabberState currentState = GrabberState.Patrolling;

    private float damageTimer;
    private int mashCount;
    private int hitCount;           // Squirt gun hits while carrying
    private int currentPatrolIndex;
    private bool isDead;

    private bool hasGrabbed;
    [Tooltip("Assign a cooldown for after the grab action occurs")]
    public int grabCooldown;

    // Offset so player doesn't clip directly into the robot sprite
    private readonly Vector3 carryOffset = new Vector3(0f, -0.7f, 0f);

    // ───────────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Start()
    {
        currentHealth = maxHealth;
        isDead = false;
        hasGrabbed = false;
        mainCamera = Camera.main;

        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.updateRotation = false;
        navMeshAgent.updateUpAxis = false;
        navMeshAgent.speed = moveSpeed;

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        damageFlash = GetComponent<DamageFlash>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (grabSound != null || releaseSound != null))
            audioSource = gameObject.AddComponent<AudioSource>();

        // Cache player references
        playerMove = FindFirstObjectByType<ClickToMove>();
        if (playerMove != null)
        {
            player = playerMove.transform;
            playerRb = playerMove.GetComponent<Rigidbody2D>();
            playerLook = playerMove.GetComponent<LookAtCursor>();
        }

        // Begin patrolling if waypoints are set
        if (!isStationary && patrolPoints.Length > 0)
            navMeshAgent.SetDestination(patrolPoints[0].position);

        if (escapeProgressText != null)
            escapeProgressText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isDead) return;

        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        switch (currentState)
        {
            case GrabberState.Patrolling: UpdatePatrol(); break;
            case GrabberState.Chasing: UpdateChase(); break;
            case GrabberState.Grabbing: UpdateGrabbing(); break;
            case GrabberState.Carrying: UpdateCarrying(); break;
            case GrabberState.Returning: UpdateReturn(); break;
        }

        UpdateRotation();

        if (escapeProgressText != null && escapeProgressText.gameObject.activeSelf && mainCamera != null)
        {
            escapeProgressText.transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward);
        }

    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────────
    #region State Logic

    private void UpdatePatrol()
    {
        // Check if player has entered detection range
        if (player != null && HasLineOfSight())
        {
            currentState = GrabberState.Chasing;
            navMeshAgent.speed = chaseSpeed;
            return;
        }

        if (isStationary || patrolPoints.Length == 0)
        {
            navMeshAgent.isStopped = true;
            return;
        }

        // Move between waypoints
        navMeshAgent.isStopped = false;
        if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance <= 0.4f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            navMeshAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    private void UpdateChase()
    {
        if (player == null || hasGrabbed) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // Lost player — return to patrol
        if (!HasLineOfSight() && dist > detectionRadius * 1.2f)
        {
            currentState = GrabberState.Patrolling;
            navMeshAgent.speed = moveSpeed;

            if (!isStationary && patrolPoints.Length > 0)
                navMeshAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
            return;
        }

        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(player.position);

        // Close enough — grab the player
        if (dist <= grabRange && !hasGrabbed)
        {
            StartGrab();
        }  
    }

    private void UpdateGrabbing()
    {
        // Hold player in place during brief grab animation
        SnapPlayerToRobot();
        CheckMashInput();
    }

    private void UpdateCarrying()
    {
        if (player == null) return;

        // Keep player attached
        SnapPlayerToRobot();

        // Damage over time
        damageTimer += Time.deltaTime;
        if (damageTimer >= damageTickRate)
        {
            DealDamageToPlayer();
            damageTimer = 0f;
        }

        CheckMashInput();

        // Reached drop point
        if (dropPoint != null && Vector2.Distance(transform.position, dropPoint.position) <= 0.8f)
        {
            ReleasePlayer("Reached destination");
            return;
        }

        // No drop point — just hold and damage in place
        if (dropPoint == null)
            navMeshAgent.isStopped = true;
    }

    private void UpdateReturn()
    {
        if (isStationary || patrolPoints.Length == 0)
        {
            currentState = GrabberState.Patrolling;
            navMeshAgent.isStopped = true;
            return;
        }

        if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance <= 0.4f)
            currentState = GrabberState.Patrolling;
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────────
    #region Grab / Release

    private void StartGrab()
    {
        if (currentState == GrabberState.Grabbing || currentState == GrabberState.Carrying) return;

        currentState = GrabberState.Grabbing;
        navMeshAgent.isStopped = true;
        mashCount = 0;
        hitCount = 0;
        damageTimer = 0f;

        // Disable player control — using pausePush rather than disabling the
        // whole component so GameOver/animator still function on the same object
        if (playerMove != null) playerMove.pausePush = true;
        if (playerLook != null) playerLook.enabled = false;

        // Freeze player physics so they don't drift
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            playerRb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (grabSound != null && audioSource != null)
            audioSource.PlayOneShot(grabSound);

        if (animator != null)
            animator.Play("Grab");      // Replace "Grab" with your actual animation name

        ShowEscapeUI(true);

        StartCoroutine(TransitionToCarry(0.3f));
    }

    private IEnumerator TransitionToCarry(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Player may have died during the grab delay
        if (isDead || currentState == GrabberState.Dead) yield break;

        currentState = GrabberState.Carrying;
        navMeshAgent.isStopped = false;
        navMeshAgent.speed = carrySpeed;

        if (dropPoint != null)
            navMeshAgent.SetDestination(dropPoint.position);
    }

    private void ReleasePlayer(string reason = "")
    {
        if (currentState != GrabberState.Grabbing && currentState != GrabberState.Carrying) return;

        hasGrabbed = true;
        StartCoroutine(GrabCooldown());

        if (playerMove != null) playerMove.pausePush = false;
        if (playerLook != null) playerLook.enabled = true;

        if (playerRb != null)
        {
            playerRb.bodyType = RigidbodyType2D.Dynamic;
            playerRb.linearVelocity = Vector2.zero;
        }

        if (releaseSound != null && audioSource != null)
            audioSource.PlayOneShot(releaseSound);

        if (animator != null)
            animator.Play("Walk");      // Replace with your idle/walk animation name

        mashCount = 0;
        hitCount = 0;
        ShowEscapeUI(false);

        currentState = GrabberState.Returning;
        navMeshAgent.speed = moveSpeed;

        if (!isStationary && patrolPoints.Length > 0)
            navMeshAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
        else
            navMeshAgent.isStopped = true;
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────────
    #region Helpers

    private void SnapPlayerToRobot()
    {
        if (player == null) return;

        // Use local-space offset so the player stays on the correct side regardless of rotation
        player.position = transform.TransformPoint(carryOffset);

        // Keep velocity zeroed so other physics don't push the player away
        if (playerRb != null)
            playerRb.linearVelocity = Vector2.zero;
    }

    private void CheckMashInput()
    {
        if (Input.GetKeyDown(mashKey))
        {
            mashCount++;
            UpdateEscapeUI();

            if (mashCount >= mashCountRequired)
                ReleasePlayer("Mashed free");
        }
    }

    private void DealDamageToPlayer()
    {
        if (player == null) return;

        PlayerStats stats = player.GetComponent<PlayerStats>();
        DamageFlash flash = player.GetComponent<DamageFlash>();

        if (stats != null)
        {
            stats.TakeDamage(damagePerTick);
            flash?.Flash();
        }
    }

    private bool HasLineOfSight()
    {
        if (player == null) return false;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectionRadius) return false;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, dir, detectionRadius,
            obstacleLayer | playerLayer
        );

        if (drawDebugLines)
        {
            Color c = (hit.collider != null && hit.collider.CompareTag("Player"))
                ? Color.green : Color.red;
            Debug.DrawRay(transform.position, dir * detectionRadius, c);
        }

        return hit.collider != null && hit.collider.CompareTag("Player");
    }

    private void UpdateRotation()
    {
        Vector2 dir = navMeshAgent.velocity.normalized;
        if (dir.magnitude > 0.01f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion target = Quaternion.Euler(0f, 0f, angle + 90f);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
    }

    private void ShowEscapeUI(bool show)
    {
        if (escapeProgressText == null) return;
        escapeProgressText.gameObject.SetActive(show);
        if (show) UpdateEscapeUI();
    }

    private void UpdateEscapeUI()
    {
        if (escapeProgressText == null) return;
        escapeProgressText.text = $"MASH [{mashKey}] TO ESCAPE! {mashCount}/{mashCountRequired}";
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────────
    #region Public API

    /// <summary>
    /// Called by SquirtProjectile when hit. Also forces a release if hit enough times.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        damageFlash?.Flash();

        // Count hits while the robot is holding the player
        if (currentState == GrabberState.Carrying || currentState == GrabberState.Grabbing)
        {
            hitCount++;

            if (hitsToForceRelease > 0 && hitCount >= hitsToForceRelease)
                ReleasePlayer("Shot free");
        }

        if (currentHealth <= 0f)
            Die();
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────────
    #region Death

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Always release before dying
        if (currentState == GrabberState.Carrying || currentState == GrabberState.Grabbing)
            ReleasePlayer("Robot died");

        currentState = GrabberState.Dead;
        navMeshAgent.isStopped = true;

        if (animator != null)
            animator.Play("Death");     // Replace with your actual death animation name

        Destroy(gameObject, 1.5f);
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────────
    #region Gizmos

    private void OnDrawGizmos()
    {
        // Detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Grab range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, grabRange);

        // Line to drop point
        if (dropPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, dropPoint.position);
            Gizmos.DrawWireSphere(dropPoint.position, 0.5f);
        }

        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 1.5f,
                $"{currentState} | HP:{currentHealth:F0}"
            );
        }
    }

    IEnumerator GrabCooldown()
    {
        yield return new WaitForSeconds(grabCooldown);
        hasGrabbed = false;
    }

    #endregion
}
