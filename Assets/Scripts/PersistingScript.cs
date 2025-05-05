using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistingScript : MonoBehaviour
{
    public static PersistingScript Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Register with the manager
        if (PersistantObjDestroyer.Instance != null)
        {
            PersistantObjDestroyer.Instance.RegisterPersistentObject(gameObject);
        }
    }

    void OnDestroy()
    {
        // Unregister when destroyed
        if (PersistantObjDestroyer.Instance != null)
        {
            PersistantObjDestroyer.Instance.UnregisterPersistentObject(gameObject);
        }
    }
}
