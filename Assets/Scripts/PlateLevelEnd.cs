using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateLevelEnd : MonoBehaviour
{
    public GameObject levelEnd;
    public GameObject keyObject;

    // Start is called before the first frame update
    void Start()
    {
        if (levelEnd != null)
        {
            levelEnd = GameObject.FindGameObjectWithTag("LevelEnd");
            levelEnd.SetActive(false);
        }
            
        if (keyObject != null)
        {
            keyObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Plated()
    {
        if (levelEnd != null)
        {
            levelEnd.SetActive(true);
        }

        if (keyObject != null)
        {
            keyObject.SetActive(true);
        }
    }
}
