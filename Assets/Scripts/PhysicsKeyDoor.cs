using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsKeyDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("The tag of the key object")]
    public string keyTag = "Key";

    [Tooltip("Maximum angle the door can open to (both directions)")]
    public float maxOpenAngle = 90f;

    [Tooltip("How much force to apply when unlocking the door")]
    public float initialUnlockForce = 5f;

    [Tooltip("Spring damping ratio (0-1, higher = less bouncy)")]
    public float springDamping = 0.7f;

    [Tooltip("Spring frequency (how fast it returns to center)")]
    public float springFrequency = 2f;

    [Tooltip("Sound to play when door unlocks")]
    public AudioClip unlockSound;

    [Tooltip("Sound to play when door hits maximum angle")]
    public AudioClip doorStopSound;

    [Tooltip("Sound to play when locked door is hit")]
    public AudioClip lockedSound;

    // Private variables
    private bool isLocked = true;
    private float initialRotationZ;
    private HingeJoint2D hinge;
    private Rigidbody2D rb;
    private AudioSource audioSource;
    private bool soundPlayed = false;
    private float timeSinceLastLockSound = 0f;
    private Collider2D doorCollider;

    void Start()
    {
        // Store initial rotation first
        initialRotationZ = transform.rotation.eulerAngles.z;

        // Get collider
        doorCollider = GetComponent<Collider2D>();
        if (doorCollider == null)
        {
            Debug.LogError("PhysicsKeyDoor requires a Collider2D component!");
        }

        // Get or add Rigidbody2D but keep it completely inactive when locked
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // Completely disable rigidbody when locked
        rb.bodyType = RigidbodyType2D.Kinematic; // Use Kinematic instead of Static
        rb.gravityScale = 0;
        rb.mass = 2f;
        rb.drag = 2f;
        rb.angularDrag = 5f;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;

        // Setup hinge joint but keep it completely disabled
        hinge = GetComponent<HingeJoint2D>();
        if (hinge != null)
        {
            // If hinge exists, destroy it initially - we'll recreate it when needed
            DestroyImmediate(hinge);
            hinge = null;
        }

        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (unlockSound != null || doorStopSound != null || lockedSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // Only process door physics if unlocked
        if (!isLocked && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            // Calculate current angle relative to initial position
            float currentZ = transform.rotation.eulerAngles.z;
            float angle = Mathf.DeltaAngle(initialRotationZ, currentZ);

            // Manually enforce angle limits
            if (Mathf.Abs(angle) > maxOpenAngle)
            {
                // Stop the door and clamp to limit
                rb.angularVelocity = 0;
                float clampedAngle = Mathf.Clamp(angle, -maxOpenAngle, maxOpenAngle);
                transform.rotation = Quaternion.Euler(0, 0, initialRotationZ + clampedAngle);

                // Add small bounce back force
                float bounceForce = (angle > 0 ? -1 : 1) * 2f;
                rb.AddTorque(bounceForce, ForceMode2D.Impulse);
            }

            // Check if door has hit the maximum opening in either direction
            if (!soundPlayed && doorStopSound != null && audioSource != null &&
                (angle >= maxOpenAngle * 0.95f || angle <= -maxOpenAngle * 0.95f))
            {
                audioSource.PlayOneShot(doorStopSound);
                soundPlayed = true;
            }
            else if (Mathf.Abs(angle) < maxOpenAngle * 0.8f)
            {
                soundPlayed = false;
            }
        }

        // Timer for locked sound cooldown
        if (timeSinceLastLockSound > 0)
        {
            timeSinceLastLockSound -= Time.deltaTime;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Only check for key collision when locked
        if (isLocked && other.CompareTag(keyTag))
        {
            // Get the key's rigidbody to calculate collision velocity
            Rigidbody2D keyRb = other.GetComponent<Rigidbody2D>();
            Vector2 keyVelocity = keyRb != null ? keyRb.velocity : Vector2.right; // Default direction if no rigidbody

            UnlockDoor(other.transform.position, keyVelocity);

            // Destroy the key after use
            Destroy(other.gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Handle key collision
        if (collision.gameObject.CompareTag(keyTag))
        {
            if (isLocked)
            {
                UnlockDoor(collision.contacts[0].point, collision.relativeVelocity);
                Destroy(collision.gameObject);
                return;
            }
        }

        // Handle locked door feedback
        if (isLocked && timeSinceLastLockSound <= 0)
        {
            // Play locked sound when something hits the locked door
            if (lockedSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(lockedSound);
                timeSinceLastLockSound = 0.5f;
            }

            // Optional: Add visual feedback that door is locked
            // StartCoroutine(DoorShakeEffect(0.2f, 0.03f));
        }
        else if (!isLocked && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            // Door is unlocked, apply physical force from collision
            ContactPoint2D contact = collision.GetContact(0);
            Vector2 contactPoint = contact.point;

            // Calculate force magnitude based on collision
            Rigidbody2D otherRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (otherRb != null)
            {
                float forceMagnitude = collision.relativeVelocity.magnitude * otherRb.mass * 0.3f;
                rb.AddForceAtPosition(collision.relativeVelocity.normalized * forceMagnitude, contactPoint);
            }
        }
    }

    public void UnlockDoor(Vector3 contactPoint, Vector2 impactVelocity)
    {
        if (!isLocked)
            return;

        Debug.Log("Door unlocked!");
        isLocked = false;

        // Create and setup hinge joint
        if (hinge == null)
        {
            hinge = gameObject.AddComponent<HingeJoint2D>();
            hinge.connectedBody = null; // Connect to world
            hinge.anchor = new Vector2(-0.5f, 0); // Position at left edge of door
            hinge.useLimits = false;
            hinge.useMotor = false;
        }

        // Change door to dynamic
        rb.bodyType = RigidbodyType2D.Dynamic;

        // Clear any residual velocity
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;

        // Apply initial unlock force based on impact
        Vector2 forceDirection = impactVelocity.normalized;
        if (forceDirection == Vector2.zero)
            forceDirection = Vector2.right; // Default direction

        rb.AddForceAtPosition(forceDirection * initialUnlockForce, contactPoint);

        // Play unlock sound
        if (unlockSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(unlockSound);
        }
    }

    // Overload for Collision2D (backward compatibility)
    public void UnlockDoor(Collision2D collision)
    {
        ContactPoint2D contact = collision.GetContact(0);
        UnlockDoor(contact.point, collision.relativeVelocity);
    }

    // Public method to reset/lock the door
    public void ResetDoor()
    {
        // Destroy hinge joint
        if (hinge != null)
        {
            DestroyImmediate(hinge);
            hinge = null;
        }

        // Stop all movement and reset to kinematic
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Reset rotation to initial position
        transform.rotation = Quaternion.Euler(0, 0, initialRotationZ);

        // Lock door
        isLocked = true;
        soundPlayed = false;
    }

    // Method to check if door is locked (useful for debugging)
    public bool IsLocked()
    {
        return isLocked;
    }

    // Door shake effect for when a locked door is hit
    System.Collections.IEnumerator DoorShakeEffect(float duration, float magnitude)
    {
        Vector3 originalPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.position = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
    }
}
