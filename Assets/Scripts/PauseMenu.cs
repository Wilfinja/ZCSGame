using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public static bool gameIsPaused = false;

    public GameObject pauseMenuUI;
    public GameObject player;
    public GameObject gameManager;

    [Header("Save System UI")]
    public SaveSlotUI[] saveSlotUIs = new SaveSlotUI[3];
    public GameObject saveSlotPanel;
    public Button showSaveSlotsButton;
    public Button hideSaveSlotsButton;
    public Button quickSaveButton;
    public Text saveStatusText;

    PlayerInput input;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (PersistantObjDestroyer.Instance != null)
        {
            PersistantObjDestroyer.Instance.RegisterPersistentObject(gameObject);
        }
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        pauseMenuUI = gameObject.GetComponent<PlayerStats>().pauseMenu;
        input = gameObject.GetComponent<PlayerInput>();

        SetupSaveUI();
    }

    private void SetupSaveUI()
    {
        // Setup save slot UIs
        for (int i = 0; i < saveSlotUIs.Length; i++)
        {
            if (saveSlotUIs[i] != null)
            {
                saveSlotUIs[i].slotIndex = i;

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

        if (quickSaveButton != null)
            quickSaveButton.onClick.AddListener(QuickSave);

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

    public void Pause(InputAction.CallbackContext context)
    {
        if (gameIsPaused && context.started == true)
        {
            Resume();
        }
        else if (!gameIsPaused && context.started == true)
        {
            pauseMenuUI.SetActive(true);
            gameIsPaused = true;
            Time.timeScale = 0f;

            UpdateSaveSlotDisplays();
        }
    }

    private void FixedUpdate()
    {
        input.enabled = !gameIsPaused;
    }

    public void Resume()
    {
        if (gameIsPaused)
        {
            Time.timeScale = 1f;
            pauseMenuUI.SetActive(false);
            gameIsPaused = false;
            HideSaveSlots();
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

    public void QuickSave()
    {
        if (SaveGameManager.Instance != null)
        {
            // Save to current slot or first available slot
            int slotToUse = SaveGameManager.Instance.currentSaveSlot;
            SaveGameManager.Instance.SaveGame(slotToUse);
            ShowSaveStatus($"Quick saved to slot {slotToUse + 1}!", Color.green);
            UpdateSaveSlotDisplays();
        }
    }

    private void LoadFromSlot(int slotIndex)
    {
        if (SaveGameManager.Instance != null && SaveGameManager.Instance.HasValidSave(slotIndex))
        {
            Time.timeScale = 1f;
            SaveGameManager.Instance.LoadSavedLevel(slotIndex);
            ShowSaveStatus($"Loaded from slot {slotIndex + 1}!", Color.blue);
        }
        else
        {
            ShowSaveStatus($"No save data in slot {slotIndex + 1}!", Color.red);
        }
    }

    private void SaveToSlot(int slotIndex)
    {
        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.SaveGame(slotIndex);
            ShowSaveStatus($"Saved to slot {slotIndex + 1}!", Color.green);
            UpdateSaveSlotDisplays();
        }
    }

    private void DeleteSlot(int slotIndex)
    {
        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.DeleteSave(slotIndex);
            ShowSaveStatus($"Deleted slot {slotIndex + 1}!", Color.yellow);
            UpdateSaveSlotDisplays();
        }
    }

    private void ShowSaveStatus(string message, Color color)
    {
        if (saveStatusText != null)
        {
            saveStatusText.text = message;
            saveStatusText.color = color;
            StartCoroutine(ClearSaveStatusAfterDelay(3f));
        }
        Debug.Log(message);
    }

    private IEnumerator ClearSaveStatusAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (saveStatusText != null)
            saveStatusText.text = "";
    }

    public void MainMenu()
    {
        // Auto-save to current slot before going to main menu
        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.SaveGame(SaveGameManager.Instance.currentSaveSlot);
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
        Object.Destroy(gameManager);
    }

    public void Quit()
    {
        // Auto-save to current slot before quitting
        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.SaveGame(SaveGameManager.Instance.currentSaveSlot);
        }

        Application.Quit();
    }

    void Update()
    {
        // Optional: Hotkeys for quick save/load
        if (gameIsPaused)
        {
            if (Input.GetKeyDown(KeyCode.F5))
                QuickSave();
        }
    }

    public void Regen()
    {
        gameObject.SetActive(true);
    }

    IEnumerator PauseDelay()
    {
        yield return new WaitForSeconds(.2f);
        Time.timeScale = 1f;
    }
}
