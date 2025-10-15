using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Eatable
{
    Yes,
    No
}

public class ThrowableItem : MonoBehaviour
{
    [Header("Item Properties")]
    [SerializeField] private Eatable eatable = Eatable.Yes;
    [SerializeField] private float throwForce = 10f;
    //[SerializeField] private float torqueForce = 5f;

    // This is the property that Dog.cs will check
    [HideInInspector] public bool willEat = true;

    [Header("Physics")]
    [SerializeField] private Collider2D bounce;

    private Rigidbody2D rb;
    private bool isHeld = false;
    private SpriteRenderer spriteRenderer;

    private bool isMonitoring = false;

    private void Awake()
    {
        // Set willEat based on the eatable enum value right at the start
        willEat = (eatable == Eatable.Yes);
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Ensure willEat is set correctly at start
        willEat = (eatable == Eatable.Yes);

        // Visual indication of whether the item is edible (optional debugging feature)
        if (spriteRenderer != null)
        {
            // If not eatable, add a slight red tint so you can tell which items dogs won't chase
            //if (!willEat)
            //{
            //    Color currentColor = spriteRenderer.color;
            //    spriteRenderer.color = new Color(
            //        currentColor.r,
            //        currentColor.g * 0.7f,  // Reduce green
            //        currentColor.b * 0.7f,  // Reduce blue
            //        currentColor.a          // Keep alpha the same
            //    );
            //}
        }
    }

    public void Pickup(Transform holder)
    {
        bounce.enabled = false;
        isHeld = true;

        rb.bodyType = RigidbodyType2D.Kinematic;
        transform.SetParent(holder);
        transform.localPosition = Vector3.zero;
    }

    public void Throw(Vector2 throwDirection)
    {
        if (!isHeld) return;

        StartCoroutine(ReactivateRB());
        isHeld = false;
        transform.SetParent(null);
        rb.bodyType = RigidbodyType2D.Dynamic;
        transform.position = GameObject.FindGameObjectWithTag("Throw").transform.position;

        // Clear any existing velocity
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        //Debug.Log($"About to apply force: {throwDirection * throwForce}");
        rb.AddForce(throwDirection * throwForce, ForceMode2D.Impulse);

        // Start monitoring velocity
        isMonitoring = true;
        StartCoroutine(MonitorVelocity());
        //rb.AddTorque(torqueForce, ForceMode2D.Impulse);
    }

    IEnumerator ReactivateRB()
    {
        yield return new WaitForSeconds(.01f);
        bounce.enabled = true;
    }

    // Check if item is currently being held
    public bool IsHeld()
    {
        return isHeld;
    }

    // Utility method to change whether the item is eatable at runtime
    public void SetEatable(bool isEatable)
    {
        eatable = isEatable ? Eatable.Yes : Eatable.No;
        willEat = isEatable;

        // Update visual cue if desired
        if (spriteRenderer != null)
        {
            if (!willEat)
            {
                Color baseColor = Color.white;
                spriteRenderer.color = new Color(baseColor.r, baseColor.g * 0.7f, baseColor.b * 0.7f, baseColor.a);
            }
            else
            {
                spriteRenderer.color = Color.white;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        isMonitoring = false; // Stop monitoring when it hits something
        if (!isHeld)
        {
            //Debug.Log($"Item collided with: {collision.gameObject.name} at velocity: {rb.velocity}");
        }
    }

    void Update()
    {
        if (!isHeld && rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            //Debug.Log($"Item velocity: {rb.velocity}");
        }
    }

    private IEnumerator MonitorVelocity()
    {
        while (isMonitoring && rb != null)
        {
            if (Mathf.Abs(rb.linearVelocity.x) > 0.01f) // If horizontal velocity appears
            {
                //Debug.Log($"Horizontal velocity detected! Velocity: {rb.velocity}, Position: {transform.position}");
                //Debug.Log($"All forces acting on object: Check for other scripts or physics interactions!");
            }
            yield return new WaitForFixedUpdate();
        }
    }
}