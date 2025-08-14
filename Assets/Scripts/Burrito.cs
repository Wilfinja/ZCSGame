using System.Collections;
using UnityEngine;

public class Burrito : MonoBehaviour
{
    [Header("Explosion Timer")]
    public float totalTime = 8f; // Total time before explosion
    private float timer;

    [Header("Blink Settings")]
    public Color normalColor = Color.white;
    public Color warningColor = Color.red;
    public float maxBlinkRate = 10f; // blinks/sec at 0 time
    private SpriteRenderer sr;
    private bool isOnPlate = false;
    public bool isHeld = false;
    private bool isBlinking = false; // Track if blinking is active

    [Header("Explosion Settings")]
    public float explosionRadius;
    public int explosionDamage;

    private Rigidbody2D rb;
    private Animator animator;

    private bool hasExploded = false;

    public GameObject plateLevelEnd;

    void Start()
    {
        timer = totalTime;
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();    
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    void Update()
    {
        if (!isOnPlate && !isHeld) return;

        // Only start blinking if not already blinking
        if (!isBlinking && !isOnPlate)
        {
            StartCoroutine(BlinkRoutine());
        }

        timer -= Time.deltaTime;

        if (timer <= 0f && !isOnPlate)
        {
            Explode();
        }
    }

    private IEnumerator BlinkRoutine()
    {
        isBlinking = true;

        while (timer > 0 && !isOnPlate)
        {
            float blinkInterval = Mathf.Lerp(1f / maxBlinkRate, 0.5f, 1f - (timer / totalTime));

            sr.color = warningColor;
            yield return new WaitForSeconds(blinkInterval / 2);

            sr.color = normalColor;
            yield return new WaitForSeconds(blinkInterval / 2);
        }

        isBlinking = false;
    }

    private void Explode()
    {
        if (hasExploded) return; // Prevent multiple explosions
        hasExploded = true;

        //Debug.Log("Burrito exploded!");
        animator.Play("Burrito_Clip");

        // Start explosion scale effect
        StartCoroutine(ExplosionScaleEffect());

        // Deal damage after a brief delay
        StartCoroutine(DelayedExplosionDamage());

        Destroy(gameObject, .5f); // Destroy the burrito
    }

    private IEnumerator DelayedExplosionDamage()
    {
        // Wait for a brief moment (adjust delay as needed)
        yield return new WaitForSeconds(0.1f);

        // Deal damage to nearby players (only once)
        DealExplosionDamage();
    }

    private IEnumerator ExplosionScaleEffect()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 explosionScale = originalScale * 1.5f; // Scale up by 50%
        float scaleUpTime = 0.1f; // Time to scale up
        float scaleDownTime = 0.15f; // Time to scale back down

        // Scale up quickly
        float elapsedTime = 0f;
        while (elapsedTime < scaleUpTime)
        {
            float t = elapsedTime / scaleUpTime;
            transform.localScale = Vector3.Lerp(originalScale, explosionScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localScale = explosionScale;

        // Scale back down
        elapsedTime = 0f;
        while (elapsedTime < scaleDownTime)
        {
            float t = elapsedTime / scaleDownTime;
            transform.localScale = Vector3.Lerp(explosionScale, originalScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originalScale;
    }

    private void DealExplosionDamage()
    {
        // Find all colliders within explosion radius
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        foreach (Collider2D hitCollider in hitColliders)
        {
            // Check if the hit object is a player
            if (hitCollider.CompareTag("Player"))
            {
                // Try to get a health component (assuming your player has one)
                PlayerStats playerStats = hitCollider.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(explosionDamage);
                    Debug.Log($"Player took {explosionDamage} explosion damage!");
                }

                // Alternative: If you're using a different health system, replace above with:
                // IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                // if (damageable != null)
                // {
                //     damageable.TakeDamage(explosionDamage);
                // }

                // Optional: Add knockback effect
                Rigidbody2D playerRb = hitCollider.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 knockbackDirection = (hitCollider.transform.position - transform.position).normalized;
                    float knockbackForce = 80f; // Adjust as needed
                    playerRb.AddForce(knockbackDirection * knockbackForce);
                }
            }
        }
    }

        private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Plate"))
        {
            isOnPlate = true;
            sr.color = normalColor;
            StopAllCoroutines();
            isBlinking = false; // Reset blinking state

            plateLevelEnd = GameObject.FindGameObjectWithTag("Plate");
            plateLevelEnd.GetComponent<PlateLevelEnd>().Plated();

            transform.position = other.transform.position;
            Destroy(rb);
            ThrowableItem item = gameObject.GetComponent<ThrowableItem>();
            Destroy(item);
            Debug.Log("Burrito safely delivered!");
        }

        if (other.CompareTag("Player"))
        {
            isHeld = true;
        }
    }
}
