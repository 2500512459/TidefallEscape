using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// 轮播图主控制器
/// 负责管理轮播图的滚动、指示器、文本显示和自动滚动功能
/// 支持拖拽手势和自动滚动两种交互方式
/// </summary>
public class Carousel : MonoBehaviour, IEndDragHandler
{
    [Header("Parts Setup")]
    /// <summary>
    /// 轮播条目数据列表，包含每个轮播项的图片、标题、描述等信息
    /// </summary>
    [SerializeField] private List<CarouselEntry> entries = new List<CarouselEntry>();
    
    [Space]
    /// <summary>
    /// 滚动视图组件，用于控制轮播图的滚动
    /// </summary>
    [SerializeField] private ScrollRect scrollRect;
 
    [Space]
    /// <summary>
    /// 水平内容容器，用于放置轮播图片
    /// </summary>
    [SerializeField] private RectTransform contentBoxHorizontal;
    /// <summary>
    /// 轮播条目预制体，用于实例化每个轮播项
    /// </summary>
    [SerializeField] private Image carouselEntryPrefab;
    /// <summary>
    /// 存储所有已实例化的轮播图片
    /// </summary>
    private List<Image> _imagesForEntries = new List<Image>();
    
    [Space]
    /// <summary>
    /// 指示器父节点，用于放置轮播指示器
    /// </summary>
    [SerializeField] private Transform indicatorParent;
    /// <summary>
    /// 指示器预制体，用于显示当前轮播位置
    /// </summary>
    [SerializeField] private CarouselIndicator indicatorPrefab;
    /// <summary>
    /// 存储所有已实例化的指示器
    /// </summary>
    private List<CarouselIndicator> _indicators = new List<CarouselIndicator>();
    
    [Header("Animation Setup")]
    /// <summary>
    /// 滚动动画持续时间（秒），范围0.25-1秒
    /// </summary>
    [SerializeField, Range(0.25f, 1f)] private float duration = 0.5f;
    /// <summary>
    /// 滚动动画的缓动曲线
    /// </summary>
    [SerializeField] private AnimationCurve easeCurve;
    
    [Header("Auto Scroll Setup")]
    /// <summary>
    /// 是否启用自动滚动功能
    /// </summary>
    [SerializeField] private bool autoScroll = false;
    /// <summary>
    /// 自动滚动的时间间隔（秒）
    /// </summary>
    [SerializeField] private float autoScrollInterval = 5f;
    /// <summary>
    /// 自动滚动计时器
    /// </summary>
    private float _autoScrollTimer;
    [Header("Info Setup")]
    /// <summary>
    /// 文本框控制器，用于显示和更新轮播项的标题和描述
    /// </summary>
    [SerializeField] private CarouselTextBox textBoxController;
    /// <summary>
    /// 行动按钮，点击后执行当前轮播项的交互操作
    /// </summary>
    [SerializeField] private Button callToAction;
    
    
    
    /// <summary>
    /// 当前显示的轮播项索引
    /// </summary>
    private int _currentIndex = 0;
    /// <summary>
    /// 当前正在执行的滚动协程
    /// </summary>
    private Coroutine _scrollCoroutine;
    
    /// <summary>
    /// Unity编辑器重置方法，自动查找子组件
    /// </summary>
    private void Reset()
    {
        scrollRect = GetComponentInChildren<ScrollRect>();
        textBoxController = GetComponentInChildren<CarouselTextBox>();
    }
    
    /// <summary>
    /// 初始化轮播图
    /// 实例化所有轮播图片和指示器，并设置初始状态
    /// </summary>
    private void Start()
    {
        foreach (var entry in entries)
        {
            Image carouselEntry = Instantiate(carouselEntryPrefab, contentBoxHorizontal);
            carouselEntry.sprite = entry.EntryGraphic;
            _imagesForEntries.Add(carouselEntry);
            
            var indicator = Instantiate(indicatorPrefab, indicatorParent);
            indicator.Initialize(() => ScrollToSpecificIndex(entries.IndexOf(entry)));
            _indicators.Add(indicator);
        }
        
        _autoScrollTimer = autoScrollInterval;
        
        // 初始化第一个轮播项的状态，包括按钮监听器
        ScrollTo(0);
    }
    
