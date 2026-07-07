using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the main menu. Handles new game (with slot picker), load game, and quit.
///
/// NEW GAME FLOW
/// -------------
/// 1. Player clicks "New Game"  →  slot picker panel opens.
/// 2. Player clicks a slot's "Select" button.
///    a. Slot is empty  →  game starts immediately.
///    b. Slot has data  →  slot picker HIDES, confirmation dialog appears.
///       • Confirm  →  game starts (old save kept until first in-game save).
///       • Cancel   →  dialog closes, slot picker reopens.
///
/// LOAD GAME FLOW
/// --------------
/// 1. Player clicks "Load Game"  →  save slot panel opens (load/delete only).
/// 2. Player clicks Load on a filled slot  →  scene loads via SaveGameManager.
///
/// PANELS
/// ------
/// slotPickerPanel      : shown during New Game slot selection.
/// saveSlotPanel        : shown during Load Game (load/delete only).
/// newGameConfirmDialog : shown when overwriting a filled slot.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Core Buttons")]
    public Button newGameButton;
    public Button loadGameButton;
    public Button quitButton;
    public GameObject titlepagepanel;

    [Header("Load Game - Save Slot UI")]
    public SaveSlotUI[] saveSlotUIs = new SaveSlotUI[3];
    public GameObject saveSlotPanel;
    public Button hideSaveSlotsButton;

    [Header("New Game - Slot Picker")]
    public SaveSlotUI[] newGameSlotUIs = new SaveSlotUI[3];
    public GameObject slotPickerPanel;
    public Button hideSlotPickerButton;

    [Header("New Game Confirmation Dialog")]
    public GameObject newGameConfirmDialog;
    public Button confirmNewGameButton;
    public Button cancelNewGameButton;
    public Text confirmDialogText;

    [Header("First Scene")]
    public string firstSceneName = "Tutorial";

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------
    private int pendingNewGameSlot = 0;
    private bool newGameSlotsWired = false;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------
    private void Start()
    {
        WireMainButtons();
        WireLoadSlots();
        WireConfirmDialog();

        SetPanelActive(saveSlotPanel, false);
        SetPanelActive(slotPickerPanel, false);
        SetPanelActive(newGameConfirmDialog, false);
    }

    // -------------------------------------------------------------------------
    // Button wiring
    // -------------------------------------------------------------------------
    private void WireMainButtons()
    {
        if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);
        if (loadGameButton != null) loadGameButton.onClick.AddListener(OnLoadGameClicked);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
    }

    private void WireLoadSlots()
    {
        if (hideSaveSlotsButton != null)
            hideSaveSlotsButton.onClick.AddListener(HideSaveSlots);

        for (int i = 0; i < saveSlotUIs.Length; i++)
        {
            if (saveSlotUIs[i] == null) continue;

            saveSlotUIs[i].slotIndex = i;

            // Hide save button - can't save from main menu
            if (saveSlotUIs[i].saveButton != null)
                saveSlotUIs[i].saveButton.gameObject.SetActive(false);

            // Hide select button - not needed in load panel
            if (saveSlotUIs[i].selectButton != null)
                saveSlotUIs[i].selectButton.gameObject.SetActive(false);

            // Load button - show and wire
            if (saveSlotUIs[i].loadButton != null)
            {
                saveSlotUIs[i].loadButton.gameObject.SetActive(true);
                saveSlotUIs[i].loadButton.onClick.AddListener(saveSlotUIs[i].OnLoadClicked);
            }

            // Delete button - show and wire
            if (saveSlotUIs[i].deleteButton != null)
            {
                saveSlotUIs[i].deleteButton.gameObject.SetActive(true);
                int captured = i;
                saveSlotUIs[i].deleteButton.onClick.AddListener(() =>
                {
                    saveSlotUIs[captured].OnDeleteClicked();
                    RefreshLoadSlotDisplays();
                });
            }
        }
    }

    /// <summary>
    /// Called from ShowSlotPicker() so buttons are guaranteed active when wired.
    /// Guard flag prevents listeners stacking on repeated opens.
    /// </summary>
    private void WireNewGameSlots()
    {
        if (newGameSlotsWired) return;
        newGameSlotsWired = true;

        if (hideSlotPickerButton != null)
            hideSlotPickerButton.onClick.AddListener(HideSlotPicker);

        for (int i = 0; i < newGameSlotUIs.Length; i++)
        {
            if (newGameSlotUIs[i] == null)
            {
                Debug.LogWarning($"[MainMenu] newGameSlotUIs[{i}] is NULL - not assigned in Inspector!");
                continue;
            }

            newGameSlotUIs[i].slotIndex = i;

            // Hide load and delete - not needed in slot picker
            if (newGameSlotUIs[i].loadButton != null)
                newGameSlotUIs[i].loadButton.gameObject.SetActive(false);
            if (newGameSlotUIs[i].deleteButton != null)
                newGameSlotUIs[i].deleteButton.gameObject.SetActive(false);

            // Wire select button
            if (newGameSlotUIs[i].selectButton != null)
            {
                int captured = i;
                newGameSlotUIs[i].selectButton.onClick.RemoveAllListeners();
                newGameSlotUIs[i].selectButton.onClick.AddListener(() =>
                {
                    Debug.Log($"[MainMenu] Select clicked - slot {captured}");
                    OnSlotSelectedForNewGame(captured);
                });
            }
            else
            {
                Debug.LogWarning($"[MainMenu] newGameSlotUIs[{i}] selectButton is NULL on {newGameSlotUIs[i].gameObject.name}!");
            }
        }
    }

    private void WireConfirmDialog()
    {
        if (confirmNewGameButton != null)
            confirmNewGameButton.onClick.AddListener(ConfirmNewGame);
        if (cancelNewGameButton != null)
            cancelNewGameButton.onClick.AddListener(CancelNewGame);
    }

    // -------------------------------------------------------------------------
    // Main-button handlers
    // -------------------------------------------------------------------------
    private void OnNewGameClicked()
    {
        HideSaveSlots();
        HideTitlePage();
        ShowSlotPicker();
    }

    private void OnLoadGameClicked()
    {
        HideSlotPicker();
        HideTitlePage();
        ShowSaveSlots();
    }

    // -------------------------------------------------------------------------
    // New-game slot picker
    // -------------------------------------------------------------------------
    private void OnSlotSelectedForNewGame(int slotIndex)
    {
        Debug.Log($"[MainMenu] OnSlotSelectedForNewGame - slot {slotIndex}");

        pendingNewGameSlot = slotIndex;

        bool slotHasData = SaveGameManager.Instance != null
                        && SaveGameManager.Instance.HasValidSave(slotIndex);

        if (slotHasData)
        {
            // Hide slot picker first so confirm dialog isn't layered on top
            SetPanelActive(slotPickerPanel, false);

            if (confirmDialogText != null)
            {
                confirmDialogText.text =
                    $"Slot {slotIndex + 1} already has a save.\n" +
                    $"Starting a new game will overwrite it when you save.\n" +
                    $"Continue?";
            }

            SetPanelActive(newGameConfirmDialog, true);
        }
        else
        {
            Debug.Log($"[MainMenu] Slot {slotIndex} is empty - loading {firstSceneName}");
            StartNewGame(slotIndex);
        }
    }

    private void ConfirmNewGame()
    {
        SetPanelActive(newGameConfirmDialog, false);
        StartNewGame(pendingNewGameSlot);
    }

    private void CancelNewGame()
    {
        // Hide confirm and reopen slot picker so player can choose again
        SetPanelActive(newGameConfirmDialog, false);
        SetPanelActive(slotPickerPanel, true);
        RefreshNewGameSlotDisplays();
    }

    private void StartNewGame(int slotIndex)
    {
        Debug.Log($"[MainMenu] StartNewGame - slot {slotIndex}, scene '{firstSceneName}'");

        if (SaveGameManager.Instance != null)
            SaveGameManager.Instance.SetCurrentSaveSlot(slotIndex);
        else
            Debug.LogWarning("[MainMenu] SaveGameManager.Instance is NULL!");

        SceneManager.LoadScene(firstSceneName);
    }

    // -------------------------------------------------------------------------
    // Panel helpers
    // -------------------------------------------------------------------------
    public void ShowSaveSlots()
    {
        SetPanelActive(saveSlotPanel, true);
        RefreshLoadSlotDisplays();
    }

    public void HideSaveSlots()
    {
        SetPanelActive(saveSlotPanel, false);
    }

    public void ShowSlotPicker()
    {
        SetPanelActive(slotPickerPanel, true);
        WireNewGameSlots();
        RefreshNewGameSlotDisplays();
    }

    public void HideSlotPicker()
    {
        SetPanelActive(slotPickerPanel, false);
        SetPanelActive(newGameConfirmDialog, false);
    }

    public void HideTitlePage()
    {
        SetPanelActive(titlepagepanel, false);
    }

    public void ShowTitlePage()
    {
        SetPanelActive(titlepagepanel, true);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    // -------------------------------------------------------------------------
    // Display refresh
    // -------------------------------------------------------------------------
    private void RefreshLoadSlotDisplays()
    {
        if (SaveGameManager.Instance == null) return;
        for (int i = 0; i < saveSlotUIs.Length; i++)
        {
            if (saveSlotUIs[i] != null)
                saveSlotUIs[i].UpdateDisplay(SaveGameManager.Instance.GetSaveData(i));
        }
    }

    private void RefreshNewGameSlotDisplays()
    {
        if (SaveGameManager.Instance == null) return;
        for (int i = 0; i < newGameSlotUIs.Length; i++)
        {
            if (newGameSlotUIs[i] != null)
                newGameSlotUIs[i].UpdateDisplay(SaveGameManager.Instance.GetSaveData(i));
        }
    }

    // -------------------------------------------------------------------------
    // Quit
    // -------------------------------------------------------------------------
    public void QuitGame()
    {
        Debug.Log("[MainMenu] Quit");
        Application.Quit();
    }
}