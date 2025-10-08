using System.Collections;
using TMPro;
using UnityEngine;

public class Burrito : MonoBehaviour
{
    [Header("Explosion Timer")]
    public float totalTime = 14f; // Total time before explosion
    private float timer;

    [Header("Countdown Display")]
    public TextMeshPro countdownTextMesh; // Changed from TextMesh to TextMeshPro
    public float displayOffset = 1f; // How far above the burrito to show the countdown

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

        // Create countdown TextMeshPro if not assigned
        if (countdownTextMesh == null)
        {
            CreateCountdownTextMesh();
        }

        // Initially hide the countdown
        if (countdownTextMesh != null)
        {
            countdownTextMesh.gameObject.SetActive(false);
        }
    }

    private void CreateCountdownTextMesh()
    {
        // Create a TextMeshPro object
        GameObject textObj = new GameObject("CountdownText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = Vector3.up * displayOffset;

        countdownTextMesh = textObj.AddComponent<TextMeshPro>();
        countdownTextMesh.text = "14.0";
        countdownTextMesh.fontSize = 4; // TMP uses different sizing
        countdownTextMesh.color = Color.blue;
        countdownTextMesh.alignment = TextAlignmentOptions.Center;

        // Set the font to be sharp
        countdownTextMesh.fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field");

        // Enable proper settings for crisp text
        countdownTextMesh.enableAutoSizing = false;
        countdownTextMesh.fontStyle = FontStyles.Bold; // Makes it more readable

        // Make the text face the camera
        if (Camera.main != null)
        {
            textObj.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    void Update()
    {
        if (!isOnPlate && !isHeld)
        {
            // Hide countdown when not active
            if (countdownTextMesh != null)
            {
                countdownTextMesh.gameObject.SetActive(false);
            }
            return;
        }

        // Show countdown when active
        if (countdownTextMesh != null && !isOnPlate)
        {
            countdownTextMesh.gameObject.SetActive(true);
            UpdateCountdownDisplay();
        }

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

    private void UpdateCountdownDisplay()
    {
        if (countdownTextMesh != null)
        {
            // Don't show negative numbers
            if (timer <= 0f)
            {
                countdownTextMesh.gameObject.SetActive(false);
                return;
            }

            // Format the timer to show one decimal place
            countdownTextMesh.text = timer.ToString("F1");

            // Change color based on time remaining
            if (timer <= 2f)
            {
                countdownTextMesh.color = Color.red;
            }
            else if (timer <= 4f)
            {
                countdownTextMesh.color = Color.yellow;
            }
            else
            {
                countdownTextMesh.color = Color.white;
            }

            // Optional: Scale text based on urgency
            float scale = timer <= 3f ? Mathf.Lerp(1.5f, 1f, timer / 3f) : 1f;
            countdownTextMesh.transform.localScale = Vector3.one * scale;

            // Keep text facing camera
            if (Camera.main != null)
            {
                countdownTextMesh.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            }
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

        // Hide countdown on explosion
        if (countdownTextMesh != null)
        {
            countdownTextMesh.gameObject.SetActive(false);
        }

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

            // Hide countdown when on plate
            if (countdownTextMesh != null)
            {
                countdownTextMesh.gameObject.SetActive(false);
            }

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
