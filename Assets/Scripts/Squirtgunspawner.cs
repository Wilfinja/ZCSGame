using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns a SquirtGun prefab at this transform's position.
/// After the gun is picked up (or after a max lifetime), it respawns
/// after a configurable cooldown so the player is never permanently stuck.
/// </summary>
public class SquirtGunSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject squirtGunPrefab;

    [Header("Timing")]
    [Tooltip("Seconds before a new gun spawns after the previous one is gone")]
    public float respawnCooldown = 15f;
    [Tooltip("Max time a gun sits uncollected before being destroyed and respawned")]
    public float maxGunLifetime = 30f;

    [Header("Visual Feedback")]
    [Tooltip("Shown while waiting for respawn (e.g. an empty holster sprite)")]
    public SpriteRenderer emptyIndicator;
    public Color readyColor = Color.cyan;
    public Color emptyColor = Color.gray;

    private GameObject currentGun;
    private bool isWaitingToRespawn = false;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        SpawnGun();
    }

    private void Update()
    {
        // Detect when the spawned gun has been destroyed (picked up or lifetime expired)
        if (currentGun == null && !isWaitingToRespawn)
        {
            StartCoroutine(RespawnAfterCooldown());
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void SpawnGun()
    {
        if (squirtGunPrefab == null)
        {
            Debug.LogWarning($"{gameObject.name}: SquirtGunSpawner has no prefab assigned!");
            return;
        }

        currentGun = Instantiate(squirtGunPrefab, transform.position, Quaternion.identity);

        // Auto-destroy if uncollected for too long
        Destroy(currentGun, maxGunLifetime);

        SetIndicatorColor(readyColor);
    }

    private IEnumerator RespawnAfterCooldown()
    {
        isWaitingToRespawn = true;
        SetIndicatorColor(emptyColor);

        // Countdown pulse so the player can see it's about to respawn
        float elapsed = 0f;
        while (elapsed < respawnCooldown)
        {
            if (emptyIndicator != null)
            {
                float pulse = Mathf.PingPong(elapsed * 2f, 1f);
                emptyIndicator.color = Color.Lerp(emptyColor, readyColor, pulse);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        isWaitingToRespawn = false;
        SpawnGun();
    }

    private void SetIndicatorColor(Color c)
    {
        if (emptyIndicator != null)
            emptyIndicator.color = c;
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.6f);
    }
}
