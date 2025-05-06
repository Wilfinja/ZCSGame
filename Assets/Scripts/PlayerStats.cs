using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    public GameManager GameManager;
    public GameObject GameOverScreen;
    public GameObject pauseMenu;
    //public GameObject mainCanvas;

    public static bool GameOver = false;

    public int health;
    public int maxHealth;

    public int chairLevel;
    public int maxChairLevel;

    public int dragLevel;
    public DragBarScript dragBar;

    private Rigidbody2D rb;

    public GameOverMenu gameOverMenu;
    public ClickToMove ClickToMove;

    public static PlayerStats Instance { get; private set; }

    private void Awake()
    {
        dragBar = FindObjectOfType<DragBarScript>();

        //mainCanvas = GameObject.Find("Canvas");

        gameOverMenu = FindObjectOfType<GameOverMenu>();

        GameOverScreen = GameObject.Find("GameOverScreen");
        GameOverScreen.SetActive(false);

        pauseMenu = GameObject.Find("PauseMenu");
        pauseMenu.SetActive(false);

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

            gameOverMenu.GameOver();

            //Set the game over script here. Should be similar to the main menu pause script.
            GameOver = true;
            GameOverScreen.SetActive(true);

        }
    }

    public void PauseRegen()
    {
        pauseMenu.SetActive(true );
    }

}
