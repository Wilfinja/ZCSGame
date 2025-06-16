using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class LookAtCursor : MonoBehaviour
{
    private Transform mtransfrom;

    private void Start()
    {
        mtransfrom = this.transform;
    }

    private void LAMouse()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Camera.main.transform.position.z; // Add this line

        Vector2 direction = Camera.main.ScreenToWorldPoint(mouseScreenPos) - mtransfrom.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle + 90, Vector3.forward);
        mtransfrom.rotation = rotation;

    }

    void Update()
    {
        LAMouse();
    }
}
