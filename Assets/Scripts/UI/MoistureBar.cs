using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MoistureBar : MonoBehaviour
{
    public Slider slider;
    public Gradient gradient;
    public Image fillImage;
    [Range(0f, 1f)]
    public float alpha = 1f; // 通透率（透明度），0为完全透明，1为完全不透明
    public void SetMaxMoisture(float MaxMoisture)
    {
        slider.maxValue = MaxMoisture;
        slider.value = MaxMoisture;

        Color color = gradient.Evaluate(1f);
        color.a = alpha;
        fillImage.color = color;
    }
    public void SetMoisture(float Moisture)
    {
        slider.value = Moisture;

        Color color = gradient.Evaluate(slider.normalizedValue);
        color.a = alpha;
        fillImage.color = color;
    }
    public void SetAlpha(float newAlpha)
    {
        alpha = Mathf.Clamp01(newAlpha);
        Color color = fillImage.color;
        color.a = alpha;
        fillImage.color = color;
    }
}

