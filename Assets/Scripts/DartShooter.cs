using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DartShooter : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;    // Assign your projectile prefab in inspector
    [SerializeField] private float projectileSpeed = 10f;    // Speed of the projectile
    [SerializeField] private Transform firePoint;            // Point where projectiles spawn
    [SerializeField] private Transform fireTarget;
    [SerializeField] private float fireRate = 0.5f;         // Time between shots

    private float nextFireTime = 0f;
    private Transform dtransform;
    public int burst = 3;

    private bool firing;
    public float burstRate = .25f;

    private Animator animator;

    [Header("References")]
    [Tooltip("Reference to the player's transform")]
    public Transform player;

    [Header("Detection Settings")]
    [Tooltip("Radius around sprite that triggers animations")]
    public float detectionRadius = 3f;

    private bool playerInRange = false;

    private void Start()
    {
        firing = false;

        // If no animator reference was set in the inspector
        if (animator == null)
        {
            // Try to get the Animator component from this GameObject
            animator = GetComponent<Animator>();

            // If still null, log an error
            if (animator == null)
            {
                Debug.LogError("Animator component not found on " + gameObject.name);
            }
        }

        // If no player reference was set in the inspector
        if (player == null)
        {
            // Try to find the player in the scene
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else
            {
                Debug.LogError("Player not found in scene. Set the player reference or tag your player as 'Player'");
            }
        }
    }

    void Update()
    {

        // Check if it's time to allow another shot
        if (Time.time >= nextFireTime && !firing)
        {
            StartCoroutine(Shoot(burstRate));
            nextFireTime = Time.time + fireRate;
        }

        if (player == null) return;

        // Calculate distance between this sprite and the player
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Check if player is entering the range
        if (distanceToPlayer <= detectionRadius && !playerInRange)
        {
            PlayerEnter();
        }
        // Check if player is exiting the range
        else if (distanceToPlayer > detectionRadius && playerInRange)
        {
            PlayerExit();
        }
    }

    private void PlayerEnter()
    {
        playerInRange = true;

        //Debug.Log("Player Entered");

        // Trigger the enter animation
        if (animator != null)
        {
            animator.Play("Gun Pickup");
        }
    }

    private void PlayerExit()
    {
        playerInRange = false;

        //Debug.Log("Player Exited");

        // Trigger the exit animation
        if (animator != null)
        {
            animator.Play("PutBack");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    IEnumerator Shoot(float burstspeed)
    {
        if (playerInRange)
        {
            firing = true;

            for (int i = 0; i < burst; i++)
            {


                // Get the direction of where you want to shoot the projectile
                Vector2 fireDirection = (fireTarget.position - firePoint.position).normalized;
                Vector2 shot = fireDirection * projectileSpeed;
                float angle = Mathf.Atan2(-fireDirection.y, -fireDirection.x) * Mathf.Rad2Deg;

                animator.Play("Shoot",-1,0f);

                // Create the projectile at the fire point position and rotation
                GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.Euler(0, 0, angle));

                // Get the Rigidbody2D component
                Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();

                // Add force in the direction the fire point is facing
                rb.AddForce(shot, ForceMode2D.Impulse);

                yield return new WaitForSeconds(burstspeed);
            }

            firing = false;
        }
    }
}
