using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    public static bool gameIsPaused = false;

    public GameObject gameOverMenuUI;

    public GameObject gameManager;

    public GameObject player;

    public GameObject gameCamera;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        gameManager = GameObject.Find("GameManager");
        gameCamera = GameObject.FindGameObjectWithTag("MainCamera");
    }


    public void GameOver()
    {
        //Debug.Log("GAME OVER Screen");

        gameOverMenuUI.SetActive(true);
        gameIsPaused = true;
        Time.timeScale = .5f;
        player.GetComponent<ClickToMove>().enabled = false;
        player.GetComponent<LookAtCursor>().enabled = false;
        player.GetComponent<PlayerInput>().enabled = false;
        player.GetComponent<Rigidbody2D>().freezeRotation = true;
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
        //Save on exit here?

        PersistantObjDestroyer.Instance.DestroyAllPersistentObjects();
        Destroy(gameManager);
        Destroy(player);

    }

    public void Quit()
    {
        Application.Quit();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();

        PersistantObjDestroyer.Instance.DestroyAllPersistentObjects();
        Destroy(gameManager);
        Destroy(player);
        Destroy(gameCamera);

        SceneManager.LoadScene(currentScene.buildIndex);
    }
}
