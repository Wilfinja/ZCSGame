using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Shown when the player dies. Lets them load a save, restart, or quit to menu.
/// Does NOT contain save functionality - dying shouldn't overwrite your progress.
/// </summary>
public class GameOverMenu : MonoBehaviour
{
    public GameObject gameOverMenuUI;

    [Header("Save Slot Load UI")]
    public SaveSlotUI[] saveSlotUIs = new SaveSlotUI[3];
    public GameObject saveSlotPanel;
    public Button showLoadSlotsButton;
    public Button hideLoadSlotsButton;
    public Text gameOverInfoText;

    // Cached references filled in GameOver()
    private GameObject player;
    private GameObject gameCamera;
    private GameObject gameManager;

    // -------------------------------------------------------------------------
    private void Start()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        for (int i = 0; i < saveSlotUIs.Length; i++)
        {
            if (saveSlotUIs[i] == null) continue;

            saveSlotUIs[i].slotIndex = i;

            // Load-only in game over screen
            if (saveSlotUIs[i].saveButton != null)
                saveSlotUIs[i].saveButton.gameObject.SetActive(false);

            if (saveSlotUIs[i].deleteButton != null)
                saveSlotUIs[i].deleteButton.gameObject.SetActive(false);

            if (saveSlotUIs[i].loadButton != null)
                saveSlotUIs[i].loadButton.onClick.AddListener(saveSlotUIs[i].OnLoadClicked);
        }

        if (showLoadSlotsButton != null) showLoadSlotsButton.onClick.AddListener(ShowLoadSlots);
        if (hideLoadSlotsButton != null) hideLoadSlotsButton.onClick.AddListener(HideLoadSlots);

        if (saveSlotPanel != null) saveSlotPanel.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Called by PlayerStats when health hits zero
    // -------------------------------------------------------------------------
    public void GameOver()
    {
        // Cache scene objects before we might destroy them
        player = GameObject.FindGameObjectWithTag("Player");
        gameManager = GameObject.Find("GameManager");
        gameCamera = GameObject.FindGameObjectWithTag("MainCamera");

        if (gameOverMenuUI != null) gameOverMenuUI.SetActive(true);
        Time.timeScale = 0.5f;

        // Disable player input
        if (player != null)
        {
            var ctm = player.GetComponent<ClickToMove>();
            if (ctm != null) ctm.enabled = false;

            var lac = player.GetComponent<LookAtCursor>();
            if (lac != null) lac.enabled = false;

            var pi = player.GetComponent<PlayerInput>();
            if (pi != null) pi.enabled = false;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.freezeRotation = true;
        }

        RefreshSlotDisplays();
        UpdateInfoText();
    }

    // -------------------------------------------------------------------------
    // UI helpers
    // -------------------------------------------------------------------------
    private void RefreshSlotDisplays()
    {
        if (SaveGameManager.Instance == null) return;

        for (int i = 0; i < saveSlotUIs.Length; i++)
        {
            if (saveSlotUIs[i] != null)
                saveSlotUIs[i].UpdateDisplay(SaveGameManager.Instance.GetSaveData(i));
        }
    }

    private void UpdateInfoText()
    {
        if (gameOverInfoText == null || SaveGameManager.Instance == null) return;

        int savedCount = 0;
        SaveData newest = null;

        for (int i = 0; i < 3; i++)
        {
            SaveData d = SaveGameManager.Instance.GetSaveData(i);
            if (d != null && !d.IsEmpty())
            {
                savedCount++;
                if (newest == null || d.totalPlayTime > newest.totalPlayTime)
                    newest = d;
            }
        }

        gameOverInfoText.text = savedCount > 0
            ? $"Saves available: {savedCount}\nMost recent: Slot {newest.saveSlotIndex + 1} — {newest.lastSaved}"
            : "No save data found.";
    }

    public void ShowLoadSlots()
    {
        if (saveSlotPanel != null)
        {
            saveSlotPanel.SetActive(true);
            RefreshSlotDisplays();
        }
    }

    public void HideLoadSlots()
    {
        if (saveSlotPanel != null) saveSlotPanel.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Button actions
    // -------------------------------------------------------------------------
    public void Restart()
    {
        Time.timeScale = 1f;
        CleanupPersistentObjects();
        PlayerStats.GameOver = false;
        LevelResetManager.Instance.RestartLevel();
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        CleanupPersistentObjects();
        SceneManager.LoadScene(0);
    }

    public void Quit()
    {
        Application.Quit();
    }

    private void CleanupPersistentObjects()
    {
        if (PersistantObjDestroyer.Instance != null)
            PersistantObjDestroyer.Instance.DestroyAllPersistentObjects();

        // Belt-and-suspenders: destroy known scene objects that might persist
        if (gameManager != null) Destroy(gameManager);
        if (player != null) Destroy(player);
        if (gameCamera != null) Destroy(gameCamera);
    }
}
