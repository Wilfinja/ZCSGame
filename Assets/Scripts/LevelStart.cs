using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attached to the persistent player object.
/// On every scene load, moves the player to the tagged spawn point.
/// This is the ONLY place that sets player position - SaveGameManager never
/// touches position, so there is no race condition.
/// </summary>
public class LevelStart : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("PlayerSpawnPoint");

        if (spawnPoint != null)
        {
            transform.position = spawnPoint.transform.position;
            transform.rotation = spawnPoint.transform.rotation;
        }
        else
        {
            Debug.LogWarning($"[LevelStart] No PlayerSpawnPoint found in scene: {scene.name}");
        }
    }
}
