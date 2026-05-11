using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the main menu. Handles new game, load game, and quit.
/// Save slots are display-only here - you can load or delete but not save
/// (there is nothing to save from the main menu).
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Core Buttons")]
    public Button newGameButton;
    public Button quitButton;

    [Header("Save Slot UI")]
    public SaveSlotUI[] saveSlotUIs = new SaveSlotUI[3];
    public GameObject saveSlotPanel;
    public Button showSaveSlotsButton;
    public Button hideSaveSlotsButton;

    [Header("New Game Confirmation")]
    public GameObject newGameConfirmDialog;
    public Button confirmNewGameButton;
    public Button cancelNewGameButton;

    [Header("First Scene")]
    [Tooltip("Scene name to load for a brand new game.")]
    public string firstSceneName = "Tutorial";

    // -------------------------------------------------------------------------
    private void Start()
    {
        SetupButtons();
        RefreshSlotDisplays();

        if (saveSlotPanel != null) saveSlotPanel.SetActive(false);
        if (newGameConfirmDialog != null) newGameConfirmDialog.SetActive(false);
    }

    private void SetupButtons()
    {
        if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (showSaveSlotsButton != null) showSaveSlotsButton.onClick.AddListener(ShowSaveSlots);
        if (hideSaveSlotsButton != null) hideSaveSlotsButton.onClick.AddListener(HideSaveSlots);

        if (confirmNewGameButton != null) confirmNewGameButton.onClick.AddListener(ConfirmNewGame);
        if (cancelNewGameButton != null) cancelNewGameButton.onClick.AddListener(CancelNewGame);

        // Wire up each slot UI
        for (int i = 0; i < saveSlotUIs.Length; i++)
        {
            if (saveSlotUIs[i] == null) continue;

            saveSlotUIs[i].slotIndex = i;

            // Disable save button - can't save from main menu
            if (saveSlotUIs[i].saveButton != null)
                saveSlotUIs[i].saveButton.gameObject.SetActive(false);

            // Wire load and delete directly through SaveSlotUI's own methods
            if (saveSlotUIs[i].loadButton != null)
                saveSlotUIs[i].loadButton.onClick.AddListener(saveSlotUIs[i].OnLoadClicked);

            if (saveSlotUIs[i].deleteButton != null)
                saveSlotUIs[i].deleteButton.onClick.AddListener(() =>
                {
                    saveSlotUIs[i].OnDeleteClicked();
                    RefreshSlotDisplays();
                });
        }
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

    // -------------------------------------------------------------------------
    // Button handlers
    // -------------------------------------------------------------------------
    private void OnNewGameClicked()
    {
        // If any save exists, ask for confirmation before overwriting progress
        bool anySave = false;
        if (SaveGameManager.Instance != null)
        {
            for (int i = 0; i < 3; i++)
            {
                if (SaveGameManager.Instance.HasValidSave(i)) { anySave = true; break; }
            }
        }

        if (anySave && newGameConfirmDialog != null)
        {
            newGameConfirmDialog.SetActive(true);
        }
        else
        {
            StartNewGame();
        }
    }

    private void ConfirmNewGame()
    {
        if (newGameConfirmDialog != null) newGameConfirmDialog.SetActive(false);
        StartNewGame();
    }

    private void CancelNewGame()
    {
        if (newGameConfirmDialog != null) newGameConfirmDialog.SetActive(false);
    }

    private void StartNewGame()
    {
        // New game goes to slot 0 by default - you could let the player pick
        if (SaveGameManager.Instance != null)
            SaveGameManager.Instance.SetCurrentSaveSlot(0);

        SceneManager.LoadScene(firstSceneName);
    }

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

    public void QuitGame()
    {
        Debug.Log("[MainMenu] Quit");
        Application.Quit();
    }
}