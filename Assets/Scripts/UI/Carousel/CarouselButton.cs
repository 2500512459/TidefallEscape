using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 轮播按钮组件
/// 处理按钮的悬停效果，当鼠标进入/离开时平滑改变背景透明度
/// </summary>
public class CarouselButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>
    /// 透明度变化动画持续时间（秒）
    /// </summary>
    [SerializeField] private float duration = 0.25f;
    /// <summary>
    /// 悬停时的背景颜色
    /// </summary>
    [SerializeField] private Color hoverColor;
    /// <summary>
    /// 按钮背景图片组件
    /// </summary>
    [SerializeField] private Image buttonBackground;
    /// <summary>
    /// 按钮组件
    /// </summary>
    [SerializeField] private Button button;
    
    /// <summary>
    /// 当前正在执行的透明度变化协程
    /// </summary>
    private Coroutine _alphaChangeCoroutine;
    
    /// <summary>
    /// Unity编辑器验证方法
    /// 自动查找组件并设置按钮颜色配置
    /// </summary>
    private void OnValidate()
    {
        button = GetComponent<Button>();
        buttonBackground = GetComponent<Image>();
        
        if (button != null)
        {
            if (button.transition != Selectable.Transition.ColorTint)
                return;
            var colorBlock = button.colors;
            colorBlock.normalColor = hoverColor;
            colorBlock.highlightedColor = hoverColor;
            colorBlock.pressedColor = hoverColor;
            colorBlock.selectedColor = hoverColor;
            colorBlock.disabledColor = Color.clear;
            button.colors = colorBlock;
        }
        
        if (buttonBackground != null)
            buttonBackground.color = hoverColor;
    }
    
    /// <summary>
    /// 初始化时设置背景为完全透明
    /// </summary>
    private void Start()
    {
        buttonBackground.color = Color.clear;
    }
    
    /// <summary>
    /// 鼠标指针进入事件处理
    /// 开始淡入背景颜色
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_alphaChangeCoroutine != null)
            StopCoroutine(_alphaChangeCoroutine);
        
        _alphaChangeCoroutine = StartCoroutine(ChangeAlpha(1, duration));
    }
    
    /// <summary>
    /// 鼠标指针离开事件处理
    /// 开始淡出背景颜色
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (_alphaChangeCoroutine != null)
            StopCoroutine(_alphaChangeCoroutine);
        
        _alphaChangeCoroutine = StartCoroutine(ChangeAlpha(0, duration));
    }
    
    /// <summary>
    /// 平滑改变背景透明度
    /// </summary>
    /// <param name="targetAlpha">目标透明度（0-1）</param>
    /// <param name="duration">变化持续时间（秒）</param>
    /// <returns>协程迭代器</returns>
    private IEnumerator ChangeAlpha(float targetAlpha, float duration)
    {
        float startAlpha = buttonBackground.color.a;
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            float lerpValue = time / duration;
            Color newColor = buttonBackground.color;
            newColor.a = Mathf.Lerp(startAlpha, targetAlpha, lerpValue);
            buttonBackground.color = newColor;
            yield return null;
        }
    }
}

