using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickToMove : MonoBehaviour
{
    private Transform ptransform;

    private Rigidbody2D rb;

    private float pushForce = 20f;

    private Animator animator;

    private bool pullDisabled;
    private bool pushDisabled;

    public bool pausePush;

    public float pushCooldown;
    public float pullCooldown;

    public float slowAmount;

    public float pushForceMultiplier = 1f;

    public float minVelocityThreshold = 0.1f;

    public Transform itemHold;
    public ThrowableItem heldItem; // Track the currently held item

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
        pausePush = false;

        pushCooldown = .5f;
        pullCooldown = .25f;
        slowAmount = 1f;

        heldItem = null; // Initialize held item to null
    }

    public void gameOver()
    {
        //Debug.Log("GAME OVER ClickToMove");

        animator.Play("Game Over");
    }

    public void Push()
    {
        if (!pushDisabled && !pausePush)
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
        if (!pullDisabled && !pausePush)
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Pickup item logic - only pick up if we're not already holding an item
        ThrowableItem item = other.GetComponent<ThrowableItem>();
        if (item != null && heldItem == null)
        {
            item.Pickup(itemHold);
            heldItem = item; // Store reference to the held item
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
        // If we have a held item, throw it
        if (heldItem != null)
        {
            // Get mouse position as percentage of screen
            Vector2 mouseScreenPercent = new Vector2(
                Input.mousePosition.x / Screen.width,
                Input.mousePosition.y / Screen.height
            );

            // Convert to world coordinates using camera bounds
            Camera cam = Camera.main;
            float camHeight = cam.orthographicSize * 2f;
            float camWidth = camHeight * cam.aspect;

            Vector3 camPos = cam.transform.position;
            Vector2 mouseWorldPos = new Vector2(
                camPos.x + (mouseScreenPercent.x - 0.5f) * camWidth,
                camPos.y + (mouseScreenPercent.y - 0.5f) * camHeight
            );

            Vector2 throwPos = GameObject.FindGameObjectWithTag("Throw").transform.position;
            Vector2 direction = (mouseWorldPos - throwPos).normalized;

            //Debug.Log($"Alternative method direction: {direction}");

            heldItem.Throw(direction);
            heldItem = null;
        }
    }

    // Check if player is holding an item
    public bool IsHoldingItem()
    {
        return heldItem != null;
    }

    // Get the currently held item (can be null)
    public ThrowableItem GetHeldItem()
    {
        return heldItem;
    }

    public void DropHeldItem()
    {
        if (heldItem != null)
        {
            Destroy(heldItem.gameObject);
            heldItem = null;
        }
    }
}
