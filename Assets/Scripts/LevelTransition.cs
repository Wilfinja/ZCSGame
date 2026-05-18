using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransition : MonoBehaviour
{
    // Removed the old 'player' GameObject reference — GameManager is a singleton,
    // no need to look it up via GameObject.Find every time.

    [Tooltip("Leave at -1 to auto-advance to the next build index scene.")]
    public int overrideNextSceneIndex = -1;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Tell the GameManager to save health before we leave
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadHealthStats();
        }

        // Save to the active slot before transitioning
        if (SaveGameManager.Instance != null)
        {
            // Collect state right now (player is still alive, scene is still loaded)
            SaveGameManager.Instance.SaveGame(SaveGameManager.Instance.CurrentSaveSlot);
        }

        // Use the iris transition instead of loading directly
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(
                SceneManager.GetActiveScene().buildIndex + 1
            );
        }
        else
        {
            // Fallback if SceneTransitionManager isn't in the scene yet
            Debug.LogWarning("SceneTransitionManager not found — loading directly.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }
}
