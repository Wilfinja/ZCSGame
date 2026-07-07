using UnityEngine;

/// <summary>
/// Sprite-based dart laser sight showing the bounce path as two segments.
/// Attach to a child of DartShootPoint.
/// Add two child GameObjects each with a SpriteRenderer using a 1x1 white pixel sprite,
/// then assign them in the Inspector.
/// </summary>
public class DartLaserSight : MonoBehaviour
{
    [Header("Segment Renderers")]
    [Tooltip("SpriteRenderer for the first leg (fire point to wall).")]
    public SpriteRenderer segment1;

    [Tooltip("SpriteRenderer for the second leg (wall to beyond bounce).")]
    public SpriteRenderer segment2;

    [Header("Settings")]
    [Tooltip("Width of the laser sight line in world units.")]
    public float lineWidth = 0.05f;

    [Tooltip("How far the second segment extends past the bounce point.")]
    public float postBounceLength = 20f;

    [Tooltip("Layers the laser sight treats as walls.")]
    public LayerMask wallLayer;

    [Tooltip("Z depth to place segments at. Match your other sprites (e.g. 1).")]
    public float segmentZ = 1f;

    [Tooltip("Offset from fire origin to avoid hitting boss own collider.")]
    public float originOffset = 0.3f;

    [Header("Color")]
    public Color sightColor = new Color(1f, 0f, 0f, 0.6f);

    private SpriteRenderer seg1SR;
    private SpriteRenderer seg2SR;

    private void Awake()
    {
        SetVisible(false);
    }

    private void Start()
    {
        // Cache references then detach so world-space positioning works
        if (segment1 != null)
        {
            seg1SR = segment1;
            segment1.transform.SetParent(null);
        }

        if (segment2 != null)
        {
            seg2SR = segment2;
            segment2.transform.SetParent(null);
        }
    }

    // ── Called by RobotCEOBoss each frame during dart telegraph ──────────────

    public void Show(Vector2 fireOrigin, Vector2 targetDirection)
    {
        SetVisible(true);
        UpdateSegments(fireOrigin, targetDirection);
    }

    public void Hide()
    {
        if (seg1SR != null) seg1SR.enabled = false;
        if (seg2SR != null) seg2SR.enabled = false;
    }

    private void OnDestroy()
    {
        if (seg1SR != null) Destroy(seg1SR.gameObject);
        if (seg2SR != null) Destroy(seg2SR.gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateSegments(Vector2 origin, Vector2 direction)
    {
        Vector2 rayStart = origin + direction * originOffset;
        RaycastHit2D wallHit = Physics2D.Raycast(rayStart, direction, postBounceLength, wallLayer);

        if (wallHit.collider != null)
        {
            Vector2 bouncePoint = wallHit.point;
            DrawSegment(seg1SR, origin, bouncePoint);

            Vector2 reflectedDir = Vector2.Reflect(direction, wallHit.normal);
            Vector2 seg2End = bouncePoint + reflectedDir * postBounceLength;
            DrawSegment(seg2SR, bouncePoint, seg2End);
        }
        else
        {
            Vector2 endpoint = origin + direction * postBounceLength;
            DrawSegment(seg1SR, origin, endpoint);
            if (seg2SR != null) seg2SR.enabled = false;
        }
    }

    private void DrawSegment(SpriteRenderer sr, Vector2 start, Vector2 end)
    {
        if (sr == null) return;

        float length = Vector2.Distance(start, end);
        if (length < 0.001f) return;

        sr.enabled = true;
        sr.color = sightColor;

        Vector2 midpoint = (start + end) * 0.5f;
        Vector2 direction = (end - start).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Z = 1 to match your scene's sprite depth convention
        sr.transform.position = new Vector3(midpoint.x, midpoint.y, segmentZ);
        sr.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        sr.transform.localScale = new Vector3(length, lineWidth, 1f);
    }

    private void SetVisible(bool visible)
    {
        if (seg1SR != null) seg1SR.enabled = visible;
        if (seg2SR != null) seg2SR.enabled = visible;

        // Fallback for before Start() runs
        if (segment1 != null) segment1.enabled = visible;
        if (segment2 != null) segment2.enabled = visible;
    }
}
