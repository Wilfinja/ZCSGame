using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the UI for a single save slot panel.
/// Set the SlotMode in the Inspector to control which buttons are shown.
///
/// LoadMenu  : shows Load + Delete buttons only (main menu load panel)
/// SlotPicker: shows Select button only (new game slot picker)
/// InGame    : shows Save + Load + Delete buttons (pause menu)
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    public enum SlotMode { LoadMenu, SlotPicker, InGame }

    [Header("Mode")]
    [Tooltip("Controls which buttons are visible. Set this in the Inspector.")]
    public SlotMode mode = SlotMode.LoadMenu;

    [Header("UI References")]
    public TextMeshProUGUI slotNumberText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI chairLevelText;
    public TextMeshProUGUI playTimeText;
    public TextMeshProUGUI lastSavedText;
    public Button loadButton;
    public Button saveButton;
    public Button deleteButton;
    public Button selectButton;
    public GameObject slotContent;       // shown when slot has data
    public GameObject emptySlotContent;  // shown when slot is empty

    [Header("Slot Info")]
    public int slotIndex;

    // -------------------------------------------------------------------------
    // Display
    // -------------------------------------------------------------------------
    public void UpdateDisplay(SaveData data)
    {
        bool isEmpty = data == null || data.IsEmpty();

        // Slot number always shows
        if (slotNumberText != null)
            slotNumberText.text = $"Slot {slotIndex + 1}";

        // Show the right content panel
        if (slotContent != null) slotContent.SetActive(!isEmpty);
        if (emptySlotContent != null) emptySlotContent.SetActive(isEmpty);

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
    // Button callbacks
    // -------------------------------------------------------------------------
    public void OnLoadClicked()
    {
        Debug.Log($"[SaveSlotUI] OnLoadClicked - slot {slotIndex}");

        if (SaveGameManager.Instance == null)
        {
            Debug.LogWarning("[SaveSlotUI] SaveGameManager is NULL!");
            return;
        }
        if (!SaveGameManager.Instance.HasValidSave(slotIndex))
        {
            Debug.LogWarning($"[SaveSlotUI] No valid save in slot {slotIndex}!");
            return;
        }

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
        Debug.Log($"[SaveSlotUI] OnDeleteClicked - slot {slotIndex}");

        if (SaveGameManager.Instance == null)
        {
            Debug.LogWarning("[SaveSlotUI] SaveGameManager is NULL!");
            return;
        }

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