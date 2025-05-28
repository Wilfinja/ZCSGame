using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorHazard : MonoBehaviour
{
    [Header("Drag Settings")]
    public float dragModifier = 5f;  // Amount of drag to add
    public bool setAbsoluteDrag = false;  // If true, sets drag directly instead of adding
    public Color hazardColor = new Color(0.5f, 0.3f, 0, 0.5f);  // Brown semi-transparent for mud

    [Header("Effect Settings")]
    public bool useParticles = true;
    public float particleRate = 0.5f;

    private void Start()
    {
        // Set up the visual representation
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = hazardColor;
        }

        // Ensure we have a trigger collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ModifyDrag(other, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        ModifyDrag(other, false);
    }

    private void ModifyDrag(Collider2D other, bool entering)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            // Store original drag value if we haven't already
            if (entering)
            {
                // Store the original drag in a new component
                //DragMemory dragMemory = other.gameObject.AddComponent<DragMemory>();
                //dragMemory.originalDrag = rb.drag;

                // Apply new drag
                if (setAbsoluteDrag)
                {
                    rb.drag = dragModifier;
                }
                else
                {
                    rb.drag += dragModifier;
                }
            }
            else
            {
                // Restore original drag
                DragMemory dragMemory = other.gameObject.GetComponent<DragMemory>();
                if (dragMemory != null)
                {
                    rb.drag = dragMemory.originalDrag;
                    Destroy(dragMemory);
                }
            }
        }
    }
}
