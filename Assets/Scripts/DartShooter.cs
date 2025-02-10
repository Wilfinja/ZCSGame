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

    private void Start()
    {
        firing = false;
    }

    void Update()
    {
        // Check if it's time to allow another shot
        if (Time.time >= nextFireTime && !firing)
        {
            StartCoroutine(Shoot(burstRate));
            nextFireTime = Time.time + fireRate;
        }
    }

    IEnumerator Shoot(float burstspeed)
    {
        firing = true;

        for (int i = 0; i < burst;  i++)
        {
            // Get the direction of where you want to shoot the projectile
            Vector2 fireDirection = (fireTarget.position - firePoint.position).normalized;
            Vector2 shot = fireDirection * projectileSpeed;
            float angle = Mathf.Atan2(-fireDirection.y, -fireDirection.x) * Mathf.Rad2Deg;

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
