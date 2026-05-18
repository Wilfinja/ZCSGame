using UnityEngine;

[System.Serializable]
public class SaveData
{
    // Slot identity
    public int saveSlotIndex;
    public string saveSlotName;
    public string lastSaved; // Stored as formatted string - JsonUtility can't serialize DateTime

    // Progress
    public string currentSceneName;
    public int currentLevel;

    // Player stats
    public int currentHealth;
    public int maxHealth;
    public int currentChairLevel;
    public int maxChairLevel;

    // Playtime
    public float totalPlayTime;

    public SaveData(int slotIndex = 0)
    {
        saveSlotIndex = slotIndex;
        saveSlotName = $"Save Slot {slotIndex + 1}";
        lastSaved = "";
        currentSceneName = "";
        currentLevel = 0;       // 0 = never saved
        currentHealth = 100;
        maxHealth = 100;
        currentChairLevel = 1;
        maxChairLevel = 5;
        totalPlayTime = 0f;
    }

    // A slot is empty when it has never been written to.
    // currentLevel == 0 is the sentinel we set only in the constructor.
    public bool IsEmpty()
    {
        return currentLevel <= 0 || string.IsNullOrEmpty(currentSceneName);
    }

    public void StampSaveTime()
    {
        lastSaved = System.DateTime.Now.ToString("MM/dd HH:mm");
    }
}