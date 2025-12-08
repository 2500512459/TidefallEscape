using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class HotUpdateView : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Text _texTips;
    [SerializeField] private Slider _slider;
    [SerializeField] private TMP_Text _texPrgs;
    [SerializeField] private Image _imgPoint;
    private float _sliderWidth;
    private void Start()
    {
        if (_slider != null)
        {
            RectTransform sliderTrans = _slider.transform as RectTransform;
            _sliderWidth = sliderTrans.rect.width;
        }
    }
    /// <summary>
    /// 刷新UI进度
    /// </summary>
    /// <param name="prgs">进度 0-1</param>
    /// <param name="prgsText">进度文本</param>
    public void RefreshUI(float prgs, string prgsText)
    {
        if (_slider != null)
        {
            _slider.value = prgs;
        }
        if (_texPrgs != null)
        {
            _texPrgs.text = prgsText;
        }
        if (_imgPoint != null)
        {
            // 假设imgPoint是Slider的子物体或者在Slider区域内移动
            _imgPoint.rectTransform.anchoredPosition = new Vector3(_sliderWidth * prgs, 0, 0);
        }
    }
    public void Show(bool show)
    {
        gameObject.SetActive(show);
    }
}

