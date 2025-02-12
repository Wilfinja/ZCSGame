using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public GameManager GameManager;

    public int health;
    public int maxHealth;

    public int chairLevel;
    public int maxChairLevel;

    public void TakeDamage(int amount)
    {
        GameManager.TakeDamge(amount);
        health = GameManager.PlayerStats.CurrentHealth;
    }

}
