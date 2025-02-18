using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public GameManager GameManager;

    public int health;
    public int maxHealth;

    public int chairLevel;
    public int maxChairLevel;

    public int dragLevel;
    public DragBarScript dragBar;

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(int amount)
    {
        GameManager.TakeDamge(amount);
        health = GameManager.PlayerStats.CurrentHealth;
    }

    public void UpgradeChair(int amount)
    {
        GameManager.UpgradeChair(amount);
        chairLevel = GameManager.PlayerStats.CurrentChairLevel;
    }

    private void Update()
    {
        dragLevel = (int)rb.drag;
        dragBar.SetDrag(dragLevel);
    }
}
