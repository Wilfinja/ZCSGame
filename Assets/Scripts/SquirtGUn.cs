using System.Collections;
using TMPro;
using UnityEngine;

public class SquirtGUn : MonoBehaviour
{
    [Header("Ammo Settings")]
    public int maxAmmo = 10;
    private int currentAmmo;

    [Header("Shooting Settings")]
    public GameObject waterProjectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 15f;

    [Header("Ammo Display")]
    public TextMeshPro ammoTextMesh;
    public float displayOffset = 1f;

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip emptyClickSound;
    private AudioSource audioSource;

    [Header("Visual Feedback")]
    public Color fullAmmoColor = Color.cyan;
    public Color lowAmmoColor = Color.yellow;
    public Color emptyAmmoColor = Color.red;

    private bool shootDisabled;
    private Animator anim;
    private bool isHeld = false;
    private Rigidbody2D rb;
    private Camera mainCamera;
    private Transform mtransfrom;

    void Start()
    {
        currentAmmo = maxAmmo;
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;

        mtransfrom = GetComponent<Transform>();

        anim = GetComponent<Animator>();

        // Get or create AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Create ammo display if not assigned
        if (ammoTextMesh == null)
        {
            CreateAmmoDisplay();
        }

        UpdateAmmoDisplay();
    }

    private void CreateAmmoDisplay()
    {
        GameObject textObj = new GameObject("AmmoDisplay");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = Vector3.up * displayOffset;

        ammoTextMesh = textObj.AddComponent<TextMeshPro>();
        ammoTextMesh.text = maxAmmo.ToString();
        ammoTextMesh.fontSize = 4;
        ammoTextMesh.color = fullAmmoColor;
        ammoTextMesh.alignment = TextAlignmentOptions.Center;

        // Set the font to be sharp
        ammoTextMesh.fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field");
        ammoTextMesh.enableAutoSizing = false;
        ammoTextMesh.fontStyle = FontStyles.Bold;

        // Make the text face the camera
        if (mainCamera != null)
        {
            textObj.transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward);
        }

        // Initially hide the display
        ammoTextMesh.gameObject.SetActive(false);
    }

    void Update()
    {
        // Only show ammo display when held
        if (ammoTextMesh != null)
        {
            ammoTextMesh.gameObject.SetActive(isHeld);

            if (isHeld)
            {
                // Keep text facing camera
                if (mainCamera != null)
                {
                    ammoTextMesh.transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward);
                }
            }
        }

        if (isHeld)
        {
            LAMouse();
        }

        // Check for shoot input when held
        //if (isHeld)
        //{
        //    Shoot();
        //}
    }

    public void Shoot()
    {
        if (currentAmmo > 0 && !shootDisabled && isHeld)
        {
            shootDisabled = true;

            // Fire projectile
            if (waterProjectilePrefab != null && firePoint != null)
            {
                // Get direction to mouse cursor
                Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 1f;

                Vector2 shootDirection = (mousePos - firePoint.position).normalized;
                float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;

                anim.Play("Fire");

                // Instantiate projectile
                GameObject projectile = Instantiate(waterProjectilePrefab, firePoint.position, Quaternion.Euler(0, 0, angle - 180));

                // Add velocity to projectile
                Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
                if (projectileRb != null)
                {
                    projectileRb.linearVelocity = shootDirection * projectileSpeed;
                }
            }

            // Decrease ammo
            currentAmmo--;
            UpdateAmmoDisplay();

            // Play shoot sound
            if (shootSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(shootSound);
            }

            Debug.Log($"Squirt! Ammo remaining: {currentAmmo}");

            StartCoroutine(wait());
        }
        else if (!shootDisabled && isHeld)
        {
            shootDisabled = true;

            // Empty gun - play click sound
            if (emptyClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(emptyClickSound);
            }

            Debug.Log("Out of ammo! Need to refill at water cooler.");

            StartCoroutine(wait());
        }
        
    }

    private void LAMouse()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Camera.main.transform.position.z; // Add this line

        Vector2 direction = Camera.main.ScreenToWorldPoint(mouseScreenPos) - mtransfrom.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle + 90, Vector3.forward);
        mtransfrom.rotation = rotation;

    }

    private void UpdateAmmoDisplay()
    {
        if (ammoTextMesh == null) return;

        ammoTextMesh.text = currentAmmo.ToString();

        // Change color based on ammo remaining
        float ammoPercentage = (float)currentAmmo / maxAmmo;

        if (currentAmmo == 0)
        {
            ammoTextMesh.color = emptyAmmoColor;
        }
        else if (ammoPercentage <= 0.3f) // 30% or less
        {
            ammoTextMesh.color = lowAmmoColor;
        }
        else
        {
            ammoTextMesh.color = fullAmmoColor;
        }

        // Optional: Scale text when low/empty
        if (currentAmmo == 0)
        {
            ammoTextMesh.transform.localScale = Vector3.one * 1.3f;
        }
        else
        {
            ammoTextMesh.transform.localScale = Vector3.one;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Refill at water cooler
        if (other.CompareTag("WaterCooler"))
        {
            currentAmmo = maxAmmo;
            UpdateAmmoDisplay();

            Debug.Log("Squirt gun refilled!");

            // Optional: Play refill sound
            if (audioSource != null)
            {
                // You can add a refill sound here if you want
                // audioSource.PlayOneShot(refillSound);
            }

            // Make the gun pickupable again if it was thrown
            ThrowableItem throwable = GetComponent<ThrowableItem>();
            if (throwable != null)
            {
                // Reset any throwable properties if needed
            }
        }

        // Track when player picks up the gun
        if (other.CompareTag("Player"))
        {
            isHeld = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Track when player drops the gun
        if (other.CompareTag("Player"))
        {
            isHeld = false;
        }
    }

    // Public method to set held state (call from your player pickup system)
    public void SetHeld(bool held)
    {
        isHeld = held;
    }

    // Public method to check if gun has ammo
    public bool HasAmmo()
    {
        return currentAmmo > 0;
    }

    // Public method to get current ammo
    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }

    private IEnumerator wait()
    {
        yield return new WaitForSeconds(.5f);
        shootDisabled = false;
    }
}
