using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateLevelEnd : MonoBehaviour
{
    public GameObject levelEnd;

    // Start is called before the first frame update
    void Start()
    {
        levelEnd = GameObject.FindGameObjectWithTag("LevelEnd");
        levelEnd.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Plated()
    {
        levelEnd.SetActive(true);
    }
}
