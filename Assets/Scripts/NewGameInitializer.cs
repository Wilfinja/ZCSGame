using System.Collections;
using UnityEngine;

public class NewGameInitializer : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(SaveAfterSceneLoads());
    }

    private IEnumerator SaveAfterSceneLoads()
    {
        // Wait one frame so the scene is fully active
        yield return null;

        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.SaveGame(SaveGameManager.Instance.CurrentSaveSlot);
            Debug.Log($"[NewGameInitializer] Initial save created in slot " +
                      $"{SaveGameManager.Instance.CurrentSaveSlot + 1} — " +
                      $"scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        }
        else
        {
            Debug.LogWarning("[NewGameInitializer] SaveGameManager not found!");
        }
    }
}
