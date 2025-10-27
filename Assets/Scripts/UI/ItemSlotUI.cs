using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 单个物品槽 UI 逻辑：
/// - 显示图标 / 名称 / 数量
/// - 支持拖拽操作（基础框架）
/// </summary>
public class ItemSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI引用")]
    public Image backgroundImage;           // 白色边框背景
    public Image itemIcon;                  // 图标
    public TextMeshProUGUI itemName;        // 名称
    public TextMeshProUGUI itemCount;       // 数量文本

    [Header("状态数据")]
    public ItemStack currentItem;           // 当前物品数据

    [Header("数据标识")]
    public InventoryType inventoryType;  // 属于哪个 SO
    public int slotIndex;                // 在列表中的索引

    private CanvasGroup canvasGroup;        // 拖拽时控制透明度
    private Transform originalParent;       // 拖拽前的父节点
    private static ItemSlotUI draggedSlot;  // 当前正在被拖拽的槽（静态，方便交互）
    private static GameObject dragIcon;     // 拖拽物体的悬浮图标（静态，方便交互）

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// 设置物品信息、容器类型与索引，并刷新显示
    /// </summary>
    public void SetItem(ItemStack itemStack, InventoryType type, int index)
    {
        inventoryType = type;
        slotIndex = index;
        currentItem = itemStack;

        if (itemStack == null || itemStack.item == null)
        {
            ClearSlot();
            return;
        }

        itemIcon.enabled = true;
        itemIcon.sprite = itemStack.item.icon;
        itemName.text = itemStack.item.itemName;
        itemCount.text = itemStack.count > 1 ? itemStack.count.ToString() : "";
    }

    /// <summary>
    /// 清空槽位显示
    /// </summary>
    public void ClearSlot()
    {
        currentItem = null;
        itemIcon.enabled = false;
        itemIcon.sprite = null;
        itemName.text = "";
        itemCount.text = "";
    }

    // ======================
    // 拖拽交互部分
    // ======================
    //开始拖拽
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null || currentItem.item == null)
            return;

        draggedSlot = this;
        originalParent = transform.parent;

        // 创建悬浮拖拽图标
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(UIManger.Instance.Root); // 挂在UI根节点
        var img = dragIcon.AddComponent<Image>();
        img.sprite = itemIcon.sprite;
        img.raycastTarget = false; // 避免挡住鼠标事件
        dragIcon.GetComponent<RectTransform>().sizeDelta = itemIcon.rectTransform.sizeDelta;

        canvasGroup.alpha = 0.6f; // 让被拖拽的槽变淡
    }
    //拖拽时
    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            dragIcon.transform.position = eventData.position;
        }
    }
    //拖拽结束
    public void OnEndDrag(PointerEventData eventData)
    {
        // 拖拽结束后销毁图标
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }

        canvasGroup.alpha = 1f;
        draggedSlot = null;
    }

    /// <summary>
    /// 当其他物品拖到此槽上方松开时触发
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        // 无效条件：没有拖拽源 或 拖拽目标是自己
        if (draggedSlot == null || draggedSlot == this)
            return;

        // ============= 获取双方的源与目标信息 =============
        var fromSlot = draggedSlot;
        var toSlot = this;

        var fromItem = fromSlot.currentItem;
        var toItem = toSlot.currentItem;

        var fromType = fromSlot.inventoryType;
        var toType = toSlot.inventoryType;

        var fromIndex = fromSlot.slotIndex;
        var toIndex = toSlot.slotIndex;

        var fromInventory = InventoryManager.Instance.GetInventory(fromType);
        var toInventory = InventoryManager.Instance.GetInventory(toType);

        // ============= 检查是否是相同物品（叠加逻辑） =============
        if (toItem != null && fromItem != null &&
            toItem.item == fromItem.item &&
            toItem.count < toItem.item.maxStack) // maxStack: 每种物品的堆叠上限
        {
            int canAdd = toItem.item.maxStack - toItem.count;
            int toAdd = Mathf.Min(canAdd, fromItem.count);

            // 修改 SO 中的真实数据
            var toData = toInventory.items[toIndex];
            var fromData = fromInventory.items[fromIndex];

            toItem.count += toAdd;
            fromItem.count -= toAdd;

            // 如果拖拽源物品移动后没有剩余，就清空
            if (fromItem.count <= 0)
            {
                fromInventory.items[fromIndex] = null;
                fromSlot.ClearSlot();
            }

            // 更新目标栏
            toSlot.SetItem(toItem, toType, toIndex);

            InventoryManager.Instance.OnInventoryChanged(fromType);
            InventoryManager.Instance.OnInventoryChanged(toType);
            return;
        }

        // ============= 否则进行普通交换 =============
        var temp = toInventory.items[toIndex];
        toInventory.items[toIndex] = fromInventory.items[fromIndex];
        fromInventory.items[fromIndex] = temp;

        toSlot.SetItem(toInventory.items[toIndex], toType, toIndex);
        fromSlot.SetItem(fromInventory.items[fromIndex], fromType, fromIndex);

        InventoryManager.Instance.OnInventoryChanged(fromType);
        InventoryManager.Instance.OnInventoryChanged(toType);
    }

    // ======================
    // 工具函数
    // ======================

    /// <summary>
    /// 判断该槽是否为空
    /// </summary>
    public bool IsEmpty()
    {
        return currentItem == null || currentItem.item == null;
    }

}
