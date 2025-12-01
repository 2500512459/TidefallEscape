using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public Gradient gradient;
    public Image fillImage;
    [Range(0f, 1f)]
    public float alpha = 1f; // 通透率（透明度），0为完全透明，1为完全不透明

    public void SetMaxHealth(float health)
    {
        slider.maxValue = health;
        slider.value = health;

        Color color = gradient.Evaluate(1f);
        color.a = alpha;
        fillImage.color = color;
    }
    public void SetHealth(float health)
    {
        slider.value = health;

        Color color = gradient.Evaluate(slider.normalizedValue);
        color.a = alpha;
        fillImage.color = color;
    }

    /// <summary>
    /// 设置血条的透明度
    /// </summary>
    /// <param name="newAlpha">透明度值，范围0-1，0为完全透明，1为完全不透明</param>
    public void SetAlpha(float newAlpha)
    {
        alpha = Mathf.Clamp01(newAlpha);
        Color color = fillImage.color;
        color.a = alpha;
        fillImage.color = color;
    }
}
