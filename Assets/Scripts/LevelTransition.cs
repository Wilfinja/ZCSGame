using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attached to a trigger zone at the end of a level.
/// When the player walks through it, we save progress then move to the next scene.
/// </summary>
public class LevelTransition : MonoBehaviour
{
    [Tooltip("Leave at -1 to auto-advance to the next build index scene.")]
    public int overrideNextSceneIndex = -1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Save to the active slot before transitioning
        if (SaveGameManager.Instance != null)
        {
            // Collect state right now (player is still alive, scene is still loaded)
            SaveGameManager.Instance.SaveGame(SaveGameManager.Instance.CurrentSaveSlot);
        }

        // Load the next scene
        int nextIndex = overrideNextSceneIndex >= 0
            ? overrideNextSceneIndex
            : SceneManager.GetActiveScene().buildIndex + 1;

        SceneManager.LoadScene(nextIndex);
    }
}
