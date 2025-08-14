using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class PauseMenu : MonoBehaviour
{
    public static bool gameIsPaused = false;

    public GameObject pauseMenuUI;
    public GameObject player;

    public GameObject gameManager;

    PlayerInput input;

    private void Awake()
    {
        //StartCoroutine(BeginDisable());
        player = GameObject.FindGameObjectWithTag("Player");
        //player.GetComponent<PlayerStats>().PauseRegen();

        //pauseMenuUI = gameObject.GetComponent<PlayerStats>().pauseMenu;

        //pauseMenuUI.SetActive(false);

        // Register with the manager
        if (PersistantObjDestroyer.Instance != null)
        {
            PersistantObjDestroyer.Instance.RegisterPersistentObject(gameObject);
        }
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        pauseMenuUI = gameObject.GetComponent<PlayerStats>().pauseMenu;
        input = gameObject.GetComponent<PlayerInput>();
        //pauseMenuUI = GameObject.FindGameObjectWithTag("PauseMenu");
    }

    public void Pause(InputAction.CallbackContext context)
    {
            if (gameIsPaused && context.started == true)
            {
                //This is when the game is paused and then is called to run again

                Time.timeScale = 1f;

                //StartCoroutine(PauseDelay());

                pauseMenuUI.SetActive(false);

                //input.enabled = true;

                gameIsPaused = false;

                Debug.Log("Game is Resumed");

                //player.GetComponent<ClickToMove>().enabled = true;
            }
            else if (!gameIsPaused && context.started == true)
            {
                //This is when the game is running and then is paused
                //pauseMenuUI = GameObject.FindGameObjectWithTag("PauseMenu");

                pauseMenuUI.SetActive(true);

                //input.enabled = false;

                gameIsPaused = true;

                Debug.Log("Game is Paused");

                //StartCoroutine(PauseDelay());

                //player.GetComponent<ClickToMove>().enabled = false;
                Time.timeScale = 0f;
            } 
    }

    private void FixedUpdate()
    {
        if(!gameIsPaused)
        {
            input.enabled = true;
        }
        else
        {
            input.enabled = false;
        }
    }

    public void Resume()
    {
        if (gameIsPaused)
        {
            
            Time.timeScale = 1f;

            pauseMenuUI.SetActive(false);
            gameIsPaused = false;
                        
        }
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);

        //Save on exit here?

        Object.Destroy(gameManager);
    }

    public void Quit()
    {
        Application.Quit();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Regen()
    {
        gameObject.SetActive(true);
    }

    IEnumerator PauseDelay()
    {
        yield return new WaitForSeconds(.2f);
        Time.timeScale = 1f;

    }

    //IEnumerator BeginDisable()
    //{
    //    yield return new WaitForSeconds(0.1f);

    //    pauseMenuUI.gameObject.SetActive(false);
    //}
}
