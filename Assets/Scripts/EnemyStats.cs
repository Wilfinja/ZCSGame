using UnityEngine;

public class EnemyStats : MonoBehaviour
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

    private Animator animator;

    public int damageAmount = 5;

    // Debug variables
    private string currentState = "";
    public bool showDebugInfo = true;

    private void Start()
    {
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

        // Make sure NavMeshAgent speed matches our moveSpeed
        navMeshAgent.speed = moveSpeed;
      
    }

    private void Update()
    {
        canSeePlayer = CheckLineOfSight();

        // Priority 1: Chase player if not slowed, can see player, and finished eating
        if (canSeePlayer)
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
        // Priority 4: Idle state (at home or no target) - keep agent active but clear destination
        else
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.ResetPath(); // Clear current path but keep agent active
        }

        // Handle rotation based on movement direction
        UpdateRotation();

        // Debug info
        if (showDebugInfo && Time.frameCount % 60 == 0) // Log every second
        {
            Debug.Log($"{gameObject.name} - State: {currentState}, CanSeePlayer: {canSeePlayer}, NavMeshAgent.hasPath: {navMeshAgent.hasPath}");
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
             // No item to steal, deal damage instead
             other.GetComponent<PlayerStats>().TakeDamage(damageAmount / 2);
             other.GetComponent<DamageFlash>().Flash();
         
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

    public void TakeDamage(int amount)
    {
        currentHealth = currentHealth - amount;
    }

}
