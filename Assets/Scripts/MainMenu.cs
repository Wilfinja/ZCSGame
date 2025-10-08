using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Basic Buttons")]
    public Button newGameButton;
    public Button quitButton;

    [Header("Save Slot UI")]
    public SaveSlotUI[] saveSlotUIs = new SaveSlotUI[3];
    public GameObject saveSlotPanel;
    public Button showSaveSlotsButton;
    public Button hideSaveSlotsButton;

    [Header("Confirmation Dialog")]
    public GameObject newGameConfirmDialog;
    public Button confirmNewGameButton;
    public Button cancelNewGameButton;

    private void Start()
    {
        SetupUI();
        UpdateSaveSlotDisplays();
    }

    private void SetupUI()
    {
        // Setup save slot buttons
        for (int i = 0; i < saveSlotUIs.Length; i++)
        {
            if (saveSlotUIs[i] != null)
            {
                saveSlotUIs[i].slotIndex = i;

                // Setup button listeners
                int slotIndex = i; // Capture for closure

                if (saveSlotUIs[i].loadButton != null)
                    saveSlotUIs[i].loadButton.onClick.AddListener(() => LoadFromSlot(slotIndex));

                if (saveSlotUIs[i].saveButton != null)
                    saveSlotUIs[i].saveButton.onClick.AddListener(() => SaveToSlot(slotIndex));

                if (saveSlotUIs[i].deleteButton != null)
                    saveSlotUIs[i].deleteButton.onClick.AddListener(() => DeleteSlot(slotIndex));
            }
        }

        // Setup other buttons
        if (showSaveSlotsButton != null)
            showSaveSlotsButton.onClick.AddListener(ShowSaveSlots);

        if (hideSaveSlotsButton != null)
            hideSaveSlotsButton.onClick.AddListener(HideSaveSlots);

        // Setup confirmation dialog
        if (newGameConfirmDialog != null)
        {
            newGameConfirmDialog.SetActive(false);

            if (confirmNewGameButton != null)
                confirmNewGameButton.onClick.AddListener(ConfirmNewGame);

            if (cancelNewGameButton != null)
                cancelNewGameButton.onClick.AddListener(CancelNewGame);
        }

        // Hide save slots initially
        if (saveSlotPanel != null)
            saveSlotPanel.SetActive(false);
    }

    private void UpdateSaveSlotDisplays()
    {
        if (SaveGameManager.Instance != null)
        {
            for (int i = 0; i < saveSlotUIs.Length; i++)
            {
                if (saveSlotUIs[i] != null)
                {
                    SaveData slotData = SaveGameManager.Instance.GetSaveData(i);
                    saveSlotUIs[i].UpdateDisplay(slotData);
                }
            }
        }
    }

    public void ShowSaveSlots()
    {
        if (saveSlotPanel != null)
        {
            saveSlotPanel.SetActive(true);
            UpdateSaveSlotDisplays();
        }
    }

    public void HideSaveSlots()
    {
        if (saveSlotPanel != null)
            saveSlotPanel.SetActive(false);
    }

    private void LoadFromSlot(int slotIndex)
    {
        if (SaveGameManager.Instance != null && SaveGameManager.Instance.HasValidSave(slotIndex))
        {
            SaveGameManager.Instance.LoadSavedLevel(slotIndex);
        }
    }

    private void SaveToSlot(int slotIndex)
    {
        // You can't save from main menu, but this could be used for copying saves
        Debug.Log($"Cannot save to slot {slotIndex + 1} from main menu");
    }

    private void DeleteSlot(int slotIndex)
    {
        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.DeleteSave(slotIndex);
            UpdateSaveSlotDisplays();
        }
    }

    public void PlayGame()
    {
        // Check if any save exists
        bool hasSaves = false;
        if (SaveGameManager.Instance != null)
        {
            for (int i = 0; i < 3; i++)
            {
                if (SaveGameManager.Instance.HasValidSave(i))
                {
                    hasSaves = true;
                    break;
                }
            }
        }

        if (hasSaves && newGameConfirmDialog != null)
        {
            newGameConfirmDialog.SetActive(true);
            return;
        }

        StartNewGame();
    }

    private void StartNewGame()
    {
        SceneManager.LoadScene("Tutorial");
    }

    private void ConfirmNewGame()
    {
        if (newGameConfirmDialog != null)
            newGameConfirmDialog.SetActive(false);
        StartNewGame();
    }

    private void CancelNewGame()
    {
        if (newGameConfirmDialog != null)
            newGameConfirmDialog.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}