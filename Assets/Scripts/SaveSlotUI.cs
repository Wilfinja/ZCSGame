using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the UI for a single save slot panel.
/// Works with SaveGameManager to display, save, load, and delete saves.
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Text slotNumberText;
    public Text levelText;
    public Text healthText;
    public Text chairLevelText;
    public Text playTimeText;
    public Text lastSavedText;
    public Button loadButton;
    public Button saveButton;
    public Button deleteButton;
    public GameObject slotContent;       // shown when slot has data
    public GameObject emptySlotContent;  // shown when slot is empty

    [Header("Slot Info")]
    public int slotIndex;

    // -------------------------------------------------------------------------
    // Display
    // -------------------------------------------------------------------------
    public void UpdateDisplay(SaveData data)
    {
        if (slotNumberText != null)
            slotNumberText.text = $"Slot {slotIndex + 1}";

        bool isEmpty = data == null || data.IsEmpty();

        if (slotContent != null) slotContent.SetActive(!isEmpty);
        if (emptySlotContent != null) emptySlotContent.SetActive(isEmpty);

        if (loadButton != null) loadButton.interactable = !isEmpty;
        if (deleteButton != null) deleteButton.interactable = !isEmpty;
        if (saveButton != null) saveButton.interactable = true; // always available

        if (isEmpty) return;

        // --- Populated slot ---
        if (levelText != null)
            levelText.text = data.currentSceneName == "Tutorial"
                ? "Tutorial"
                : $"Level {data.currentLevel}";

        if (healthText != null)
            healthText.text = $"HP: {data.currentHealth} / {data.maxHealth}";

        if (chairLevelText != null)
            chairLevelText.text = $"Chair Lv.{data.currentChairLevel}";

        if (playTimeText != null)
        {
            int totalSeconds = Mathf.FloorToInt(data.totalPlayTime);
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            playTimeText.text = $"{hours}h {minutes:D2}m";
        }

        if (lastSavedText != null)
            lastSavedText.text = string.IsNullOrEmpty(data.lastSaved)
                ? "Not saved yet"
                : $"Saved: {data.lastSaved}";
    }

    // -------------------------------------------------------------------------
    // Button callbacks  (wire these up in the Inspector or from MainMenu.cs)
    // -------------------------------------------------------------------------
    public void OnLoadClicked()
    {
        if (SaveGameManager.Instance == null) return;
        if (!SaveGameManager.Instance.HasValidSave(slotIndex)) return;

        SaveGameManager.Instance.LoadSavedLevel(slotIndex);
    }

    public void OnSaveClicked()
    {
        if (SaveGameManager.Instance == null) return;

        SaveGameManager.Instance.SaveGame(slotIndex);
        Refresh();
    }

    public void OnDeleteClicked()
    {
        if (SaveGameManager.Instance == null) return;

        SaveGameManager.Instance.DeleteSave(slotIndex);
        Refresh();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------
    private void Refresh()
    {
        UpdateDisplay(SaveGameManager.Instance?.GetSaveData(slotIndex));
    }
}