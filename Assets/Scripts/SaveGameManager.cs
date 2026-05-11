using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages saving and loading game state across scenes.
///
/// DESIGN PHILOSOPHY
/// -----------------
/// Save happens at level transition (or manually via F5 / pause menu).
/// Load restores stats only - position is handled by LevelStart / PlayerSpawnPoint.
/// We never blindly apply save data on every scene load; we only apply it when
/// the player explicitly chose to load a save (flagged by pendingLoad).
/// </summary>
public class SaveGameManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------
    public static SaveGameManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Inspector settings
    // -------------------------------------------------------------------------
    [Header("Save Settings")]
    public int numberOfSaveSlots = 3;
    public bool autoSave = true;
    public float autoSaveInterval = 120f; // seconds

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------
    private string saveFolderPath;
    private SaveData[] saveSlots;

    private float autoSaveTimer;
    private float sessionPlayTime; // time accumulated this session

    // Which slot the player is actively playing in.
    public int CurrentSaveSlot { get; private set; } = 0;

    // Set to true right before loading a scene from a save so that
    // OnSceneLoaded knows to apply save data once the scene is ready.
    private bool pendingLoad = false;
    private int pendingLoadSlot = 0;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();

            // Register with the persistent object tracker so GameOver can clean up
            if (PersistantObjDestroyer.Instance != null)
                PersistantObjDestroyer.Instance.RegisterPersistentObject(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        saveFolderPath = Path.Combine(Application.persistentDataPath, "SaveSlots");

        if (!Directory.Exists(saveFolderPath))
            Directory.CreateDirectory(saveFolderPath);

        saveSlots = new SaveData[numberOfSaveSlots];
        LoadAllSlotsFromDisk();

        autoSaveTimer = autoSaveInterval;
        sessionPlayTime = 0f;

        Debug.Log($"[SaveGameManager] Save folder: {saveFolderPath}");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        sessionPlayTime += Time.deltaTime;

        // Auto-save only when there is an active save in the current slot
        if (autoSave && !saveSlots[CurrentSaveSlot].IsEmpty())
        {
            autoSaveTimer -= Time.deltaTime;
            if (autoSaveTimer <= 0f)
            {
                SaveGame(CurrentSaveSlot);
                autoSaveTimer = autoSaveInterval;
            }
        }

        // Debug hotkeys
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveGame(CurrentSaveSlot);
            Debug.Log($"[SaveGameManager] Manual save to slot {CurrentSaveSlot + 1}");
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            LoadSavedLevel(CurrentSaveSlot);
            Debug.Log($"[SaveGameManager] Manual load from slot {CurrentSaveSlot + 1}");
        }
    }

    // -------------------------------------------------------------------------
    // Scene loading
    // -------------------------------------------------------------------------
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only apply save data when the player explicitly asked to load one.
        if (!pendingLoad) return;

        pendingLoad = false;

        // Delay one frame so all Awake/Start calls in the new scene finish first.
        StartCoroutine(ApplyAfterDelay(pendingLoadSlot));
    }

    private System.Collections.IEnumerator ApplyAfterDelay(int slotIndex)
    {
        yield return null; // wait one frame
        ApplySaveDataToGame(slotIndex);
    }

    // -------------------------------------------------------------------------
    // Public save / load API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Save current game state into the specified slot.
    /// Call this from LevelTransition, PauseMenu, or wherever needed.
    /// </summary>
    public void SaveGame(int slotIndex)
    {
        if (!SlotIndexValid(slotIndex)) return;

        if (saveSlots[slotIndex] == null)
            saveSlots[slotIndex] = new SaveData(slotIndex);

        CollectCurrentGameState(slotIndex);
        WriteToDisk(slotIndex);

        Debug.Log($"[SaveGameManager] Saved to slot {slotIndex + 1}");
    }

    /// <summary>
    /// Load a scene from a save slot. Stats are applied once the scene finishes loading.
    /// </summary>
    public void LoadSavedLevel(int slotIndex)
    {
        if (!SlotIndexValid(slotIndex)) return;
        if (saveSlots[slotIndex] == null || saveSlots[slotIndex].IsEmpty())
        {
            Debug.LogWarning($"[SaveGameManager] Slot {slotIndex + 1} is empty.");
            return;
        }

        CurrentSaveSlot = slotIndex;
        pendingLoad = true;
        pendingLoadSlot = slotIndex;

        SceneManager.LoadScene(saveSlots[slotIndex].currentSceneName);
    }

    /// <summary>
    /// Delete the data in a save slot.
    /// </summary>
    public void DeleteSave(int slotIndex)
    {
        if (!SlotIndexValid(slotIndex)) return;

        string path = GetFilePath(slotIndex);
        if (File.Exists(path))
        {
            try { File.Delete(path); }
            catch (System.Exception e) { Debug.LogError($"[SaveGameManager] Delete failed: {e.Message}"); }
        }

        saveSlots[slotIndex] = new SaveData(slotIndex);
        Debug.Log($"[SaveGameManager] Deleted slot {slotIndex + 1}");
    }

    // -------------------------------------------------------------------------
    // Collecting & applying state
    // -------------------------------------------------------------------------

    /// <summary>
    /// Pull the current game state into the SaveData object.
    /// Called just before writing to disk.
    /// </summary>
    private void CollectCurrentGameState(int slotIndex)
    {
        SaveData data = saveSlots[slotIndex];

        data.currentSceneName = SceneManager.GetActiveScene().name;

        // Parse level number from scene name (e.g. "Level3" -> 3, "Tutorial" -> 0)
        string sceneName = data.currentSceneName;
        if (sceneName.StartsWith("Level") && int.TryParse(sceneName.Substring(5), out int lvl))
            data.currentLevel = lvl;
        else
            data.currentLevel = 1; // fallback so IsEmpty() stays false

        // Accumulate play time
        data.totalPlayTime += sessionPlayTime;
        sessionPlayTime = 0f;

        // Stats from GameManager
        if (GameManager.Instance != null && GameManager.Instance.PlayerStats != null)
        {
            data.currentHealth = GameManager.Instance.PlayerStats.CurrentHealth;
            data.maxHealth = GameManager.Instance.PlayerStats.MaxHealth;
            data.currentChairLevel = GameManager.Instance.PlayerStats.CurrentChairLevel;
            data.maxChairLevel = GameManager.Instance.PlayerStats.MaxChairLevel;
        }

        data.StampSaveTime();
    }

    /// <summary>
    /// Push save data back into the live game objects.
    /// Safe to call after scene load - checks for null before touching anything.
    /// </summary>
    public void ApplySaveDataToGame(int slotIndex)
    {
        if (!SlotIndexValid(slotIndex)) return;

        SaveData data = saveSlots[slotIndex];
        if (data == null || data.IsEmpty()) return;

        CurrentSaveSlot = slotIndex;

        // ----- GameManager / UI -----
        if (GameManager.Instance != null && GameManager.Instance.PlayerStats != null)
        {
            GameManager.Instance.PlayerStats.CurrentHealth = data.currentHealth;
            GameManager.Instance.PlayerStats.MaxHealth = data.maxHealth;
            GameManager.Instance.PlayerStats.CurrentChairLevel = data.currentChairLevel;
            GameManager.Instance.PlayerStats.MaxChairLevel = data.maxChairLevel;

            if (GameManager.Instance.healthBar != null)
            {
                GameManager.Instance.healthBar.SetMaxHealth(data.maxHealth);
                GameManager.Instance.healthBar.SetHeatlth(data.currentHealth);
            }

            if (GameManager.Instance.chairBar != null)
            {
                GameManager.Instance.chairBar.SetMaxChair(data.maxChairLevel);
                GameManager.Instance.chairBar.SetChair(data.currentChairLevel);
            }
        }

        // ----- PlayerStats singleton -----
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.health = data.currentHealth;
            PlayerStats.Instance.maxHealth = data.maxHealth;
            PlayerStats.Instance.chairLevel = data.currentChairLevel;
            PlayerStats.Instance.maxChairLevel = data.maxChairLevel;
        }

        Debug.Log($"[SaveGameManager] Applied slot {slotIndex + 1} data to game.");
    }

    // -------------------------------------------------------------------------
    // Disk I/O
    // -------------------------------------------------------------------------
    private void LoadAllSlotsFromDisk()
    {
        for (int i = 0; i < numberOfSaveSlots; i++)
        {
            string path = GetFilePath(i);
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    saveSlots[i] = JsonUtility.FromJson<SaveData>(json);

                    // Sanity check - if deserialization produced null, reset
                    if (saveSlots[i] == null)
                        saveSlots[i] = new SaveData(i);
                }
                else
                {
                    saveSlots[i] = new SaveData(i);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveGameManager] Failed to load slot {i + 1}: {e.Message}");
                saveSlots[i] = new SaveData(i);
            }
        }
    }

    private void WriteToDisk(int slotIndex)
    {
        try
        {
            string json = JsonUtility.ToJson(saveSlots[slotIndex], true);
            File.WriteAllText(GetFilePath(slotIndex), json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveGameManager] Failed to write slot {slotIndex + 1}: {e.Message}");
        }
    }

    private string GetFilePath(int slotIndex)
    {
        return Path.Combine(saveFolderPath, $"saveslot_{slotIndex}.json");
    }

    // -------------------------------------------------------------------------
    // Public query helpers
    // -------------------------------------------------------------------------
    public SaveData GetSaveData(int slotIndex)
    {
        if (!SlotIndexValid(slotIndex)) return null;
        return saveSlots[slotIndex];
    }

    public bool HasValidSave(int slotIndex)
    {
        if (!SlotIndexValid(slotIndex)) return false;
        return saveSlots[slotIndex] != null && !saveSlots[slotIndex].IsEmpty();
    }

    public void SetCurrentSaveSlot(int slotIndex)
    {
        if (SlotIndexValid(slotIndex))
            CurrentSaveSlot = slotIndex;
    }

    private bool SlotIndexValid(int i) => i >= 0 && i < numberOfSaveSlots;
}
