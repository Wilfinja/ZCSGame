using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public GameManager GameManager;
    public GameObject GameOverScreen;

    public static bool GameOver = false;

    public int health;
    public int maxHealth;

    public int chairLevel;
    public int maxChairLevel;

    public int dragLevel;
    public DragBarScript dragBar;

    private Rigidbody2D rb;

    public GameOverMenu GameOverMenu;
    public ClickToMove ClickToMove;

    public static PlayerStats Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        //GameOverScreen = GameObject.FindGameObjectWithTag("GameOverScreen");
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

        if (health <= 0)
        {
            //Debug.Log("GAME OVER");

            GameOverScreen.SetActive(true);

            ClickToMove.gameOver();

            GameOverMenu.GameOver();

            //Set the game over script here. Should be similar to the main menu pause script.
            GameOver = true;
            GameOverScreen.SetActive(true);

        }
    }
}
