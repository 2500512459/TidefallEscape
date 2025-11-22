using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 轮播文本框控制器
/// 管理轮播项的标题和描述文本的显示
/// 支持淡入淡出效果，提供平滑的文本切换动画
/// </summary>
public class CarouselTextBox : MonoBehaviour
{
    /// <summary>
    /// 标题文本组件
    /// </summary>
    [SerializeField] private TMP_Text headline;
    /// <summary>
    /// 描述文本组件
    /// </summary>
    [SerializeField] private TMP_Text description;
    
    /// <summary>
    /// 是否启用文本淡入淡出效果
    /// </summary>
    [SerializeField] private bool fadeText = true;
    
    /// <summary>
    /// 淡入淡出动画的总持续时间（秒）
    /// </summary>
    private float _fadeDuration = 0.5f;
    /// <summary>
    /// 淡入或淡出的单次持续时间（总时长的一半）
    /// </summary>
    private float _halfFadeDuration => _fadeDuration * 0.5f;
    
    /// <summary>
    /// 当前正在执行的淡入淡出协程
    /// </summary>
    private Coroutine _fadeCoroutine;
    
    /// <summary>
    /// 直接设置文本内容，不使用淡入淡出效果
    /// 用于初始化或需要立即显示文本的情况
    /// </summary>
    /// <param name="headlineText">标题文本</param>
    /// <param name="descriptionText">描述文本</param>
    public void SetTextWithoutFade(string headlineText, string descriptionText)
    {
        headline.SetText(headlineText);
        description.SetText(descriptionText);
        
        headline.alpha = 1;
        description.alpha = 1;
    }
    
    /// <summary>
    /// 设置文本内容，支持淡入淡出效果
    /// 如果禁用淡入淡出或持续时间为0，则直接设置文本
    /// </summary>
    /// <param name="headlineText">标题文本</param>
    /// <param name="descriptionText">描述文本</param>
    /// <param name="fadingDuration">淡入淡出持续时间（秒），默认0表示不使用淡入淡出</param>
    public void SetText(string headlineText, string descriptionText, float fadingDuration = 0f)
    {
        if (!fadeText || fadingDuration <= 0)
        {
            SetTextWithoutFade(headlineText, descriptionText);
            return;
        }
        
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            
            headline.alpha = 1;
            description.alpha = 1;
        }
        
        _fadeDuration = fadingDuration;
        _fadeCoroutine = StartCoroutine(FadeText(headlineText, descriptionText));
    }
    
    /// <summary>
    /// 执行文本淡入淡出动画
    /// 先淡出旧文本，然后更新文本内容，最后淡入新文本
    /// </summary>
    /// <param name="headlineText">新的标题文本</param>
    /// <param name="descriptionText">新的描述文本</param>
    /// <returns>协程迭代器</returns>
    private IEnumerator FadeText(string headlineText, string descriptionText)
    {
        // 第一阶段：淡出旧文本
        float time = 0;
        while (time < _halfFadeDuration)
        {
            time += Time.deltaTime;
            float lerpValue = 1 - (time / _halfFadeDuration);
            headline.alpha = lerpValue;
            description.alpha = lerpValue;
            yield return null;
        }
        
        // 更新文本内容
        headline.SetText(headlineText);
        description.SetText(descriptionText);
        time = 0;
        
        // 第二阶段：淡入新文本
        while (time < _halfFadeDuration)
        {
            time += Time.deltaTime;
            float lerpValue = time / _halfFadeDuration;
            headline.alpha = lerpValue;
            description.alpha = lerpValue;
            yield return null;
        }
        headline.alpha = 1;
        description.alpha = 1;
    }
}
