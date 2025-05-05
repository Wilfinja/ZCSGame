using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool gameIsPaused = false;

    public GameObject pauseMenuUI;

    public GameObject gameManager;

    public void Pause()
    {
        if (gameIsPaused)
        {
            //This is when the game is paused and then is called to run again
            pauseMenuUI.SetActive(false);
            gameIsPaused = false;
            Time.timeScale = 1f;
        }
        else
        {
            //This is when the game is running and then is paused
            pauseMenuUI.SetActive(true);
            gameIsPaused = true;
            Time.timeScale = 0f;
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
}
