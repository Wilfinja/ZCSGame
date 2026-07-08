using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransition : MonoBehaviour
{
    // Removed the old 'player' GameObject reference — GameManager is a singleton,
    // no need to look it up via GameObject.Find every time.

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Tell the GameManager to save health before we leave
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadHealthStats();
        }

        if (ClickToMove.Instance != null && ClickToMove.Instance.IsHoldingItem())
        {
            ClickToMove.Instance.DropHeldItem();
        }

        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.SaveOnNextSceneLoad(SaveGameManager.Instance.CurrentSaveSlot);
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