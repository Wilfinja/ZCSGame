using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelStart : MonoBehaviour
{
    void OnEnable()
    {
        // Subscribe to the scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // Unsubscribe when this object is disabled/destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find the spawn point in the newly loaded scene
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("PlayerSpawnPoint");

        if (spawnPoint != null)
        {
            // Move the player to the spawn point position
            transform.position = spawnPoint.transform.position;

            // Optionally set rotation too
            transform.rotation = spawnPoint.transform.rotation;
        }
        else
        {
            Debug.LogWarning("No spawn point found in scene: " + scene.name);
        }
    }

}
