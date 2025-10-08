using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverMenu : MonoBehaviour
{
    public static bool gameIsPaused = false;

    public GameObject gameOverMenuUI;
    public GameObject gameManager;
    public GameObject player;
    public GameObject gameCamera;

    [Header("Save System UI")]
    public SaveSlotUI[] saveSlotUIs = new SaveSlotUI[3];
    public GameObject saveSlotPanel;
    public Button showLoadSlotsButton;
    public Button hideLoadSlotsButton;
    public Text gameOverInfoText;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        gameManager = GameObject.Find("GameManager");
        gameCamera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    private void Start()
    {
        SetupLoadUI();
    }

    private void SetupLoadUI()
    {
        // Setup save slot UIs (only load functionality)
        for (int i = 0; i < saveSlotUIs.Length; i++)
        {
            if (saveSlotUIs[i] != null)
            {
                saveSlotUIs[i].slotIndex = i;

                int slotIndex = i; // Capture for closure

                if (saveSlotUIs[i].loadButton != null)
                    saveSlotUIs[i].loadButton.onClick.AddListener(() => LoadFromSlot(slotIndex));

                // Disable save and delete buttons in game over screen
                if (saveSlotUIs[i].saveButton != null)
                    saveSlotUIs[i].saveButton.gameObject.SetActive(false);

                if (saveSlotUIs[i].deleteButton != null)
                    saveSlotUIs[i].deleteButton.gameObject.SetActive(false);
            }
        }

        // Setup show/hide buttons
        if (showLoadSlotsButton != null)
            showLoadSlotsButton.onClick.AddListener(ShowLoadSlots);

        if (hideLoadSlotsButton != null)
            hideLoadSlotsButton.onClick.AddListener(HideLoadSlots);

        // Hide load slots initially
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

    public void GameOver()
    {
        gameOverMenuUI.SetActive(true);
        gameIsPaused = true;
        Time.timeScale = .5f;

        player = GameObject.FindGameObjectWithTag("Player");
        player.GetComponent<ClickToMove>().enabled = false;
        player.GetComponent<LookAtCursor>().enabled = false;
        player.GetComponent<PlayerInput>().enabled = false;
        player.GetComponent<Rigidbody2D>().freezeRotation = true;

        UpdateGameOverUI();
    }

    private void UpdateGameOverUI()
    {
        UpdateSaveSlotDisplays();

        // Show info about available saves
        if (gameOverInfoText != null && SaveGameManager.Instance != null)
        {
            int availableSaves = 0;
            SaveData mostRecentSave = null;

            for (int i = 0; i < 3; i++)
            {
                SaveData save = SaveGameManager.Instance.GetSaveData(i);
                if (save != null && !save.IsEmpty())
                {
                    availableSaves++;
                    if (mostRecentSave == null || save.lastSaved > mostRecentSave.lastSaved)
                    {
                        mostRecentSave = save;
                    }
                }
            }

            if (availableSaves > 0)
            {
                gameOverInfoText.text = $"Available Saves: {availableSaves}\nMost Recent: Slot {mostRecentSave.saveSlotIndex + 1} - Level {mostRecentSave.currentLevel}";
            }
            else
            {
                gameOverInfoText.text = "No Save Data Available";
            }
        }
    }

    public void ShowLoadSlots()
    {
        if (saveSlotPanel != null)
        {
            saveSlotPanel.SetActive(true);
            UpdateSaveSlotDisplays();
        }
    }

    public void HideLoadSlots()
    {
        if (saveSlotPanel != null)
            saveSlotPanel.SetActive(false);
    }

    private void LoadFromSlot(int slotIndex)
    {
        if (SaveGameManager.Instance != null && SaveGameManager.Instance.HasValidSave(slotIndex))
        {
            Time.timeScale = 1f;
            CleanupCurrentScene();
            SaveGameManager.Instance.LoadSavedLevel(slotIndex);
        }
        else
        {
            Debug.LogWarning($"No valid save in slot {slotIndex + 1}!");
        }
    }

    private void CleanupCurrentScene()
    {
        if (PersistantObjDestroyer.Instance != null)
        {
            PersistantObjDestroyer.Instance.DestroyAllPersistentObjects();
        }

        if (gameManager != null) Destroy(gameManager);
        if (player != null) Destroy(player);
        if (gameCamera != null) Destroy(gameCamera);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        CleanupCurrentScene();
        SceneManager.LoadScene(0);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        CleanupCurrentScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}
