using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChairBarScript : MonoBehaviour
{
    public Slider slider;

    public void SetMaxChair(int chair)
    {
        slider.maxValue = chair;
        slider.value = chair;
    }

    public void SetChair(int chair)
    {
        slider.value = chair;
    }
}
