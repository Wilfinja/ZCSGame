using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ClickToMove : MonoBehaviour
{
    private Transform ptransform;

    private Rigidbody2D rb;

    private float pushForce = 20f;

    private Animator animator;

    private bool pullDisabled;
    private bool pushDisabled;

    public float pushCooldown;
    public float pullCooldown;

    public float slowAmount;

    public float pushForceMultiplier = 1f;

    public float minVelocityThreshold = 0.1f;

    public Transform itemHold;

    public static ClickToMove Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        ptransform = this.transform;

        animator = GetComponent<Animator>();

        pullDisabled = false;
        pushDisabled = false;

        pushCooldown = .5f;
        pullCooldown = .25f;
        slowAmount = 1f;
    }

    public void gameOver()
    {
        //Debug.Log("GAME OVER ClickToMove");

        animator.Play("Game Over");
    }

    public void Push()
    {
        if (!pushDisabled)
        {
            //Debug.Log("Pushed");

            animator.Play("[Push]");

            Vector2 direction = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - ptransform.position).normalized;

            Vector2 push = -direction * pushForce;

            rb.AddForce(push, ForceMode2D.Impulse);

            pushDisabled = true;

            StartCoroutine(PushCooldown(pushCooldown / slowAmount));
        }
    }

    public void Pull()
    {
        if (!pullDisabled)
        {
            //Debug.Log("Pulled");

            animator.Play("[Pull]");

            Vector2 direction = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - ptransform.position).normalized;

            Vector2 pull = direction * (pushForce / 5);

            rb.AddForce(pull, ForceMode2D.Impulse);

            pullDisabled = true;

            StartCoroutine(PullCooldown(pullCooldown / slowAmount));
        }
    }

    public void Throw()
    {
        ThrowItem();
    }

    public void Idle()
    {
        animator.Play("Idle");
    }

    IEnumerator PullCooldown(float wait)
    {
        yield return new WaitForSeconds(wait);

        Idle();

        pullDisabled = false;
    }

    IEnumerator PushCooldown(float wait)
    {
        yield return new WaitForSeconds(wait);

        Idle();

        pushDisabled = false;
    }

    //private void OnCollisionEnter2D(Collision2D collision)
    //{
    //    // Check if the colliding object has a Rigidbody2D
    //    Rigidbody2D pusherRigidbody = GetComponent<Rigidbody2D>();
    //    Rigidbody2D pusheeRigidbody = collision.gameObject.GetComponent<Rigidbody2D>();
    //
    //    if (pusherRigidbody != null && pusheeRigidbody != null)
    //    {
    //        // Get the current velocity of the pushing object
    //        Vector2 currentVelocity = pusherRigidbody.velocity;
    //
    //        // Check if the velocity is significant enough to push
    //        if (currentVelocity.magnitude >= minVelocityThreshold)
    //        {
    //            // Calculate push direction and force
    //            Vector2 pushDirection = currentVelocity.normalized;
    //            float pushForce = currentVelocity.magnitude * pushForceMultiplier;
    //
    //            // Apply the push force to the other object
    //            pusheeRigidbody.AddForce(pushDirection * pushForce, ForceMode2D.Impulse);
    //
    //            // Optional: Debug log to verify push
    //           Debug.Log($"Pushed {collision.gameObject.name} with force: {pushForce}");
    //        }
    //    }
    //}

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Pickup item logic
        ThrowableItem item = other.GetComponent<ThrowableItem>();
        if (item != null && transform.childCount == 2)
        {
            item.Pickup(itemHold);
        }
    }

    public void BeginSlow(float amount)
    {
        slowAmount = amount;
        StartCoroutine(Slowed());
    }

    IEnumerator Slowed()
    {
        yield return new WaitForSeconds(3);
        slowAmount = 1;
    }

    public void ThrowItem()
    {
        // Find the first throwable item in children
        ThrowableItem item = GetComponentInChildren<ThrowableItem>();
        if (item != null)
        {
            // Calculate throw direction (towards mouse position)
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 throwDirection = (mousePosition - (Vector2)transform.position).normalized;

            item.Throw(throwDirection);
        }
    }
}
