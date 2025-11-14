using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ManaPointsBar : MonoBehaviour
{
    public Slider slider;
    public Gradient gradient;
    public Image fillImage;
    public void SetMaxManaPoints(float MaxMP)
    {
        slider.maxValue = MaxMP;
        slider.value = MaxMP;

        fillImage.color = gradient.Evaluate(1f);
    }
    public void SetManaPoints(float MP)
    {
        slider.value = MP;

        fillImage.color = gradient.Evaluate(slider.normalizedValue);
    }
}
