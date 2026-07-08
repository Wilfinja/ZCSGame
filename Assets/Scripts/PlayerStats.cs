using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RefreshSceneReferences();
    public static PlayerStats Instance { get; private set; }

    private void Awake()
    {
        //dragBar = FindFirstObjectByType<DragBarScript>();

        //mainCanvas = GameObject.Find("Canvas");

        //gameOverMenu = FindFirstObjectByType<GameOverMenu>();

        //GameOverScreen = GameObject.Find("GameOverScreen");
        //if (GameOverScreen != null) GameOverScreen.SetActive(false);

        //pauseMenu = GameObject.FindGameObjectWithTag("PauseMenu");

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        GameManager = FindFirstObjectByType<GameManager>();
        dragBar = FindFirstObjectByType<DragBarScript>();
        gameOverMenu = FindFirstObjectByType<GameOverMenu>();

        RefreshSceneReferences();

        GameOverScreen = GameObject.Find("GameOverScreen");
        if (GameOverScreen != null) GameOverScreen.SetActive(false);

        pauseMenu = GameObject.FindGameObjectWithTag("PauseMenu");
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
        //GameManager = FindFirstObjectByType<GameManager>();
        if (GameManager == null) return;

        dragLevel = (int)rb.linearDamping;
        dragBar.SetDrag(dragLevel);

        if (health <= 0 && !GameOver)
        {
            GameOverScreen.SetActive(true);
            ClickToMove.gameOver();
            gameOverMenu.GameOver();

            GameOver = true;
            GameOverScreen.SetActive(true);
        }
    }

    public void PauseRegen()
    {
        gameObject.GetComponent<PauseMenu>().OpenPause();
    }

    private void RefreshSceneReferences()
    {
        rb = GetComponent<Rigidbody2D>();
        GameManager = FindFirstObjectByType<GameManager>();
        dragBar = FindFirstObjectByType<DragBarScript>();
        gameOverMenu = FindFirstObjectByType<GameOverMenu>();

        GameOverScreen = GameObject.Find("GameOverScreen");
        if (GameOverScreen != null) GameOverScreen.SetActive(false);

        pauseMenu = GameObject.FindGameObjectWithTag("PauseMenu");
    }

}
