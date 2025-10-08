using UnityEngine;
using UnityEngine.UI;
using System;

public class SaveSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Text slotNumberText;
    public Text levelText;
    public Text healthText;
    public Text chairLevelText;
    public Text playTimeText;
    public Text lastSavedText;
    public Text emptySlotText;
    public Button loadButton;
    public Button saveButton;
    public Button deleteButton;
    public GameObject slotContent;
    public GameObject emptySlotContent;

    [Header("Slot Info")]
    public int slotIndex;

    private SaveData saveData;

    public void UpdateDisplay(SaveData data)
    {
        saveData = data;

        if (slotNumberText != null)
            slotNumberText.text = $"Slot {slotIndex + 1}";

        bool isEmpty = data == null || data.IsEmpty();

        // Show appropriate content
        if (slotContent != null) slotContent.SetActive(!isEmpty);
        if (emptySlotContent != null) emptySlotContent.SetActive(isEmpty);

        if (isEmpty)
        {
            if (emptySlotText != null)
                emptySlotText.text = "Empty Slot";

            if (loadButton != null) loadButton.interactable = false;
            if (deleteButton != null) deleteButton.interactable = false;
        }
        else
        {
            // Update slot info
            if (levelText != null)
                levelText.text = $"Level {data.currentLevel}";

            if (healthText != null)
                healthText.text = $"Health: {data.currentHealth}/{data.maxHealth}";

            if (chairLevelText != null)
                chairLevelText.text = $"Chair Lv.{data.currentChairLevel}";

            if (playTimeText != null)
            {
                float hours = data.totalPlayTime / 3600f;
                float minutes = (data.totalPlayTime % 3600f) / 60f;
                playTimeText.text = $"{hours:F0}h {minutes:F0}m";
            }

            if (lastSavedText != null)
                lastSavedText.text = $"Saved: {data.lastSaved:MM/dd HH:mm}";

            if (loadButton != null) loadButton.interactable = true;
            if (deleteButton != null) deleteButton.interactable = true;
        }

        // Save button is always available
        if (saveButton != null) saveButton.interactable = true;
    }

    public void OnLoadClicked()
    {
        if (SaveGameManager.Instance != null && saveData != null && !saveData.IsEmpty())
        {
            SaveGameManager.Instance.LoadSavedLevel(slotIndex);
        }
    }

    public void OnSaveClicked()
    {
        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.SaveGame(slotIndex);
            // Refresh display
            UpdateDisplay(SaveGameManager.Instance.GetSaveData(slotIndex));
        }
    }

    public void OnDeleteClicked()
    {
        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.DeleteSave(slotIndex);
            // Refresh display
            UpdateDisplay(SaveGameManager.Instance.GetSaveData(slotIndex));
        }
    }
}