using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 船只召唤界面主控制器
/// 负责管理召唤界面的滚动、指示器、文本显示和自动滚动功能
/// 支持拖拽手势和自动滚动两种交互方式
/// 点击召唤按钮后调用 PlayerShipManager 实例化船只模型
/// </summary>
public class Summon : MonoBehaviour, IEndDragHandler
{
    [Header("Parts Setup")]
    /// <summary>
    /// 召唤条目数据列表，包含每个召唤项的图片、船只类型、属性等信息
    /// </summary>
    [SerializeField] private List<SummonEntry> entries = new List<SummonEntry>();
    
    [Space]
    /// <summary>
    /// 滚动视图组件，用于控制召唤界面的滚动
    /// </summary>
    [SerializeField] private ScrollRect scrollRect;
 
    [Space]
    /// <summary>
    /// 水平内容容器，用于放置召唤图片
    /// </summary>
    [SerializeField] private RectTransform contentBoxHorizontal;
    /// <summary>
    /// 召唤条目预制体，用于实例化每个召唤项
    /// </summary>
    [SerializeField] private Image summonEntryPrefab;
    /// <summary>
    /// 存储所有已实例化的召唤图片
    /// </summary>
    private List<Image> _imagesForEntries = new List<Image>();
    
    [Space]
    /// <summary>
    /// 指示器父节点，用于放置召唤指示器
    /// </summary>
    [SerializeField] private Transform indicatorParent;
    /// <summary>
    /// 指示器预制体，用于显示当前召唤位置
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
    /// 文本框控制器，用于显示和更新召唤项的船只类型和属性
    /// </summary>
    [SerializeField] private CarouselTextBox textBoxController;
    /// <summary>
    /// 召唤按钮，点击后执行召唤当前船只的操作
    /// </summary>
    [SerializeField] private Button callToAction;
    /// <summary>
    /// 召唤按钮的文本组件，用于显示按钮文本
    /// </summary>
    [SerializeField] private TMP_Text callToActionText;
    /// <summary>
    /// 拥有船只时的按钮文本
    /// </summary>
    [SerializeField] private string summonButtonText = "召唤";
    /// <summary>
    /// 未拥有船只时的按钮文本
    /// </summary>
    [SerializeField] private string notOwnedButtonText = "未拥有";
    
    [Header("Timeline Setup")]
    /// <summary>
    /// Intro Director 对象，挂载了 PlayableDirector 组件，用于播放召唤动画
    /// </summary>
    [SerializeField] private GameObject introDirector;
    
    
    
    /// <summary>
    /// 当前显示的召唤项索引
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
        if (callToAction != null)
        {
            callToActionText = callToAction.GetComponentInChildren<TMP_Text>();
        }
    }
    
    /// <summary>
    /// 初始化召唤界面
    /// 实例化所有召唤图片和指示器，并设置初始状态
    /// </summary>
    private void Start()
    {
        foreach (var entry in entries)
        {
            Image summonEntry = Instantiate(summonEntryPrefab, contentBoxHorizontal);
            summonEntry.sprite = entry.EntryGraphic;
            _imagesForEntries.Add(summonEntry);
            
            var indicator = Instantiate(indicatorPrefab, indicatorParent);
            indicator.Initialize(() => ScrollToSpecificIndex(entries.IndexOf(entry)));
            _indicators.Add(indicator);
        }
        
        _autoScrollTimer = autoScrollInterval;
        
        // 初始化第一个召唤项的状态，包括按钮监听器
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
    /// 处理当前召唤项的交互操作
    /// 调用 PlayerShipManager 实例化船只模型，并播放 Intro Timeline 动画
    /// </summary>
    private void HandleCurrentEntryInteraction()
    {
        if (PlayerShipManager.Instance != null && _currentIndex >= 0 && _currentIndex < entries.Count)
        {
            ShipType shipType = entries[_currentIndex].ShipType;
            
            // 检查是否拥有该船只
            if (PlayerDataManager.Instance != null && !PlayerDataManager.Instance.OwnedShips.Contains(shipType))
            {
                Debug.LogWarning($"未拥有船只类型: {shipType}，无法召唤");
                return;
            }
                        

            // 设置当前船只类型
            if (PlayerDataManager.Instance.CurrentShipType != shipType)
            {
                // 设置当前船只类型
                PlayerDataManager.Instance.SetCurrentShipType(shipType);
                // 使能 Intro Director 对象以播放 Timeline 动画
                if (introDirector != null)
                {
                    introDirector.SetActive(true);
                }
            }
            
            // 实例化船只模型
            GameObject shipModel = PlayerShipManager.Instance.InstantiateShipModel();
        }
    }
    
    /// <summary>
    /// 检查指定索引的船只是否被拥有
    /// </summary>
    /// <param name="index">条目索引</param>
    /// <returns>如果拥有返回true，否则返回false</returns>
    private bool IsShipOwned(int index)
    {
        if (index < 0 || index >= entries.Count)
            return false;
        
        if (PlayerDataManager.Instance == null)
            return false;
        
        ShipType shipType = entries[index].ShipType;
        return PlayerDataManager.Instance.OwnedShips.Contains(shipType);
    }
    
    /// <summary>
    /// 更新按钮状态和文本
    /// </summary>
    /// <param name="isOwned">是否拥有该船只</param>
    private void UpdateButtonState(bool isOwned)
    {
        if (callToActionText != null)
        {
            callToActionText.text = isOwned ? summonButtonText : notOwnedButtonText;
        }
        
        if (callToAction != null)
        {
            // 如果未拥有，禁用按钮交互
            callToAction.interactable = isOwned;
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
    /// 滚动到下一个召唤项
    /// </summary>
    public void ScrollToNext()
    {
        ClearCurrentIndex();
        
        _currentIndex = (_currentIndex + 1) % _imagesForEntries.Count;
        ScrollTo(_currentIndex);
    }
    
    /// <summary>
    /// 滚动到上一个召唤项
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
        // 防止除以零：当只有一个条目时，位置应该是 0
        float targetHorizontalPosition = _imagesForEntries.Count > 1 
            ? (float)_currentIndex / (_imagesForEntries.Count - 1) 
            : 0f;
        
        if (_scrollCoroutine != null)
            StopCoroutine(_scrollCoroutine);
        
        _scrollCoroutine = StartCoroutine(LerpToPos(targetHorizontalPosition));
        
        var shipTypeName = entries[_currentIndex].GetShipTypeName();
        var description = entries[_currentIndex].GetDescriptionText();
        
        textBoxController.SetText(shipTypeName, description, duration);
        
        _indicators[_currentIndex].Activate(duration);
        
        // 检查是否拥有该船只，并更新按钮状态
        bool isOwned = IsShipOwned(_currentIndex);
        UpdateButtonState(isOwned);
        
        // 只有拥有该船只时才添加点击监听器
        if (isOwned)
        {
            callToAction.onClick.AddListener(HandleCurrentEntryInteraction);
        }
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

