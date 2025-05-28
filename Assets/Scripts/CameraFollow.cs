using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject playerTransform;

    private Transform cameraLocation;

    public static CameraFollow Instance { get; private set; }

    private void Awake()
    {

        playerTransform = GameObject.Find("MrHandsome");
        cameraLocation = playerTransform.transform.Find("CameraPosition");

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


        // Update is called once per frame
        void Update()
    {

        if (playerTransform == null)
        {
            playerTransform = GameObject.Find("MrHandsome");

            if(playerTransform != null)
            {
                cameraLocation = playerTransform.transform.Find("CameraPosition");
            }
            
        }
        else
        {
            transform.position = cameraLocation.position;
        }
        

    }
}
