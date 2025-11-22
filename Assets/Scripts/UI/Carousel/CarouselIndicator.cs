using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 轮播指示器组件
/// 用于显示当前轮播位置，支持点击跳转到对应位置
/// 通过透明度变化来显示激活/非激活状态
/// </summary>
public class CarouselIndicator : MonoBehaviour
{
    /// <summary>
    /// 指示器的图片组件
    /// </summary>
    [SerializeField] private Image image;
    /// <summary>
    /// 指示器的按钮组件，用于点击跳转
    /// </summary>
    [SerializeField] private Button button;
    
    /// <summary>
    /// 当前正在执行的透明度变化协程
    /// </summary>
    private Coroutine _alphaChangeCoroutine;
    
    /// <summary>
    /// 点击指示器时执行的回调动作
    /// </summary>
    private UnityAction _onClickAction;
    
    /// <summary>
    /// 初始化指示器
    /// 设置点击回调函数
    /// </summary>
    /// <param name="onClickAction">点击时执行的回调函数</param>
    public void Initialize(UnityAction onClickAction)
    {
        _onClickAction = onClickAction;
        button.onClick.AddListener(_onClickAction);
    }
    
    /// <summary>
    /// 销毁时移除按钮监听器，防止内存泄漏
    /// </summary>
    private void OnDestroy()
    {
        button.onClick.RemoveListener(_onClickAction);
    }
    
    /// <summary>
    /// Unity编辑器重置方法
    /// 自动查找子组件并设置初始透明度为0
    /// </summary>
    private void Reset()
    {
        image = GetComponentInChildren<Image>();
        var color = image.color;
        color.a = 0;
        image.color = color;
    }
    
    /// <summary>
    /// 激活指示器（显示）
    /// 平滑淡入到完全不透明
    /// </summary>
    /// <param name="duration">淡入持续时间（秒）</param>
    public void Activate(float duration)
    {
        if (_alphaChangeCoroutine != null)
            StopCoroutine(_alphaChangeCoroutine);
        
        _alphaChangeCoroutine = StartCoroutine(ChangeAlpha(1, duration));
    }
    
    /// <summary>
    /// 停用指示器（隐藏）
    /// 平滑淡出到完全透明
    /// </summary>
    /// <param name="duration">淡出持续时间（秒）</param>
    public void Deactivate(float duration)
    {
        if (_alphaChangeCoroutine != null)
            StopCoroutine(_alphaChangeCoroutine);
        
        _alphaChangeCoroutine = StartCoroutine(ChangeAlpha(0, duration));
    }
    
    /// <summary>
    /// 平滑改变指示器透明度
    /// </summary>
    /// <param name="targetAlpha">目标透明度（0-1）</param>
    /// <param name="duration">变化持续时间（秒）</param>
    /// <returns>协程迭代器</returns>
    private IEnumerator ChangeAlpha(float targetAlpha, float duration)
    {
        float startAlpha = image.color.a;
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            float lerpValue = time / duration;
            Color newColor = image.color;
            newColor.a = Mathf.Lerp(startAlpha, targetAlpha, lerpValue);
            image.color = newColor;
            yield return null;
        }
    }
}
