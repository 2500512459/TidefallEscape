using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class LoopScrollView : MonoBehaviour
{
    [Header("ScrollView 信息")]
    private ScrollRect scrollRect;  // 滚动视图组件
    private float height;       // 滚动视图高度
    private float width;        // 滚动视图宽度
    public Transform content;   // 内容节点
    private RectTransform contentRect;  // 内容节点的 RectTransform

    [Header("物品槽信息")]
    public StorageItem StorageItem; // 物品槽预制体
    private float itemHeight;    // 物品槽高度
    private float itemWidth;     // 物品槽宽度
    public float offsetX = 10f;  // X 轴间距
    public float offsetY = 10f; // Y 轴间距

    //列表能显示的行列
    private int row;
    private int column;

    //列表最多显示的cell个数
    private int maxShowItemNum;

    //列表显示的Item列表，用于设置位置,只存了0-显示的个数这么多
    private List<StorageItem> showItems = new List<StorageItem>();

    //总行数
    private int totalRow;
    //上次显示的节点序号
    private int preStartIndex = 0;
    //总数
    public int totalNum = 100;

    //事件相关
    public UnityAction<StorageItem, int> updateItemAction;

    private UnityAction<int> clickItemAction;

    private void Awake()
    {

    }
    private void OnDestroy()
    {
        updateItemAction = null;
        clickItemAction = null;
    }

    public void InitXScrollView(int num)
    {
        totalNum = num;
        
        scrollRect = GetComponent<ScrollRect>();
        var rect = this.GetComponent<RectTransform>().rect;
        height = rect.height;
        width = rect.width;


        var itemRect = StorageItem.GetComponent<RectTransform>().rect;
        itemHeight = itemRect.height;
        itemWidth = itemRect.width;

        //列
        column = Mathf.FloorToInt(width / (itemWidth + offsetX));
        //行
        row = Mathf.CeilToInt(height / (itemHeight + offsetY));
        //最多只创建屏幕显示的个数这么多
        maxShowItemNum = column * (row + 1);
        
        totalRow = Mathf.CeilToInt((float)totalNum / column);

        //隐藏模版cell
        StorageItem.gameObject.SetActive(false);

        //设置content的大小
        contentRect = content.GetComponent<RectTransform>();
        var contentHeight = totalRow * (itemHeight + offsetY); //向上取整，并且乘以每一行的高
        var contentWidth = contentRect.sizeDelta.x;
        var contentSize = new Vector2(contentWidth, contentHeight);
        contentRect.sizeDelta = contentSize;


        //开始创建节点
        StartCoroutine(CreateCell());

        //滚动事件
        scrollRect.onValueChanged.AddListener(ScrollViewOnValueChanged);

    }


    /// <summary>
    /// 添加列表项更新事件
    /// </summary>
    /// <param name="updateAction"></param>
    public void AddUpdateCellAction(UnityAction<StorageItem, int> updateAction)
    {
        updateItemAction += updateAction;
    }
    
    
    /// <summary>
    /// 添加列表项点击事件
    /// </summary>
    /// <param name="clickAction"></param>
    public void AddCellClickAction(UnityAction<int> clickAction)
    {
        clickItemAction += clickAction;
    }


    /// <summary>
    /// 创建格子
    /// </summary>
    /// <returns></returns>
    IEnumerator CreateCell()
    {
        //最多只创建屏幕显示的个数这么多
        var showCell = Mathf.Min(maxShowItemNum, totalNum);
        
        showItems = new List<StorageItem>(showCell);

        for (int i = 0; i < showCell; i++)
        {
            yield return null;

            var index = i;

            var go = GameObject.Instantiate(StorageItem.gameObject, content);
            go.name = $"Cell_{index}";
            var scrollItem = go.GetComponent<StorageItem>();

            UpdateCell(scrollItem, index);
            scrollItem.AddButtonClickListener(clickItemAction);
            go.SetActive(true);

            showItems.Add(scrollItem);
        }
        
    }


    /// <summary>
    /// 更新节点
    /// </summary>
    /// <param name="scrollItem"></param>
    /// <param name="index"></param>
    void UpdateCell(StorageItem scrollItem, int index)
    {
        scrollItem.UpdateCellPos(GetCellPos(index));
        
        updateItemAction?.Invoke(scrollItem, index);
    }



    /// <summary>
    /// 获取节点位置
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    Vector2 GetCellPos(int index)
    {
        var curColumn = index % column; //当前的列
        var curX = curColumn * (itemWidth + offsetX); //当前的x坐标

        var curRow = index / column; //当前的行
        var curY = -curRow * (itemHeight + offsetY); //当前的y坐标

        var pos = new Vector2(curX, curY);
        return pos;
    }

    

    /// <summary>
    /// 滚动事件
    /// </summary>
    /// <param name="v"></param>
    void ScrollViewOnValueChanged(Vector2 v)
    {
        UpdateScrollView();
    }

    public void UpdateScrollView(bool forceUpdate = false)
    {
        var y = contentRect.anchoredPosition.y;
        //可能会小于0
        y = Mathf.Max(0, y);
        //print(y + ":" + v);
        //拖动的距离，超过一行时，则顶部的一行移动到底部显示
        var moveRow = Mathf.FloorToInt(y / (itemHeight + offsetY));
        //视图范围内,移动的行数+视图显示的行数<=总行数
        if (moveRow >= 0 && (moveRow + row) <= totalRow)
        {
            var startIndex = moveRow * column; //起始序号
            //和上次的起始序号不同才进行刷新
            if (!forceUpdate && startIndex == preStartIndex)
            {
                return;
            }

            if (showItems == null || showItems.Count == 0)
            {
                return;
            }

            //更新所有cell
            for (int i = 0; i < showItems.Count; i++)
            {
                var index = startIndex + i;
                ScrollUpdateCell(index, startIndex);
            }

            preStartIndex = startIndex;
        }
    }

    /// <summary>
    /// 更新节点信息
    /// </summary>
    /// <param name="index"></param>
    /// <param name="startIndex"></param>
    void ScrollUpdateCell(int index, int startIndex)
    {
        var itemIndex = index - startIndex;
        if (itemIndex < 0 || itemIndex >= showItems.Count)
        {
            return;
        }

        var scrollViewItem = showItems[itemIndex];
        //超出总数的不显示
        if (index >= totalNum)
        {
            scrollViewItem.gameObject.SetActive(false);
            return;
        }

        scrollViewItem.gameObject.SetActive(true);

        UpdateCell(scrollViewItem, index);
    }

    /// <summary>
    /// 强制刷新当前显示的所有格子内容
    /// </summary>
    public void ForceRefreshVisibleItems()
    {
        if (showItems == null || showItems.Count == 0)
        {
            return;
        }

        for (int i = 0; i < showItems.Count; i++)
        {
            ScrollUpdateCell(preStartIndex + i, preStartIndex);
        }
    }

    /// <summary>
    /// 刷新数据总数并强制更新当前显示内容，可选保持滚动位置
    /// </summary>
    /// <param name="newTotalCount">最新物品总数</param>
    /// <param name="keepPosition">是否保持当前滚动位置</param>
    public void RefreshData(int newTotalCount, bool keepPosition = true)
    {
        totalNum = Mathf.Max(0, newTotalCount);
        totalRow = column > 0 ? Mathf.CeilToInt((float)totalNum / column) : 0;

        if (!keepPosition)
        {
            preStartIndex = 0;
            if (contentRect != null)
            {
                contentRect.anchoredPosition = Vector2.zero;
            }
        }
        else
        {
            int maxStartRow = Mathf.Max(0, totalRow - row);
            int maxStartIndex = maxStartRow * column;
            preStartIndex = Mathf.Clamp(preStartIndex, 0, maxStartIndex);
        }

        if (contentRect != null)
        {
            var contentHeight = totalRow * (itemHeight + offsetY);
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, contentHeight);
        }

        UpdateScrollView(true);
        ForceRefreshVisibleItems();
    }

    /// <summary>
    /// 清空所有已创建的物品格子，并重置滚动状态
    /// </summary>
    public void ClearAllCells()
    {
        // 停止所有协程（防止CreateCell未结束时切换）
        StopAllCoroutines();

        // 移除滚动监听
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(ScrollViewOnValueChanged);
        }

        // 清空旧物品节点
        if (showItems != null && showItems.Count > 0)
        {
            for (int i = 0; i < showItems.Count; i++)
            {
                if (showItems[i] != null)
                {
                    Destroy(showItems[i].gameObject);
                }
            }
            showItems.Clear();
        }

        // 重置Content大小与位置
        if (contentRect != null)
        {
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
        }

        // 重置数据
        totalNum = 0;
        totalRow = 0;
        preStartIndex = 0;
        maxShowItemNum = 0;

        // 重置事件
        updateItemAction = null;
        clickItemAction = null;

        // 清除引用，防止内存泄漏
        scrollRect = null;
    }
}
