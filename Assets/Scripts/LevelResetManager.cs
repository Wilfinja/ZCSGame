using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelResetManager : MonoBehaviour
{
    public static LevelResetManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RestartLevel()
    {
        // Reset the static GameOver flag on PlayerStats first
        PlayerStats.GameOver = false;

        // Destroy DontDestroyOnLoad objects that should NOT carry over
        // Add any others you know about here
        PlayerStats ps = FindFirstObjectByType<PlayerStats>();
        if (ps != null) Destroy(ps.gameObject);

        SaveGameManager sgm = FindFirstObjectByType<SaveGameManager>();
        if (sgm != null) Destroy(sgm.gameObject);

        // Reload the current scene fresh
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
