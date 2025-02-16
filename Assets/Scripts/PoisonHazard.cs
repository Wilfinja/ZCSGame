using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonHazard : MonoBehaviour
{
    [Header("Cloud Properties")]
    public float maxSize = 3f;
    public float minSize = 0.5f;
    public float growthTime = 1f;
    public float activeTime = 3f;
    public float shrinkTime = 1f;
    public bool destroyAfterLoops = false;
    public int numberOfLoops = 3;  // Only used if destroyAfterLoops is true

    [Header("Damage Settings")]
    public int damagePerTick = 10;
    public float damageTickRate = 0.5f;

    [Header("Visual Settings")]
    public Color poisonColor = new Color(0.4f, 0.8f, 0.4f, 0.6f);

    private float currentDamageTimer;
    private Vector3 originalScale;
    private SpriteRenderer spriteRenderer;
    private int currentLoop = 0;

    [SerializeField]
    private float rotationSpeed = 100f; // Degrees per second

    [SerializeField]
    private bool rotateClockwise = true;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = poisonColor;
        }

        originalScale = transform.localScale;
        transform.localScale = Vector3.one * minSize;

        // Start the cloud lifecycle
        StartCoroutine(CloudLoop());
    }

    private void Update()
    {
        float direction = rotateClockwise ? -1f : 1f;
        transform.Rotate(0f, 0f, rotationSpeed * direction * Time.deltaTime);
    }

    private IEnumerator CloudLoop()
    {
        while (!destroyAfterLoops || currentLoop < numberOfLoops)
        {
            // Growth phase
            float elapsedTime = 0f;
            Vector3 startScale = Vector3.one * minSize;
            Vector3 endScale = Vector3.one * maxSize;

            while (elapsedTime < growthTime)
            {
                float growthProgress = elapsedTime / growthTime;
                transform.localScale = Vector3.Lerp(startScale, endScale, growthProgress);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Active phase at max size
            yield return new WaitForSeconds(activeTime);

            // Shrink phase
            elapsedTime = 0f;
            while (elapsedTime < shrinkTime)
            {
                float shrinkProgress = elapsedTime / shrinkTime;
                transform.localScale = Vector3.Lerp(endScale, startScale, shrinkProgress);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(activeTime);

            currentLoop++;
        }

        // Only destroy if we're using limited loops
        if (destroyAfterLoops)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        currentDamageTimer += Time.deltaTime;

        // Check if it's time to deal damage
        if (currentDamageTimer >= damageTickRate)
        {
            // Try to get health component
            PlayerStats healthComponent = other.GetComponent<PlayerStats>();
            DamageFlash damageFlash = other.GetComponent<DamageFlash>();
            if (healthComponent != null)
            {
                healthComponent.TakeDamage(damagePerTick);
                damageFlash.Flash();
            }

            currentDamageTimer = 0f;
        }
    }

    // Public method to stop the cloud if needed
    public void StopCloud()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }
}
