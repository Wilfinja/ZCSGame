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

        //StartCoroutine(BeginDisable());
    }


    public void GameOver()
    {
        //Debug.Log("GAME OVER Screen");

        gameOverMenuUI.SetActive(true);
        gameIsPaused = true;
        Time.timeScale = .5f;
        player = GameObject.FindGameObjectWithTag("Player");
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

        gameManager = GameObject.Find("GameManager");
        Destroy(gameManager);

        player = GameObject.FindGameObjectWithTag("Player");
        Destroy(player);

        gameCamera = GameObject.FindGameObjectWithTag("MainCamera");
        Destroy(gameCamera);
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
        gameManager = GameObject.Find("GameManager");
        Destroy(gameManager);

        player = GameObject.FindGameObjectWithTag("Player");
        player.GetComponent<PlayerStats>().PauseRegen();
        Destroy(player);

        gameCamera = GameObject.FindGameObjectWithTag("MainCamera");
        Destroy(gameCamera);



        SceneManager.LoadScene(currentScene.buildIndex);
    }

    //IEnumerator BeginDisable()
    //{
    //    yield return new WaitForSeconds(0.1f);

    //    gameOverMenuUI.gameObject.SetActive(false);
    //}
}
