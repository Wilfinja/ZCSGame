using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SaveGameManager : MonoBehaviour
{
    [Header("Save Settings")]
    public int numberOfSaveSlots = 3;
    public bool autoSave = true;
    public float autoSaveInterval = 60f;
    public int currentSaveSlot = 0; // Currently active save slot

    private string saveFolderPath;
    private SaveData[] saveSlots;
    private float autoSaveTimer;
    private float playTimeTracker;

    // Singleton pattern
    public static SaveGameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        saveFolderPath = Path.Combine(Application.persistentDataPath, "SaveSlots");

        // Create save folder if it doesn't exist
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
        }

        saveSlots = new SaveData[numberOfSaveSlots];
        LoadAllSaveSlots();

        autoSaveTimer = autoSaveInterval;
        playTimeTracker = 0f;

        Debug.Log($"Save folder location: {saveFolderPath}");
    }

    private void Update()
    {
        playTimeTracker += Time.deltaTime;

        if (autoSave && HasValidSaveInCurrentSlot())
        {
            autoSaveTimer -= Time.deltaTime;
            if (autoSaveTimer <= 0f)
            {
                SaveGame(currentSaveSlot);
                autoSaveTimer = autoSaveInterval;
            }
        }

        // Debug keys
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveGame(currentSaveSlot);
            Debug.Log($"Manual save to slot {currentSaveSlot + 1}!");
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            LoadGame(currentSaveSlot);
            ApplySaveDataToGame(currentSaveSlot);
            Debug.Log($"Manual load from slot {currentSaveSlot + 1}!");
        }
    }

    public void LoadAllSaveSlots()
    {
        for (int i = 0; i < numberOfSaveSlots; i++)
        {
            string filePath = GetSaveFilePath(i);

            try
            {
                if (File.Exists(filePath))
                {
                    string jsonData = File.ReadAllText(filePath);
                    saveSlots[i] = JsonUtility.FromJson<SaveData>(jsonData);
                }
                else
                {
                    saveSlots[i] = new SaveData(i);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load save slot {i + 1}: {e.Message}");
                saveSlots[i] = new SaveData(i);
            }
        }

        Debug.Log($"Loaded {numberOfSaveSlots} save slots");
    }

    public void SaveGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= numberOfSaveSlots)
        {
            Debug.LogError($"Invalid save slot index: {slotIndex}");
            return;
        }

        try
        {
            // Initialize slot if it doesn't exist
            if (saveSlots[slotIndex] == null)
            {
                saveSlots[slotIndex] = new SaveData(slotIndex);
            }

            UpdateSaveDataFromCurrentGame(slotIndex);

            string jsonData = JsonUtility.ToJson(saveSlots[slotIndex], true);
            string filePath = GetSaveFilePath(slotIndex);

            File.WriteAllText(filePath, jsonData);

            Debug.Log($"Game saved to slot {slotIndex + 1}!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save to slot {slotIndex + 1}: {e.Message}");
        }
    }

    public void LoadGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= numberOfSaveSlots)
        {
            Debug.LogError($"Invalid save slot index: {slotIndex}");
            return;
        }

        string filePath = GetSaveFilePath(slotIndex);

        try
        {
            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                saveSlots[slotIndex] = JsonUtility.FromJson<SaveData>(jsonData);
                currentSaveSlot = slotIndex;
                Debug.Log($"Game loaded from slot {slotIndex + 1}!");
            }
            else
            {
                Debug.LogWarning($"No save file found in slot {slotIndex + 1}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load from slot {slotIndex + 1}: {e.Message}");
        }
    }

    private void UpdateSaveDataFromCurrentGame(int slotIndex)
    {
        SaveData saveData = saveSlots[slotIndex];

        // Update save info
        saveData.lastSaved = System.DateTime.Now;
        saveData.currentSceneName = SceneManager.GetActiveScene().name;
        saveData.totalPlayTime += playTimeTracker;
        playTimeTracker = 0f;

        // Update player position
        ClickToMove player = ClickToMove.Instance;
        if (player != null)
        {
            Vector3 playerPos = player.transform.position;
            saveData.playerPosX = playerPos.x;
            saveData.playerPosY = playerPos.y;
            saveData.playerPosZ = playerPos.z;

            saveData.isHoldingItem = player.IsHoldingItem();
            saveData.heldItemType = player.IsHoldingItem() && player.GetHeldItem() != null ?
                player.GetHeldItem().gameObject.name : "";
        }

        // Update stats from GameManager
        if (GameManager.Instance != null && GameManager.Instance.PlayerStats != null)
        {
            saveData.currentHealth = GameManager.Instance.PlayerStats.CurrentHealth;
            saveData.maxHealth = GameManager.Instance.PlayerStats.MaxHealth;
            saveData.currentChairLevel = GameManager.Instance.PlayerStats.CurrentChairLevel;
            saveData.maxChairLevel = GameManager.Instance.PlayerStats.MaxChairLevel;
        }

        // Update drag
        PlayerDrag playerDrag = FindFirstObjectByType<PlayerDrag>();
        if (playerDrag != null)
        {
            saveData.currentDrag = playerDrag.GetCurrentDrag();
        }
    }

    public void ApplySaveDataToGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= numberOfSaveSlots || saveSlots[slotIndex] == null)
        {
            Debug.LogError($"Cannot apply save data from invalid slot: {slotIndex}");
            return;
        }

        SaveData saveData = saveSlots[slotIndex];
        currentSaveSlot = slotIndex;

        // Apply to GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerStats.CurrentHealth = saveData.currentHealth;
            GameManager.Instance.PlayerStats.MaxHealth = saveData.maxHealth;
            GameManager.Instance.PlayerStats.CurrentChairLevel = saveData.currentChairLevel;
            GameManager.Instance.PlayerStats.MaxChairLevel = saveData.maxChairLevel;

            if (GameManager.Instance.healthBar != null)
            {
                GameManager.Instance.healthBar.SetMaxHealth(saveData.maxHealth);
                GameManager.Instance.healthBar.SetHeatlth(saveData.currentHealth);
            }

            if (GameManager.Instance.chairBar != null)
            {
                GameManager.Instance.chairBar.SetMaxChair(saveData.maxChairLevel);
                GameManager.Instance.chairBar.SetChair(saveData.currentChairLevel);
            }
        }

        // Apply player position
        ClickToMove player = ClickToMove.Instance;
        if (player != null)
        {
            Vector3 savedPosition = new Vector3(saveData.playerPosX, saveData.playerPosY, saveData.playerPosZ);
            player.transform.position = savedPosition;

            if (saveData.isHoldingItem && !string.IsNullOrEmpty(saveData.heldItemType))
            {
                RestoreHeldItem(saveData.heldItemType);
            }
        }

        // Sync PlayerStats
        PlayerStats playerStats = PlayerStats.Instance;
        if (playerStats != null)
        {
            playerStats.health = saveData.currentHealth;
            playerStats.maxHealth = saveData.maxHealth;
            playerStats.chairLevel = saveData.currentChairLevel;
            playerStats.maxChairLevel = saveData.maxChairLevel;
        }

        // Apply drag if not in hazard
        PlayerDrag playerDrag = FindFirstObjectByType<PlayerDrag>();
        if (playerDrag != null && !playerDrag.IsInHazard())
        {
            Rigidbody2D rb = playerDrag.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearDamping = saveData.currentDrag;
            }
        }
    }

    private void RestoreHeldItem(string itemType)
    {
        GameObject[] allItems = GameObject.FindGameObjectsWithTag("ThrowableItem");

        foreach (GameObject item in allItems)
        {
            if (item.name.Contains(itemType) || item.name == itemType)
            {
                ThrowableItem throwableItem = item.GetComponent<ThrowableItem>();
                if (throwableItem != null && ClickToMove.Instance != null)
                {
                    throwableItem.Pickup(ClickToMove.Instance.itemHold);
                    ClickToMove.Instance.heldItem = throwableItem;
                    Debug.Log($"Restored held item: {itemType}");
                    return;
                }
            }
        }

        Debug.LogWarning($"Could not restore held item: {itemType}");
    }

    private string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(saveFolderPath, $"saveslot_{slotIndex}.json");
    }

    // Public methods for slot management
    public void SetCurrentSaveSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < numberOfSaveSlots)
        {
            currentSaveSlot = slotIndex;
        }
    }

    public SaveData GetSaveData(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < numberOfSaveSlots)
        {
            return saveSlots[slotIndex];
        }
        return null;
    }

    public bool HasValidSave(int slotIndex)
    {
        SaveData saveData = GetSaveData(slotIndex);
        return saveData != null && !saveData.IsEmpty();
    }

    public bool HasValidSaveInCurrentSlot()
    {
        return HasValidSave(currentSaveSlot);
    }

    public void DeleteSave(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= numberOfSaveSlots)
            return;

        try
        {
            string filePath = GetSaveFilePath(slotIndex);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            saveSlots[slotIndex] = new SaveData(slotIndex);
            Debug.Log($"Deleted save slot {slotIndex + 1}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to delete save slot {slotIndex + 1}: {e.Message}");
        }
    }

    public void LoadLevel(int levelIndex, int saveSlot = -1)
    {
        int targetSlot = saveSlot >= 0 ? saveSlot : currentSaveSlot;

        if (saveSlots[targetSlot] == null)
            saveSlots[targetSlot] = new SaveData(targetSlot);

        saveSlots[targetSlot].currentLevel = levelIndex;
        SaveGame(targetSlot);
        SceneManager.LoadScene($"Level{levelIndex}");
    }

    public void LoadSavedLevel(int slotIndex)
    {
        if (HasValidSave(slotIndex))
        {
            SetCurrentSaveSlot(slotIndex);
            SceneManager.LoadScene(saveSlots[slotIndex].currentSceneName);
        }
    }

    public void MarkLevelComplete(int levelIndex, int saveSlot = -1)
    {
        int targetSlot = saveSlot >= 0 ? saveSlot : currentSaveSlot;

        if (saveSlots[targetSlot] != null &&
            levelIndex >= 0 &&
            levelIndex < saveSlots[targetSlot].levelCompletions.Length)
        {
            saveSlots[targetSlot].levelCompletions[levelIndex] = true;
            SaveGame(targetSlot);
        }
    }

    // Scene loading events
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (HasValidSaveInCurrentSlot())
        {
            Invoke(nameof(ApplyCurrentSaveToGame), 0.1f);
        }
    }

    private void ApplyCurrentSaveToGame()
    {
        ApplySaveDataToGame(currentSaveSlot);
    }
}
