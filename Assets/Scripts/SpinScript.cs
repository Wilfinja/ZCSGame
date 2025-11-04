using UnityEngine;
using UnityEngine.Rendering;

public class SpinScript : MonoBehaviour
{
    [SerializeField] private float rotationspeed = 50f;
    [SerializeField] private Transform rotateAround;

    // Update is called once per frame
    void Update()
    {
        this.transform.Rotate(Vector3.forward, rotationspeed *  Time.deltaTime);
    }
}
