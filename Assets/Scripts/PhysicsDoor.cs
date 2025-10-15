using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("The minimum velocity required to unlock the door")]
    public float thresholdVelocity = 3f;

    [Tooltip("Maximum angle the door can open to (both directions)")]
    public float maxOpenAngle = 90f;

    [Tooltip("How much force to apply when unlocking the door")]
    public float initialUnlockForce = 5f;

    [Tooltip("Spring damping ratio (0-1, higher = less bouncy)")]
    public float springDamping = 0.7f;

    [Tooltip("Spring frequency (how fast it returns to center)")]
    public float springFrequency = 2f;

    [Header("Optional Settings")]
    [Tooltip("Tag of objects that can open the door")]
    public string activatorTag = "Player";

    [Tooltip("Sound to play when door opens")]
    public AudioClip openSound;

    [Tooltip("Sound to play when door hits maximum angle")]
    public AudioClip doorStopSound;

    // Private variables
    private bool isLocked = true;
    //private float currentAngle = 0f;
    private float initialRotationZ;
    private HingeJoint2D hinge;
    private Rigidbody2D rb;
    private AudioSource audioSource;
    private bool soundPlayed = false;

    void Start()
    {
        // Get or add needed components
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.mass = 2f;
            rb.linearDamping = 2f;        // Increased drag to reduce excessive spinning
            rb.angularDamping = 5f; // Increased angular drag to prevent wild spinning
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

            // Disable joint limits initially - we'll handle them manually
            hinge.useLimits = false;
            hinge.useMotor = false;

            // Configure spring to bring door back to center
            JointSuspension2D spring = new JointSuspension2D();
            spring.dampingRatio = springDamping;  // Damping to reduce bouncing
            spring.frequency = springFrequency;   // Spring frequency
            spring.angle = 0f;                   // Target angle (closed position)
            //hinge.suspension = spring;
            //hinge.useSpring = true;
        }

        // Lock the door at start
        rb.bodyType = RigidbodyType2D.Static;

        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (openSound != null || doorStopSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // Check the door angle for sound effects and enforce limits
        if (!isLocked)
        {
            // Calculate current angle relative to initial position
            float currentZ = transform.rotation.eulerAngles.z;
            float angle = Mathf.DeltaAngle(initialRotationZ, currentZ);

            // Manually enforce angle limits if joint limits aren't working
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
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the colliding object has the correct tag
        if (!string.IsNullOrEmpty(activatorTag) && !collision.gameObject.CompareTag(activatorTag))
        {
            return;
        }

        // Calculate the impact velocity
        float impactVelocity = collision.relativeVelocity.magnitude;

        // Debug the velocity
        Debug.Log($"Impact velocity: {impactVelocity} from {collision.gameObject.name}");

        // Get the contact point
        ContactPoint2D contact = collision.GetContact(0);
        Vector2 contactPoint = contact.point;

        // If the door is locked and impact exceeds threshold
        if (isLocked && impactVelocity >= thresholdVelocity)
        {
            UnlockDoor(contactPoint, collision.relativeVelocity);
        }
        else if (!isLocked)
        {
            // Door is already unlocked, apply force based on collision
            Vector2 doorCenter = transform.TransformPoint(hinge.anchor);

            // Calculate force magnitude based on collision
            float forceMagnitude = collision.relativeVelocity.magnitude *
                                 collision.gameObject.GetComponent<Rigidbody2D>().mass * 0.3f;

            // Apply controlled force at the contact point
            rb.AddForceAtPosition(collision.relativeVelocity.normalized * forceMagnitude, contactPoint);
        }
    }

    public void UnlockDoor(Vector2 contactPoint, Vector2 velocity)
    {
        if (!isLocked)
            return;

        isLocked = false;

        // Change door to dynamic
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;

        // Make sure limits are disabled since we handle them manually
        hinge = gameObject.GetComponent<HingeJoint2D>();
        hinge.useLimits = false;
        hinge.useMotor = false;

        // Apply initial force to create movement in appropriate direction
        rb.AddForceAtPosition(velocity.normalized * initialUnlockForce, contactPoint);

        // Play sound if available
        if (openSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(openSound);
        }
    }

    // Public method to reset/lock the door
    public void ResetDoor()
    {
        // Stop all movement
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;

        // Reset rotation
        transform.rotation = Quaternion.Euler(0, 0, initialRotationZ);

        // Lock door
        rb.bodyType = RigidbodyType2D.Static;
        isLocked = true;
        soundPlayed = false;
    }
}
