using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DragBarScript : MonoBehaviour
{
    public Slider slider;

    public void SetDrag(int drag)
    {
        slider.value = drag;
    }
}
