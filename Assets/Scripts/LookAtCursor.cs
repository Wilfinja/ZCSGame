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
        Vector2 direction = Camera.main.ScreenToWorldPoint(Input.mousePosition) - mtransfrom.position;

        //This is the angle that the weapon must rotate around to face the cursor
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        //Vector3.forward is the z axis
        Quaternion rotation = Quaternion.AngleAxis(angle + 90, Vector3.forward);
        mtransfrom.rotation = rotation;

       
    }

    void Update()
    {
        LAMouse();
    }
}
