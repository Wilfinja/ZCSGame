using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    //Singleton instance: I need to figure out what EXACTLY this means
    public static GameManager Instance { get; private set; }

    public int health;

    public HealthBarScript healthBar;
    public ChairBarScript chairBar;

    //Character stats and conditions
    public class CharacterStats
    {
        public int MaxHealth { get; set; } = 100;
        public int CurrentHealth { get; set; } = 100;
        public int MaxChairLevel { get; set; } = 5;
        public int CurrentChairLevel { get; set; } = 1;

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
            LoadHealthStats();
        }
    }

    //Save stats to PlayerPrefs
    public void SaveStats()
    {
        PlayerPrefs.SetInt("MaxHealth", PlayerStats.MaxHealth);
        PlayerPrefs.SetInt("CurrentHealth", PlayerStats.CurrentHealth);
        PlayerPrefs.SetInt("MaxChairLevel", PlayerStats.MaxChairLevel);
        PlayerPrefs.SetInt("CurrentChairLevel", PlayerStats.CurrentChairLevel);
        PlayerPrefs.Save();
    }

    //Load stats from PlayerPrefs
    public void LoadHealthStats()
    {
        PlayerStats.MaxHealth = PlayerPrefs.GetInt("MaxHealth", 100);
        healthBar.SetMaxHealth(PlayerStats.MaxHealth);
        
    }

    public void LoadChairStats()
    {
        PlayerStats.MaxChairLevel = PlayerPrefs.GetInt("MaxChairLevel", 5);
        chairBar.SetMaxChair(PlayerStats.MaxChairLevel);
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
        healthBar.SetHeatlth(PlayerStats.CurrentHealth);
    }

    public void UpgradeChair (int amount)
    {
        PlayerStats.CurrentChairLevel = Mathf.Max(PlayerStats.CurrentChairLevel - amount, 0);
        chairBar.SetChair(PlayerStats.CurrentChairLevel);
    }
}
