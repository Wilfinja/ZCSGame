using System;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    [Header("Save Slot Info")]
    public int saveSlotIndex;
    public string saveSlotName;
    public System.DateTime lastSaved;

    [Header("Level Information")]
    public int currentLevel;
    public string currentSceneName;

    [Header("Player Stats")]
    public int currentHealth;
    public int maxHealth;
    public int currentChairLevel;
    public int maxChairLevel;

    [Header("Player Position")]
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;

    [Header("Player State")]
    public float currentDrag;
    public bool isHoldingItem;
    public string heldItemType;

    [Header("Game Progress")]
    public bool[] levelCompletions;
    public float totalPlayTime;

    [Header("Settings")]
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;

    public SaveData(int slotIndex = 0)
    {
        saveSlotIndex = slotIndex;
        saveSlotName = $"Save Slot {slotIndex + 1}";
        lastSaved = System.DateTime.Now;
        currentLevel = 1;
        currentSceneName = "Level1";
        currentHealth = 100;
        maxHealth = 100;
        currentChairLevel = 1;
        maxChairLevel = 5;
        playerPosX = 0f;
        playerPosY = 0f;
        playerPosZ = 0f;
        currentDrag = 1f;
        isHoldingItem = false;
        heldItemType = "";
        levelCompletions = new bool[10];
        totalPlayTime = 0f;
        masterVolume = 1f;
        musicVolume = 1f;
        sfxVolume = 1f;
    }

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(currentSceneName) || currentLevel <= 0;
    }
}
