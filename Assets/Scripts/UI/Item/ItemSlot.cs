using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// 物品格 UI 控制类
/// - 仅负责显示物品图标、名称、数量等通用信息
/// - 不包含交互逻辑
/// </summary>
public class ItemSlot : MonoBehaviour
{
    [Header("UI组件")]
    public Image backgroundImage;       // 背景图片
    public Image itemIcon;              // 物品图标
    public TextMeshProUGUI itemName;    // 物品名称文本
    public TextMeshProUGUI itemCount;   // 数量显示文本
    public GameObject selectNode;       // 选中高亮节点

    [Header("数据绑定")]
    public ItemStack currentItem;           // 当前格子绑定的物品数据（可能为 null）

    protected RectTransform rectTransform;    // 自身的 RectTransform 缓存引用

    protected virtual void Awake()
    {
    }

    /// <summary>
    /// 缓存 RectTransform 引用
    /// </summary>
    public virtual RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            return rectTransform;
        }
    }

    /// <summary>
    /// 设置物品信息（绑定数据并刷新 UI）
    /// </summary>
    /// <param name="itemStack">物品堆叠数据</param>
    public virtual void SetItem(ItemStack itemStack)
    {
        currentItem = itemStack;

        // 空数据时清空显示
        if (itemStack == null || itemStack.item == null)
        {
            ClearSlot();
            return;
        }

        // 正常显示物品
        itemIcon.enabled = true;
        itemIcon.sprite = itemStack.item.icon;
        itemName.text = itemStack.item.itemName;
        itemCount.text = itemStack.count > 1 ? itemStack.count.ToString() : "";
    }

    /// <summary>
    /// 清空格子显示
    /// </summary>
    public virtual void ClearSlot()
    {
        currentItem = null;
        itemIcon.enabled = false;
        itemIcon.sprite = null;
        itemName.text = "";
        itemCount.text = "";
    }


    /// <summary>
    /// 更新格子在 Content 中的位置
    /// </summary>
    /// <param name="pos">新的锚点坐标</param>
    public virtual void UpdateCellPos(Vector2 pos)
    {
        RectTransform.anchoredPosition = pos;
    }

    /// <summary>
    /// 设置选中状态（控制选中高亮）
    /// </summary>
    public virtual void UpdateCellSelect(bool select)
    {
        if (selectNode != null)
            selectNode.SetActive(select);
    }

    /// <summary>
    /// 是否为空格子
    /// </summary>
    public bool IsEmpty() => currentItem == null || currentItem.item == null;
}
