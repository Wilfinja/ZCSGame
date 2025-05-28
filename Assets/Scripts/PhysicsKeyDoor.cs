using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsKeyDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("The tag of the key object")]
    public string keyTag = "Key";

    [Tooltip("Maximum angle the door can open to")]
    public float maxOpenAngle = 90f;

    [Tooltip("How much force to apply when unlocking the door")]
    public float initialUnlockForce = 5f;

    [Tooltip("How strong the hinge spring is")]
    public float springStrength = 0.1f;

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

    void Start()
    {
        // Get or add needed components
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.mass = 2f;
            rb.drag = 0.5f;
            rb.angularDrag = 1f;
        }

        // Store initial rotation
        initialRotationZ = transform.rotation.eulerAngles.z;

        // Setup hinge joint
        hinge = GetComponent<HingeJoint2D>();
        if (hinge == null)
        {
            hinge = gameObject.AddComponent<HingeJoint2D>();
            hinge.connectedBody = null; // Connect to world
            hinge.anchor = new Vector2(-0.5f, 0); // Position at left edge of door

            // Set door limits
            JointAngleLimits2D limits = new JointAngleLimits2D();
            limits.min = 0;
            limits.max = maxOpenAngle;
            hinge.limits = limits;
            hinge.useLimits = true;

            // Add some bounce using a spring
            JointMotor2D motor = new JointMotor2D();
            motor.motorSpeed = 0;
            motor.maxMotorTorque = springStrength;
            hinge.motor = motor;
        }

        // Lock the door at start
        rb.bodyType = RigidbodyType2D.Static;

        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (unlockSound != null || doorStopSound != null || lockedSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // Check the door angle for sound effects
        if (!isLocked)
        {
            // Calculate current angle
            float angle = Mathf.DeltaAngle(initialRotationZ, transform.rotation.eulerAngles.z);

            // Check if door has hit the maximum opening or close to it
            if (!soundPlayed && doorStopSound != null && audioSource != null && angle >= maxOpenAngle * 0.95f)
            {
                audioSource.PlayOneShot(doorStopSound);
                soundPlayed = true;
            }
            else if (angle < maxOpenAngle * 0.8f)
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

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if this is a key
        if (collision.gameObject.CompareTag(keyTag))
        {
            if (isLocked)
            {
                UnlockDoor(collision);

                // You may want to destroy the key or deactivate it
                // Uncomment the line below if you want the key to disappear after use
                // Destroy(collision.gameObject);
            }
        }
        else if (isLocked && timeSinceLastLockSound <= 0)
        {
            // Play locked sound when something hits the locked door
            // Only play sound periodically to avoid sound spam
            if (lockedSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(lockedSound);
                timeSinceLastLockSound = 0.5f; // Set cooldown
            }

            // Optionally add visual feedback that door is locked
            StartCoroutine(DoorShakeEffect(0.2f, 0.03f));
        }
        else if (!isLocked)
        {
            // Door is unlocked, apply physical force from collision
            ContactPoint2D contact = collision.GetContact(0);
            Vector2 contactPoint = contact.point;

            // Calculate push direction
            Vector2 doorCenter = transform.TransformPoint(hinge.anchor);
            Vector2 contactDirection = (contactPoint - doorCenter).normalized;

            // Apply force at the contact point
            rb.AddForceAtPosition(collision.relativeVelocity * collision.gameObject.GetComponent<Rigidbody2D>().mass * 0.5f, contactPoint);
        }
    }

    public void UnlockDoor(Collision2D collision)
    {
        if (!isLocked)
            return;

        isLocked = false;

        // Change door to dynamic
        rb.bodyType = RigidbodyType2D.Dynamic;

        // Enable using limits
        hinge.useLimits = true;
        hinge.useMotor = false;

        // Get the contact point
        ContactPoint2D contact = collision.GetContact(0);
        Vector2 contactPoint = contact.point;

        // Calculate push direction
        Vector2 doorCenter = transform.TransformPoint(hinge.anchor);
        Vector2 contactDirection = (contactPoint - doorCenter).normalized;

        // Apply initial force in the direction of collision
        rb.AddForceAtPosition(collision.relativeVelocity.normalized * initialUnlockForce, contactPoint);

        // Play sound if available
        if (unlockSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(unlockSound);
        }
    }

    // Public method to reset/lock the door
    public void ResetDoor()
    {
        // Stop all movement
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;

        // Reset rotation
        transform.rotation = Quaternion.Euler(0, 0, initialRotationZ);

        // Lock door
        rb.bodyType = RigidbodyType2D.Static;
        isLocked = true;
        soundPlayed = false;
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