    /// <summary>
    /// 清除当前索引的状态
    /// 停用当前指示器并移除当前项的按钮监听器
    /// </summary>
    private void ClearCurrentIndex()
    {
        _indicators[_currentIndex].Deactivate(duration);
        callToAction.onClick.RemoveAllListeners();
    }
    
    /// <summary>
    /// 处理当前轮播项的交互操作
    /// 调用LoadManager加载场景
    /// </summary>
    private void HandleCurrentEntryInteraction()
    {
        if (LoadManager.Instance != null && _currentIndex >= 0 && _currentIndex < entries.Count)
        {
            entries[_currentIndex].Interact(LoadManager.Instance.LoadScene);
        }
    }
    
    /// <summary>
    /// 滚动到指定索引位置
    /// </summary>
    /// <param name="index">目标索引</param>
    private void ScrollToSpecificIndex(int index)
    {
        ClearCurrentIndex();
        
        ScrollTo(index);
    }
    
    /// <summary>
    /// 滚动到下一个轮播项
    /// </summary>
    public void ScrollToNext()
    {
        ClearCurrentIndex();
        
        _currentIndex = (_currentIndex + 1) % _imagesForEntries.Count;
        ScrollTo(_currentIndex);
    }
    
    /// <summary>
    /// 滚动到上一个轮播项
    /// </summary>
    public void ScrollToPrevious()
    {
        ClearCurrentIndex();
        
        _currentIndex = (_currentIndex - 1 + _imagesForEntries.Count) % _imagesForEntries.Count;
        ScrollTo(_currentIndex);
    }
    
    /// <summary>
    /// 执行滚动到指定索引的核心逻辑
    /// 更新滚动位置、文本内容、指示器状态和按钮监听器
    /// </summary>
    /// <param name="index">目标索引</param>
    private void ScrollTo(int index)
    {
        _currentIndex = index;
        _autoScrollTimer = autoScrollInterval;
        float targetHorizontalPosition = (float)_currentIndex / (_imagesForEntries.Count - 1);
        
        if (_scrollCoroutine != null)
            StopCoroutine(_scrollCoroutine);
        
        _scrollCoroutine = StartCoroutine(LerpToPos(targetHorizontalPosition));
        
        var headline = entries[_currentIndex].Headline;
        var description = entries[_currentIndex].Description;
        
        textBoxController.SetText(headline, description, duration);
        
        _indicators[_currentIndex].Activate(duration);
        callToAction.onClick.AddListener(HandleCurrentEntryInteraction);
    }
    
    /// <summary>
    /// 使用缓动曲线平滑滚动到目标位置
    /// </summary>
    /// <param name="targetHorizontalPosition">目标水平位置（0-1之间）</param>
    /// <returns>协程迭代器</returns>
    private IEnumerator LerpToPos(float targetHorizontalPosition)
    {  
        float elapsedTime = 0f;
        float initialPos = scrollRect.horizontalNormalizedPosition;
        
        if (duration > 0)
        {
            while (elapsedTime <= duration)
            {
                float easeValue = easeCurve.Evaluate(elapsedTime / duration);
                float newPosition = Mathf.Lerp(initialPos, targetHorizontalPosition, easeValue);
                scrollRect.horizontalNormalizedPosition = newPosition;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
    
        scrollRect.horizontalNormalizedPosition = targetHorizontalPosition;
    }
    
    /// <summary>
    /// 每帧更新，处理自动滚动逻辑
    /// </summary>
    private void Update()
    {
        if (!autoScroll) 
            return;
        
        _autoScrollTimer -= Time.deltaTime;
        if (_autoScrollTimer <= 0)
        {
            ScrollToNext();
            _autoScrollTimer = autoScrollInterval;
        }
    }
    
    /// <summary>
    /// 拖拽结束事件处理
    /// 根据拖拽方向决定滚动到上一项还是下一项
    /// </summary>
    /// <param name="data">指针事件数据</param>
    public void OnEndDrag(PointerEventData data)
    {
        if (data.delta.x != 0)
        {
            if (data.delta.x > 0)
                ScrollToPrevious();
            else if (data.delta.x < 0)
                ScrollToNext();
        }
        else
            ScrollToSpecificIndex(_currentIndex);
    }
}
