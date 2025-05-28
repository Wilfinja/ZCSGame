using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Dog : MonoBehaviour
{
    [Header("Enemy Properties")]
    public float moveSpeed = 3f;
    public float slowAmount = 0.5f;
    public float maxHealth = 3f;

    [Header("Chasing Properties")]
    public float chaseRadius = 10f;
    public float stopChaseDistance = 0.5f;
    public float itemChaseRadius = 15f; // This is bigger than regular chase radius

    [Header("Item Drop")]
    public GameObject droppedItemPrefab;

    private NavMeshAgent navMeshAgent;
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

    private void Start()
    {
        slowedPlayer = false;

        // Get NavMeshAgent component from unity so we can call on it later
        navMeshAgent = GetComponent<NavMeshAgent>();

        // Configure NavMeshAgent to make sure it's right side up
        navMeshAgent.updateRotation = false;
        navMeshAgent.updateUpAxis = false;

        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        playerTarget = FindObjectOfType<ClickToMove>();

        player = playerTarget != null ? playerTarget.transform : null;

        lasPosition = transform.position;

        animator = GetComponent<Animator>();

        eatFinished = true;

        // Make sure NavMeshAgent speed matches our moveSpeed
        navMeshAgent.speed = moveSpeed;

        if (animator != null)
        {
            animator.Play("Walk");
        }
    }

    private void Update()
    {
        canSeePlayer = CheckLineOfSight();

        bool foundItem = FindNearestItem();

        // First priority: Chase throwable items if found
        if (foundItem && currentTarget != null)
        {
            // Make sure agent is not stopped
            navMeshAgent.isStopped = false;

            // Set destination to the item
            navMeshAgent.SetDestination(currentTarget.transform.position);

            // Check if we've reached the item
            CheckItemChaseComplete();

            // Set animation
            if (animator != null)
            {
                animator.Play("Run");
            }

            //if (drawDebugLines)
            //{
            //    Debug.DrawLine(transform.position, currentTarget.transform.position, Color.green);
            //}
        }
        // Second priority: Chase player if not slowed and can see player
        else if (!slowedPlayer && canSeePlayer && eatFinished)
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(player.position);

            if (animator != null)
            {
                animator.Play("Run");
            }
        }
        // Otherwise: Stop moving (when slowed or eating)
        else
        {
            navMeshAgent.isStopped = true;

            if (animator != null)
            {
                animator.Play("Walk");
            }
        }

        // Handle rotation based on movement direction
        UpdateRotation();
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

    private void CheckItemChaseComplete()
    {
        if (currentTarget == null) return;

        // Check if close enough to item
        float distance = Vector2.Distance(transform.position, currentTarget.transform.position);
        if (distance <= stopChaseDistance)
        {
            Destroy(currentTarget.gameObject);
            StartCoroutine(EatCheese());
            currentTarget = null;
        }
    }

    private bool FindNearestItem()
    {
        // Find all throwable items in the scene
        ThrowableItem[] items = FindObjectsOfType<ThrowableItem>();

        // Use itemChaseRadius instead of chaseRadius for items
        float closestDistance = itemChaseRadius;
        ThrowableItem closestItem = null;

        foreach (ThrowableItem item in items)
        {
            // Skip items that are held by player or items that dog won't eat
            if (item.transform.parent != null || !item.willEat)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, item.transform.position);
            if (distance < closestDistance)
            {
                closestItem = item;
                closestDistance = distance;
            }
        }

        // Update the current target
        currentTarget = closestItem;

        // Return true if we found an item
        return currentTarget != null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Slow down player when touched
        ClickToMove player = collision.gameObject.GetComponent<ClickToMove>();
        if (player != null)
        {
            player.BeginSlow(slowAmount);
            slowedPlayer = true;
            StartCoroutine(ChaseTimer());
        }

        // Check if collided with the item being chased
        ThrowableItem item = collision.gameObject.GetComponent<ThrowableItem>();
        if (item != null && item == currentTarget && item.willEat)
        {
            Destroy(item.gameObject);
            StartCoroutine(EatCheese());
            currentTarget = null;
        }
    }

    IEnumerator ChaseTimer()
    {
        yield return new WaitForSeconds(slowCooldown);
        slowedPlayer = false;
    }

    IEnumerator EatCheese()
    {
        eatFinished = false;
        yield return new WaitForSeconds(eatCooldown);
        eatFinished = true;
    }

    private void OnDrawGizmos()
    {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw item chase radius (in a different color)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, itemChaseRadius);
    }

    // Public method to check if player is visible
    public bool CanSeePlayer()
    {
        return canSeePlayer;
    }

}
