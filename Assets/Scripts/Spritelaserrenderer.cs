using UnityEngine;

public class SpriteLaserRenderer : MonoBehaviour
{
    [Header("Sprite References")]
    public SpriteRenderer beamMiddleRenderer;
    public SpriteRenderer beamEndRenderer;

    [Header("Beam Settings")]
    public float maxBeamLength = 20f;
    public LayerMask blockingLayers;
    public float beamWidth = 0.5f;

    // ── Private ──────────────────────────────────────────────────────────────
    private float beamMiddleNaturalWidth;
    private float beamMiddleNaturalHeight;

    private void Awake()
    {
        if (beamMiddleRenderer != null && beamMiddleRenderer.sprite != null)
        {
            // Read world-unit size directly from the sprite — no manual PPU math needed
            beamMiddleNaturalWidth = beamMiddleRenderer.sprite.bounds.size.x;
            beamMiddleNaturalHeight = beamMiddleRenderer.sprite.bounds.size.y;
        }
        else
        {
            // Safe fallback so we never divide by zero
            beamMiddleNaturalWidth = 1f;
            beamMiddleNaturalHeight = 1f;
            Debug.LogWarning("SpriteLaserRenderer: beamMiddleRenderer or its sprite is not assigned.");
        }

        SetVisible(false);
    }

    public void UpdateBeam()
    {
        SetVisible(true);

        Vector2 origin = transform.position;
        Vector2 direction = -transform.up;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, maxBeamLength, blockingLayers);
        float beamLength = hit.collider != null ? hit.distance : maxBeamLength;

        if (beamMiddleRenderer != null)
        {
            float scaleY = beamLength / beamMiddleNaturalHeight;
            float scaleX = beamWidth / beamMiddleNaturalWidth;

            beamMiddleRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            beamMiddleRenderer.transform.localPosition = new Vector3(0f, -beamLength * 0.5f, 0f);
            beamMiddleRenderer.transform.localRotation = Quaternion.identity;
        }

        if (beamEndRenderer != null)
        {
            float scaleUniform = beamWidth / beamMiddleNaturalWidth;

            beamEndRenderer.transform.localScale = new Vector3(scaleUniform, scaleUniform, 1f);
            beamEndRenderer.transform.localPosition = new Vector3(0f, -beamLength, 0f);
            beamEndRenderer.transform.localRotation = Quaternion.identity;
        }
    }

    public void HideBeam()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (beamMiddleRenderer != null) beamMiddleRenderer.enabled = visible;
        if (beamEndRenderer != null) beamEndRenderer.enabled = visible;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position,
                        transform.position + (-transform.up) * maxBeamLength);
    }
}
