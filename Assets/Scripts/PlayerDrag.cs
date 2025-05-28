using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDrag : MonoBehaviour
{
    [Header("Drag Settings")]
    [SerializeField] private float normalDrag = 1f;
    [SerializeField] private float defaultTransitionSpeed = 2f;

    [Header("Current State")]
    [SerializeField] private float currentTargetDrag;
    [SerializeField] private bool isInHazard = false;
    [SerializeField] private int hazardCount = 0;

    private Rigidbody2D rb;
    private Coroutine dragTransitionCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentTargetDrag = normalDrag;
        rb.drag = normalDrag;
    }

    public void SetHazardDrag(float newDrag, float transitionSpeed = -1f)
    {
        hazardCount++;

        if (transitionSpeed < 0)
            transitionSpeed = defaultTransitionSpeed;

        // If we're entering a new hazard with different drag, update to the new value
        if (!isInHazard || newDrag != currentTargetDrag)
        {
            isInHazard = true;
            currentTargetDrag = newDrag;
            StartDragTransition(newDrag, transitionSpeed);
        }
    }

    public void RestoreNormalDrag(float transitionSpeed = -1f)
    {
        hazardCount = Mathf.Max(0, hazardCount - 1);

        // Only restore normal drag if we're not in any other hazards
        if (hazardCount <= 0)
        {
            isInHazard = false;
            currentTargetDrag = normalDrag;

            if (transitionSpeed < 0)
                transitionSpeed = defaultTransitionSpeed;

            StartDragTransition(normalDrag, transitionSpeed);
        }
    }

    private void StartDragTransition(float targetDrag, float speed)
    {
        // Stop any existing transition
        if (dragTransitionCoroutine != null)
        {
            StopCoroutine(dragTransitionCoroutine);
        }

        // Start new transition
        dragTransitionCoroutine = StartCoroutine(TransitionDrag(targetDrag, speed));
    }

    private System.Collections.IEnumerator TransitionDrag(float targetDrag, float speed)
    {
        float startDrag = rb.drag;
        float elapsedTime = 0f;

        while (elapsedTime < 1f / speed)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime * speed;
            rb.drag = Mathf.Lerp(startDrag, targetDrag, t);
            yield return null;
        }

        rb.drag = targetDrag;
        dragTransitionCoroutine = null;
    }

    // Public methods for external access
    public float GetCurrentDrag() => rb.drag;
    public float GetNormalDrag() => normalDrag;
    public bool IsInHazard() => isInHazard;
    public int GetHazardCount() => hazardCount;

    // Method to force reset (useful for debugging or special cases)
    public void ForceResetDrag()
    {
        hazardCount = 0;
        isInHazard = false;
        currentTargetDrag = normalDrag;

        if (dragTransitionCoroutine != null)
        {
            StopCoroutine(dragTransitionCoroutine);
            dragTransitionCoroutine = null;
        }

        rb.drag = normalDrag;
    }
}
