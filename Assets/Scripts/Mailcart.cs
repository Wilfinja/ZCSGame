using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class Mailcart : MonoBehaviour
{
    public Transform Arrive;
    public Transform Return;
    public Transform Body;

    private bool movingToArrive;
    public float moveSpeed = 5f;
    public float rotationSpeed = 8f;

    private float journeyLength;
    private float journeyStartTime;
    private Vector3 startPosition;
    private Vector3 targetPosition;

    public int damageAmount;

    private void Start()
    {
        movingToArrive = true;
        StartNewJourney(Arrive.position);
    }

    void Update()
    {
        // Calculate how far along the journey we are (0 to 1)
        float distanceCovered = (Time.time - journeyStartTime) * moveSpeed;
        float fractionOfJourney = distanceCovered / journeyLength;

        // Apply ease in/out curve (smooth start and stop)
        float smoothFraction = Mathf.SmoothStep(0f, 1f, fractionOfJourney);

        // Move the body using lerp with the smooth fraction
        Vector3 previousPosition = Body.position;
        Body.position = Vector3.Lerp(startPosition, targetPosition, smoothFraction);

        // Calculate movement direction for rotation
        Vector3 moveDirection = (Body.position - previousPosition).normalized;

        // Rotate to face movement direction
        if (moveDirection.magnitude > 0.01f)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // Check if we've reached the destination
        if (fractionOfJourney >= 1f)
        {
            if (movingToArrive)
            {
                // Reached arrive point, now go back to return
                movingToArrive = false;
                StartNewJourney(Return.position);
            }
            else
            {
                // Reached return point, now go to arrive
                movingToArrive = true;
                StartNewJourney(Arrive.position);
            }
        }
    }

    private void StartNewJourney(Vector3 destination)
    {
        startPosition = Body.position;
        targetPosition = destination;
        journeyLength = Vector3.Distance(startPosition, targetPosition);
        journeyStartTime = Time.time;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Steal object from player when touched else deal damage
        ClickToMove player = collision.gameObject.GetComponent<ClickToMove>();
        PlayerStats other = collision.gameObject.GetComponent<PlayerStats>();

        if (player != null)
        {
            if (player.IsHoldingItem())
            {
                player.DropHeldItem();
                Debug.Log("Vacuum stole the player's item!");
            }
            else
            {
                // No item to steal, deal damage instead
                other.GetComponent<PlayerStats>().TakeDamage(damageAmount / 2);
                other.GetComponent<DamageFlash>().Flash();
            }
        }
    }
}
