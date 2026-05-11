using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pause menu — handles pausing, resuming, saving, loading, and quitting.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static bool gameIsPaused = false;

    public GameObject pauseMenuUI;

    [Header("Save Slot UI")]
    public SaveSlotUI[] saveSlotUIs = new SaveSlotUI[3];
    public GameObject saveSlotPanel;
    public Button showSaveSlotsButton;
    public Button hideSaveSlotsButton;
    public Button quickSaveButton;
    public Text saveStatusText;

    private PlayerInput input;
    private GameObject gameManager;

    // -------------------------------------------------------------------------
    private void Awake()
    {
        // Register with the persistent object cleaner
        if (PersistantObjDestroyer.Instance != null)
            PersistantObjDestroyer.Instance.RegisterPersistentObject(gameObject);
    }

    private void Start()
    {
        gameManager = GameObject.Find("GameManager");

        pauseMenuUI = gameObject.GetComponent<PlayerStats>()?.pauseMenu;
        input = gameObject.GetComponent<PlayerInput>();

        SetupSaveUI();
    }

    private void SetupSaveUI()
    {
        for (int i = 0; i < saveSlotUIs.Length; i++)
        {
            if (saveSlotUIs[i] == null) continue;

            saveSlotUIs[i].slotIndex = i;

            int captured = i; // closure

            if (saveSlotUIs[i].loadButton != null)
                saveSlotUIs[i].loadButton.onClick.AddListener(() => LoadFromSlot(captured));

            if (saveSlotUIs[i].saveButton != null)
                saveSlotUIs[i].saveButton.onClick.AddListener(() => SaveToSlot(captured));

            if (saveSlotUIs[i].deleteButton != null)
                saveSlotUIs[i].deleteButton.onClick.AddListener(() => DeleteSlot(captured));
        }

        if (showSaveSlotsButton != null) showSaveSlotsButton.onClick.AddListener(ShowSaveSlots);
        if (hideSaveSlotsButton != null) hideSaveSlotsButton.onClick.AddListener(HideSaveSlots);
        if (quickSaveButton != null) quickSaveButton.onClick.AddListener(QuickSave);

        if (saveSlotPanel != null) saveSlotPanel.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Pause / Resume  (called by PlayerInput action)
    // -------------------------------------------------------------------------
    public void Pause(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        if (gameIsPaused) Resume();
        else OpenPause();
    }

    private void OpenPause()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        gameIsPaused = true;
        Time.timeScale = 0f;
        RefreshSlotDisplays();
    }

    public void Resume()
    {
        if (!gameIsPaused) return;

        Time.timeScale = 1f;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        gameIsPaused = false;
        HideSaveSlots();
    }

    // PlayerInput should be disabled while paused.
    // FixedUpdate runs on physics time so it still fires at timeScale 0.
    private void FixedUpdate()
    {
        if (input != null) input.enabled = !gameIsPaused;
    }

    // -------------------------------------------------------------------------
    // Save / Load / Delete
    // -------------------------------------------------------------------------
    public void QuickSave()
    {
        if (SaveGameManager.Instance == null) return;

        int slot = SaveGameManager.Instance.CurrentSaveSlot;
        SaveGameManager.Instance.SaveGame(slot);
        ShowStatus($"Quick saved to slot {slot + 1}!", Color.green);
        RefreshSlotDisplays();
    }

    private void SaveToSlot(int slotIndex)
    {
        if (SaveGameManager.Instance == null) return;

        SaveGameManager.Instance.SaveGame(slotIndex);
        ShowStatus($"Saved to slot {slotIndex + 1}!", Color.green);
        RefreshSlotDisplays();
    }

    private void LoadFromSlot(int slotIndex)
    {
        if (SaveGameManager.Instance == null) return;

        if (!SaveGameManager.Instance.HasValidSave(slotIndex))
        {
            ShowStatus($"No save in slot {slotIndex + 1}!", Color.red);
            return;
        }

        Time.timeScale = 1f;
        gameIsPaused = false;
        SaveGameManager.Instance.LoadSavedLevel(slotIndex);
    }

    private void DeleteSlot(int slotIndex)
    {
        if (SaveGameManager.Instance == null) return;

        SaveGameManager.Instance.DeleteSave(slotIndex);
        ShowStatus($"Deleted slot {slotIndex + 1}.", Color.yellow);
        RefreshSlotDisplays();
    }

    // -------------------------------------------------------------------------
    // UI helpers
    // -------------------------------------------------------------------------
    public void ShowSaveSlots()
    {
        if (saveSlotPanel != null)
        {
            saveSlotPanel.SetActive(true);
            RefreshSlotDisplays();
        }
    }

    public void HideSaveSlots()
    {
        if (saveSlotPanel != null) saveSlotPanel.SetActive(false);
    }

    private void RefreshSlotDisplays()
    {
        if (SaveGameManager.Instance == null) return;

        for (int i = 0; i < saveSlotUIs.Length; i++)
        {
            if (saveSlotUIs[i] != null)
                saveSlotUIs[i].UpdateDisplay(SaveGameManager.Instance.GetSaveData(i));
        }
    }

    private void ShowStatus(string message, Color color)
    {
        if (saveStatusText == null) return;

        saveStatusText.text = message;
        saveStatusText.color = color;
        StopCoroutine(nameof(ClearStatusAfterDelay));
        StartCoroutine(nameof(ClearStatusAfterDelay));

        Debug.Log($"[PauseMenu] {message}");
    }

    private IEnumerator ClearStatusAfterDelay()
    {
        yield return new WaitForSecondsRealtime(3f);
        if (saveStatusText != null) saveStatusText.text = "";
    }

    // -------------------------------------------------------------------------
    // Navigation
    // -------------------------------------------------------------------------
    public void MainMenu()
    {
        // Auto-save before leaving
        if (SaveGameManager.Instance != null)
            SaveGameManager.Instance.SaveGame(SaveGameManager.Instance.CurrentSaveSlot);

        Time.timeScale = 1f;
        gameIsPaused = false;
        SceneManager.LoadScene(0);
        if (gameManager != null) Destroy(gameManager);
    }

    public void Quit()
    {
        if (SaveGameManager.Instance != null)
            SaveGameManager.Instance.SaveGame(SaveGameManager.Instance.CurrentSaveSlot);

        Application.Quit();
    }

    // Hotkeys while paused
    private void Update()
    {
        if (gameIsPaused && Input.GetKeyDown(KeyCode.F5))
            QuickSave();
    }

    public void Regen()
    {
        gameObject.SetActive(true);
    }
}
