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
        // Create a copy of the list to avoid collection modification issues during iteration
        List<GameObject> objectsToDestroy = new List<GameObject>(persistentObjects);

        foreach (GameObject obj in objectsToDestroy)
        {
            // Skip destroying this manager itself
            if (obj != gameObject)
            {
                Destroy(obj);
            }
        }

        // Clear the list (except for this manager)
        persistentObjects.Clear();
        persistentObjects.Add(gameObject);

        Destroy(gameObject);
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
