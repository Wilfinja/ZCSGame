using System.Collections;
using UnityEngine;

/// <summary>
/// Reusable dramatic death explosion sequence.
/// For the boss: plays small explosions building to a big one, with camera pan.
/// For regular enemies: plays a quick small explosion and destroys.
/// Attach to any enemy and call Play() on death.
/// </summary>
public class ExplosionSequence : MonoBehaviour
{
    [Header("Small Explosions")]
    [Tooltip("Assign any of the WFX_ExplosiveSmoke prefabs here.")]
    public GameObject[] smallExplosionPrefabs;

    [Tooltip("How many small explosions fire before the big one.")]
    public int smallExplosionCount = 6;

    [Tooltip("Time between each small explosion.")]
    public float smallExplosionInterval = 0.2f;

    [Tooltip("Radius around the object to scatter small explosions.")]
    public float smallExplosionRadius = 0.8f;

    [Header("Big Explosion")]
    public GameObject bigExplosionPrefab;
    public float bigExplosionLifetime = 2f;

    [Tooltip("Scale multiplier for the big explosion. 1 = default size, 2 = twice as large.")]
    public float bigExplosionScale = 1f;

    [Header("Camera Pan (Boss Only)")]
    [Tooltip("Enable for boss death — pans the camera to this object before exploding.")]
    public bool doCameraPan = false;

    [Tooltip("How long the camera takes to pan to the boss.")]
    public float cameraPanDuration = 1.2f;

    [Tooltip("How long to hold on the boss after panning before exploding.")]
    public float holdBeforeExplosion = 0.4f;

    [Header("Timing")]
    [Tooltip("Seconds before the sequence starts (lets death animation play first).")]
    public float startDelay = 0f;

    // ─────────────────────────────────────────────────────────────────────────

    private Camera mainCamera;
    private Transform cameraTransform;

    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
            cameraTransform = mainCamera.transform;
    }

    /// <summary>
    /// Call this from Die() on any enemy to trigger the explosion sequence.
    /// The owning GameObject is destroyed at the end automatically.
    /// </summary>
    public void Play()
    {
        StartCoroutine(RunSequence());
    }

    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator RunSequence()
    {
        yield return new WaitForSeconds(startDelay);

        // ── Optional camera pan ───────────────────────────────────────────────
        if (doCameraPan && cameraTransform != null)
        {
            yield return StartCoroutine(PanCameraTo(transform.position, cameraPanDuration));
            yield return new WaitForSeconds(holdBeforeExplosion);
        }

        // ── Small explosions building up ──────────────────────────────────────
        for (int i = 0; i < smallExplosionCount; i++)
        {
            SpawnSmallExplosion();
            yield return new WaitForSeconds(smallExplosionInterval);
        }

        // ── Big final explosion ───────────────────────────────────────────────
        if (bigExplosionPrefab != null)
        {
            GameObject big = Instantiate(bigExplosionPrefab, transform.position, Quaternion.identity);
            big.transform.localScale = Vector3.one * bigExplosionScale;
            Destroy(big, bigExplosionLifetime);
        }

        // Small delay so the big explosion is visible before the object vanishes
        yield return new WaitForSeconds(0.1f);

        Destroy(gameObject);
    }

    private void SpawnSmallExplosion()
    {
        if (smallExplosionPrefabs == null || smallExplosionPrefabs.Length == 0) return;

        // Pick a random small explosion prefab from your array
        GameObject prefab = smallExplosionPrefabs[Random.Range(0, smallExplosionPrefabs.Length)];
        if (prefab == null) return;

        // Scatter randomly around the object
        Vector2 offset = Random.insideUnitCircle * smallExplosionRadius;
        Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0f);

        GameObject fx = Instantiate(prefab, spawnPos, Quaternion.identity);
        Destroy(fx, 2f);
    }

    private IEnumerator PanCameraTo(Vector3 targetWorldPos, float duration)
    {
        if (cameraTransform == null) yield break;

        // Detach camera from any follow script temporarily
        CameraFollow follow = mainCamera.GetComponent<CameraFollow>();
        if (follow != null) follow.enabled = false;

        Vector3 startPos = cameraTransform.position;

        // Keep the camera's Z so it doesn't fly into the scene
        Vector3 endPos = new Vector3(targetWorldPos.x, targetWorldPos.y, startPos.z);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            cameraTransform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.position = endPos;

        // Re-enable follow after sequence finishes — called from outside if needed
        // Left disabled intentionally so the credits screen can fade in cleanly
    }
}
