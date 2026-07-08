using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistantObjDestroyer : MonoBehaviour
{
    // Singleton instance
    public static PersistantObjDestroyer Instance { get; private set; }

    // List to track all persistent objects
    private List<GameObject> persistentObjects = new List<GameObject>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Register a persistent object
    public void RegisterPersistentObject(GameObject obj)
    {
        if (!persistentObjects.Contains(obj))
        {
            persistentObjects.Add(obj);
        }
    }

    // Unregister an object (for cleanup)
    public void UnregisterPersistentObject(GameObject obj)
    {
        if (persistentObjects.Contains(obj))
        {
            persistentObjects.Remove(obj);
        }
    }

    // Destroy all persistent objects
    public void DestroyAllPersistentObjects()
    {
        List<GameObject> objectsToDestroy = new List<GameObject>(persistentObjects);

        foreach (GameObject obj in objectsToDestroy)
        {
            if (obj != gameObject)
            {
                Destroy(obj);
            }
        }

        persistentObjects.Clear();
        // Don't destroy this manager — it needs to keep tracking objects
        // registered by whatever loads next.
    }

    // Destroy specific types of persistent objects
    public void DestroyPersistentObjectsOfType<T>() where T : Component
    {
        List<GameObject> objectsToDestroy = new List<GameObject>();

        foreach (GameObject obj in persistentObjects)
        {
            if (obj.GetComponent<T>() != null)
            {
                objectsToDestroy.Add(obj);
            }
        }

        foreach (GameObject obj in objectsToDestroy)
        {
            persistentObjects.Remove(obj);
            Destroy(obj);
        }
    }
}
