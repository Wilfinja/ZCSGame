using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransition : MonoBehaviour
{
    public GameObject player;

    private void Awake()
    {
        player = GameObject.Find("GameManager");

        
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

            GameManager stats = player.GetComponent<GameManager>();

            stats.LoadHealthStats();
        }
    }
}
