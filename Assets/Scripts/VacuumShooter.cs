using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VacuumShooter : MonoBehaviour
{
    [Header("Enemy Properties")]
    public float moveSpeed = 3f;
    public float maxHealth = 3f;

    [Header("Chasing Properties")]
    public float chaseRadius = 10f;
    public float stopChaseDistance = 0.5f;
    public float itemChaseRadius = 15f; // This is bigger than regular chase radius

    [Header("Item Drop")]
    public GameObject droppedItemPrefab;

    [Header("Shooting Settings")]
    public GameObject projectilePrefab;           // Assign your projectile prefab in inspector
    public float projectileSpeed = 10f;           // Speed of the projectile
    public Transform firePoint;                   // Point where projectiles spawn
    public float fireRate = 2f;                   // Time between shots
    public int burst = 1;                         // Number of projectiles per burst
    public float burstRate = 0.25f;               // Time between each shot in a burst
    public float shootingRange = 8f;              // Range at which vacuum starts shooting

    private float nextFireTime = 0f;
    private bool firing = false;

    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private float currentHealth;
    private ThrowableItem currentTarget;
    private ClickToMove playerTarget;
    private Rigidbody2D rb;
    private bool slowedPlayer = false;

    public float slowCooldown = 2;
    public float eatCooldown = 2;

    [Header("Detection Settings")]
    public float detectionRadius = 10f;        // How far the enemy can see
    public LayerMask obstacleLayer;           // Layer for walls/obstacles
    public LayerMask playerLayer;             // Layer the player is on
    public bool drawDebugLines = true;        // Visualize the raycast

    public float rotationSpeed = 8f;
    private Vector2 lasPosition;
    private Vector2 currentDirection;

    private Transform player;
    private bool canSeePlayer = false;
    private bool hasLineOfSightToPlayer = false;

    private bool eatFinished = true;

    private Animator animator;

    public int damageAmount = 5;

    // Debug variables
    private string currentState = "";
    public bool showDebugInfo = true;

    private void Start()
    {
        slowedPlayer = false;

        // Get NavMeshAgent component from unity so we can call on it later
        navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        // Configure NavMeshAgent to make sure it's right side up
        navMeshAgent.updateRotation = false;
        navMeshAgent.updateUpAxis = false;

        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        playerTarget = FindFirstObjectByType<ClickToMove>();

        player = playerTarget != null ? playerTarget.transform : null;

        lasPosition = transform.position;

        eatFinished = true;

        // Make sure NavMeshAgent speed matches our moveSpeed
        navMeshAgent.speed = moveSpeed;

        animator = GetComponent<Animator>();

        // Validate shooting setup
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"{gameObject.name}: Projectile prefab is not assigned!");
        }
        if (firePoint == null)
        {
            Debug.LogWarning($"{gameObject.name}: Fire point is not assigned! Using enemy position as default.");
            // Create a default fire point as child
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = Vector3.zero;
            firePoint = fp.transform;
        }
    }

    private void Update()
    {
        canSeePlayer = CheckPlayerWithinRadius();
        hasLineOfSightToPlayer = CheckLineOfSight();
        float distanceToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;

        // Check if we should shoot at the player (only if there's line of sight AND in range)
        if (hasLineOfSightToPlayer && distanceToPlayer <= shootingRange && Time.time >= nextFireTime && !firing)
        {
            StartCoroutine(ShootAtPlayer());
            nextFireTime = Time.time + fireRate;
        }

        // Priority 1: Chase player if not slowed, player is within detection radius, and finished eating
        if (!slowedPlayer && canSeePlayer && eatFinished)
        {
            currentState = "Chasing Player";
            navMeshAgent.isStopped = false;

            // Validate path to player
            if (SetDestinationSafe(player.position))
            {
                if (showDebugInfo) Debug.Log($"{gameObject.name}: Chasing player");
            }
            else
            {
                if (showDebugInfo) Debug.LogWarning($"{gameObject.name}: Cannot find path to player!");
            }
        }
        // Priority 2: Stop moving if slowed or eating
        else if (slowedPlayer || !eatFinished)
        {
            if (!eatFinished)
                currentState = "Eating";
            else if (slowedPlayer)
                currentState = "Slowed";

            navMeshAgent.isStopped = true;
        }
        // Priority 3: Idle state (no target)
        else
        {
            currentState = "Idle";
            navMeshAgent.isStopped = false;
            navMeshAgent.ResetPath(); // Clear current path but keep agent active
        }

        // Handle rotation based on movement direction
        UpdateRotation();

        // Debug info
        if (showDebugInfo && Time.frameCount % 60 == 0) // Log every second
        {
            Debug.Log($"{gameObject.name} - State: {currentState}, CanSeePlayer: {canSeePlayer}, HasLOS: {hasLineOfSightToPlayer}, NavMeshAgent.hasPath: {navMeshAgent.hasPath}");
        }
    }

    private IEnumerator ShootAtPlayer()
    {
        if (player == null || projectilePrefab == null || firePoint == null) yield break;

        firing = true;

        for (int i = 0; i < burst; i++)
        {
            // Get the direction to the player
            Vector2 fireDirection = (player.position - firePoint.position).normalized;
            Vector2 shot = fireDirection * projectileSpeed;
            float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;

            // Play shooting animation if available
            if (animator != null)
            {
                animator.Play("Fire", -1, 0f);
            }

            // Create the projectile at the fire point position and rotation
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.Euler(0, 0, angle - 180));

            // Get the Rigidbody2D component
            Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();

            if (projectileRb != null)
            {
                // Add force in the direction of the player
                projectileRb.AddForce(shot, ForceMode2D.Impulse);
            }

            yield return new WaitForSeconds(burstRate);
        }

        firing = false;
    }

    private bool SetDestinationSafe(Vector3 destination)
    {
        // Check if the destination is on the NavMesh
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(destination, out hit, 1.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            return navMeshAgent.SetDestination(hit.position);
        }
        return false;
    }

    private void UpdateRotation()
    {
        // Get the direction from the NavMeshAgent's velocity
        Vector2 movementDirection = navMeshAgent.velocity.normalized;

        if (movementDirection.magnitude > 0.01f)
        {
            currentDirection = movementDirection;

            // Calculate the angle for rotation
            float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;

            Quaternion targetRotation = Quaternion.Euler(0, 0, angle - 90);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private bool CheckPlayerWithinRadius()
    {
        if (player == null) return false;

        // Check if player is within detection radius
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        return distanceToPlayer <= detectionRadius;
    }

    private bool CheckLineOfSight()
    {
        if (player == null) return false;

        // Check if player is within shooting range
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer > shootingRange) return false;

        // Calculate direction to player
        Vector2 directionToPlayer = (player.position - transform.position).normalized;

        // Create the raycast
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,          // Start position
            directionToPlayer,           // Direction
            shootingRange,               // Max distance
            obstacleLayer | playerLayer  // What to hit
        );

        // Draw debug ray in scene view
        if (drawDebugLines)
        {
            Color rayColor = (hit.collider != null && hit.collider.CompareTag("Player")) ? Color.green : Color.red;
            Debug.DrawRay(transform.position, directionToPlayer * shootingRange, rayColor);
        }

        // Check what the raycast hit
        if (hit.collider != null)
        {
            // Return true only if we hit the player first (no obstacles in the way)
            return hit.collider.CompareTag("Player");
        }

        return false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Steal object from player when touched else deal damage
        ClickToMove player = collision.gameObject.GetComponent<ClickToMove>();
        PlayerStats other = collision.gameObject.GetComponent<PlayerStats>();

        if (player != null)
        {
            if (player.IsHoldingItem())
            {
                player.DropHeldItem();
                Debug.Log("Vacuum stole the player's item!");
            }
            else
            {
                // No item to steal, deal damage instead
                other.GetComponent<PlayerStats>().TakeDamage(damageAmount / 2);
                other.GetComponent<DamageFlash>().Flash();
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw item chase radius (in a different color)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, itemChaseRadius);

        // Draw shooting range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootingRange);

        // Show current state as text
        if (showDebugInfo)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, currentState);
        }
    }

    // Public method to check if player is visible
    public bool CanSeePlayer()
    {
        return canSeePlayer;
    }
}
