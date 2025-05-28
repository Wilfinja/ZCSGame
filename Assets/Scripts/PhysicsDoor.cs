using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("The minimum velocity required to unlock the door")]
    public float thresholdVelocity = 3f;

    [Tooltip("Maximum angle the door can open to")]
    public float maxOpenAngle = 90f;

    [Tooltip("How much force to apply when unlocking the door")]
    public float initialUnlockForce = 5f;

    [Tooltip("How strong the hinge spring is")]
    public float springStrength = 0.1f;

    [Header("Optional Settings")]
    [Tooltip("Tag of objects that can open the door")]
    public string activatorTag = "Player";

    [Tooltip("Sound to play when door opens")]
    public AudioClip openSound;

    [Tooltip("Sound to play when door hits maximum angle")]
    public AudioClip doorStopSound;

    // Private variables
    private bool isLocked = true;
    private float currentAngle = 0f;
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

            // Lock door initially
            hinge.useMotor = false;

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
        if (audioSource == null && (openSound != null || doorStopSound != null))
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
            Vector2 forceDirection = (contactPoint - doorCenter).normalized;

            // Apply force at the contact point
            rb.AddForceAtPosition(collision.relativeVelocity * collision.gameObject.GetComponent<Rigidbody2D>().mass * 0.5f, contactPoint);
        }
    }

    public void UnlockDoor(Vector2 contactPoint, Vector2 velocity)
    {
        rb = GetComponent<Rigidbody2D>();
        hinge = GetComponent<HingeJoint2D>();

        if (!isLocked)
            return;

        isLocked = false;

        // Change door to dynamic
        rb.bodyType = RigidbodyType2D.Dynamic;

        // Enable using limits but not motor
        hinge.useLimits = true;
        hinge.useMotor = false;

        // Calculate push direction
        Vector2 doorCenter = transform.TransformPoint(hinge.anchor);
        Vector2 contactDirection = (contactPoint - doorCenter).normalized;

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
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;

        // Reset rotation
        transform.rotation = Quaternion.Euler(0, 0, initialRotationZ);

        // Lock door
        rb.bodyType = RigidbodyType2D.Static;
        isLocked = true;
        soundPlayed = false;
    }
}
