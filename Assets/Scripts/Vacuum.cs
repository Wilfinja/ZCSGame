using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vacuum : MonoBehaviour
{
    [Header("Enemy Properties")]
    public float moveSpeed = 3f;
    public float maxHealth = 3f;

    [Header("Chasing Properties")]
    public float chaseRadius = 10f;
    public float stopChaseDistance = 0.5f;
    public float itemChaseRadius = 15f; // This is bigger than regular chase radius
    public float homeStopDistance = .5f; // Distance to stop when reaching home

    [Header("Item Drop")]
    public GameObject droppedItemPrefab;

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

    private bool eatFinished = true;

    private Animator animator;

    public int damageAmount = 5;

    public Transform home;

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

        // Validate home position
        if (home == null)
        {
            Debug.LogWarning($"{gameObject.name}: Home transform is not assigned!");
        }
    }

    private void Update()
    {
        canSeePlayer = CheckLineOfSight();

        // Priority 1: Chase player if not slowed, can see player, and finished eating
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
        // Priority 2: Go home if can't see player and not at home
        else if (!canSeePlayer && !IsAtHome() && eatFinished && home != null)
        {
            currentState = "Returning Home";
            navMeshAgent.isStopped = false;

            // Validate path to home
            if (SetDestinationSafe(home.position))
            {
                if (showDebugInfo) Debug.Log($"{gameObject.name}: Returning home - Distance: {Vector2.Distance(transform.position, home.position):F2}");
            }
            else
            {
                if (showDebugInfo) Debug.LogWarning($"{gameObject.name}: Cannot find path to home! Home position: {home.position}");
                // Try to move towards home using direct movement as fallback
                MoveTowardsHomeDirect();
            }
        }
        // Priority 3: Stop moving ONLY when slowed or eating (not when at home or idle)
        else if (slowedPlayer || !eatFinished)
        {
            if (!eatFinished)
                currentState = "Eating";
            else if (slowedPlayer)
                currentState = "Slowed";

            navMeshAgent.isStopped = true;
        }
        // Priority 4: Idle state (at home or no target) - keep agent active but clear destination
        else
        {
            if (IsAtHome())
                currentState = "At Home";
            else
                currentState = "Idle";

            navMeshAgent.isStopped = false;
            navMeshAgent.ResetPath(); // Clear current path but keep agent active
        }

        // Handle rotation based on movement direction
        UpdateRotation();

        // Debug info
        if (showDebugInfo && Time.frameCount % 60 == 0) // Log every second
        {
            Debug.Log($"{gameObject.name} - State: {currentState}, CanSeePlayer: {canSeePlayer}, IsAtHome: {IsAtHome()}, NavMeshAgent.hasPath: {navMeshAgent.hasPath}");
        }
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

    private void MoveTowardsHomeDirect()
    {
        // Fallback movement when NavMesh fails
        if (home == null) return;

        Vector2 direction = (home.position - transform.position).normalized;
        transform.Translate(direction * moveSpeed * Time.deltaTime);

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: Using direct movement to home");
        }
    }

    private bool IsAtHome()
    {
        if (home == null) return false;

        float distanceToHome = Vector2.Distance(transform.position, home.position);
        return distanceToHome <= homeStopDistance;
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

    private bool CheckLineOfSight()
    {
        if (player == null) return false;

        // Check if player is within radius first
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRadius) return false;

        // Calculate direction to player
        Vector2 directionToPlayer = (player.position - transform.position).normalized;

        // Create the raycast
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,          // Start position
            directionToPlayer,           // Direction
            detectionRadius,             // Max distance
            obstacleLayer | playerLayer  // What to hit
        );

        // Draw debug ray in scene view
        if (drawDebugLines)
        {
            Color rayColor = (hit.collider != null && hit.collider.CompareTag("Player")) ? Color.green : Color.red;
            Debug.DrawRay(transform.position, directionToPlayer * detectionRadius, rayColor);
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

        // Draw home stop distance (if home is assigned)
        if (home != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(home.position, homeStopDistance);

            // Draw line to home
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, home.position);
        }

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

    // Public method to check if vacuum is at home
    public bool IsVacuumAtHome()
    {
        return IsAtHome();
    }
}
