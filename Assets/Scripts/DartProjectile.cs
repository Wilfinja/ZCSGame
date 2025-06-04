using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DartProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;    // How long the projectile exists

    public int damageAmount = 5;
    public Collider2D hitCollider;

    void Start()
    {
        // Destroy the projectile after lifetime seconds
        Destroy(gameObject, lifetime);
        
    }

    void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("Player"))
        {
            hitCollider.enabled = false;
            //Debug.Log("Player Hit!");
            other.GetComponent<PlayerStats>().TakeDamage(damageAmount/2);
            other.GetComponent<DamageFlash>().Flash();
            Destroy(gameObject,.2f);
        }
        else if (other.CompareTag("Environment") || other.CompareTag("Enemy"))
        {
            //Debug.Log("Wall Hit!");
            Destroy(gameObject,.2f);
        }
    }
}
