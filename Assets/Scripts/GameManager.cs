using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    //Singleton instance: I need to figure out what EXACTLY this means
    public static GameManager Instance { get; private set; }

    public int health;

    //Character stats and conditions
    public class CharacterStats
    {
        public int MaxHealth { get; set; } = 100;
        public int CurrentHealth { get; set; } = 100;

    }

    public CharacterStats PlayerStats { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeStats();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeStats()
    {
        //Initialize with default values if no saved data exists
        if (PlayerStats == null)
        {
            PlayerStats = new CharacterStats();
            LoadStats();
        }
    }

    //Save stats to PlayerPrefs
    public void SaveStats()
    {
        PlayerPrefs.SetInt("MaxHealth", PlayerStats.MaxHealth);
        PlayerPrefs.SetInt("CurrentHealth", PlayerStats.CurrentHealth);
        PlayerPrefs.Save();
    }

    //Load stats from PlayerPrefs
    private void LoadStats()
    {
        PlayerStats.MaxHealth = PlayerPrefs.GetInt("MaxHealth", 100);
    }

    //Method to update stats like when leveling up etc.
    public void UpdateStats(int maxHealth)
    {
        PlayerStats.MaxHealth += maxHealth;
        SaveStats();
    }

    //Method to heal character
    public void HealCharacter(int amount)
    {
        PlayerStats.CurrentHealth = Mathf.Min(PlayerStats.CurrentHealth + amount, PlayerStats.MaxHealth);
        SaveStats();
    }

    public void TakeDamge (int amount)
    {
        PlayerStats.CurrentHealth = Mathf.Max(PlayerStats.CurrentHealth -  amount, 0);
    }
}
