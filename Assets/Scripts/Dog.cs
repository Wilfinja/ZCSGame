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
    public float itemChaseRadius = 15f;

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

        player = playerTarget.transform;

        lasPosition = transform.position;

        animator = GetComponent<Animator>();

        eatFinished = true;

        animator.Play("Walk");
    }

    private void Update()
    {

        canSeePlayer = CheckLineOfSight();

        // First, check for nearby throwable items
        if (FindNearestItem())
        {
            navMeshAgent.SetDestination(currentTarget.transform.position);
            CheckItemChaseComplete();
            animator.Play("Run");
        }
        // If no items, chase the player
        else if (slowedPlayer == false && canSeePlayer && eatFinished == true)
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(playerTarget.transform.position);
            animator.Play("Run");
        }
        else if (slowedPlayer == true || eatFinished == false)
        {
            navMeshAgent.isStopped = true;
            animator.Play("Walk");
        }
        else
        {
            navMeshAgent.isStopped = true;
            animator.Play("Walk");
        }

        Vector2 currentPosition = transform.position;
        Vector2 movementDirection;

        movementDirection = navMeshAgent.velocity.normalized;

        if (movementDirection.magnitude > 0.01f)
        {
            currentDirection = movementDirection;

            // Calculate the angle for rotation
            float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;

            Quaternion targetRotation = Quaternion.Euler(0, 0, angle-90);
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
        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.transform.position);
        if (distanceToPlayer > detectionRadius) return false;

        // Calculate direction to player
        Vector2 directionToPlayer = (playerTarget.transform.position - transform.position).normalized;

        // Create the raycast
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,          // Start position
            directionToPlayer,          // Direction
            detectionRadius,            // Max distance
            obstacleLayer | playerLayer // What to hit
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
        if (Vector2.Distance(transform.position, currentTarget.transform.position) <= stopChaseDistance)
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
        float closestDistance = chaseRadius;
        currentTarget = null;

        foreach (ThrowableItem item in items)
        {
            // Skip items being held by player
            if (item.transform.parent != null) continue;

            float distance = Vector2.Distance(transform.position, item.transform.position);
            if (distance < closestDistance)
            {
                currentTarget = item;
                closestDistance = distance;
                return true;
            }
        }

        return false;
    }

    private void ChaseItem()
    {
        if (currentTarget == null) return;

        // Calculate direction to item
        Vector2 directionToItem = ((Vector2)currentTarget.transform.position - rb.position).normalized;

        // Move towards the item
        rb.velocity = directionToItem * moveSpeed;

        // Stop chasing if very close to item
        if (Vector2.Distance(transform.position, currentTarget.transform.position) <= stopChaseDistance)
        {
            rb.velocity = Vector2.zero;
            Destroy(currentTarget.gameObject);
            currentTarget = null;
        }
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
        if (item != null && item == currentTarget)
        {
            Destroy(item.gameObject);
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
        if (drawDebugLines)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }

    // Public method to check if player is visible
    public bool CanSeePlayer()
    {
        return canSeePlayer;
    }

}
