using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 物品拖拽逻辑组件
/// - 与 StorageItem 一起使用（同物体上）
/// - 负责处理拖拽、交换、叠加、放置等逻辑
/// </summary>
[RequireComponent(typeof(StorageItem))]
public class DragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private StorageItem slot;                  // 所属 StorageItem 引用
    private CanvasGroup canvasGroup;        // 控制拖拽时透明度
    private Transform originalParent;       // 拖拽前的父节点

    private static DragItem draggedItem;    // 当前正在被拖拽的 DragItem（静态，方便全局访问）
    private static GameObject dragIcon;     // 拖拽中跟随鼠标的图标对象

    private void Awake()
    {
        slot = GetComponent<StorageItem>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// 开始拖拽
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 如果当前为商店界面，则禁止拖拽
        var shopPanel = UIManger.Instance.GetPanel<ShopPanel>();
        if (shopPanel != null && shopPanel.IsVisible)
            return;

        // 无效物品禁止拖拽
        if (slot.currentItem == null || slot.currentItem.item == null)
            return;

        draggedItem = this;
        originalParent = transform.parent;

        // 创建一个临时拖拽图标
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(UIManger.Instance.InventoryCanvas); // 放在UI根节点
        var img = dragIcon.AddComponent<Image>();
        img.sprite = slot.itemIcon.sprite;
        img.raycastTarget = false; // 防止挡住UI交互
        dragIcon.GetComponent<RectTransform>().sizeDelta = slot.itemIcon.rectTransform.sizeDelta;

        // 拖拽中的格子半透明
        canvasGroup.alpha = 0.6f;
    }

    /// <summary>
    /// 拖拽进行中
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        var shopPanel = UIManger.Instance.GetPanel<ShopPanel>();
        if (shopPanel != null && shopPanel.IsVisible)
            return;

        if (dragIcon != null)
            dragIcon.transform.position = eventData.position;
    }

    /// <summary>
    /// 结束拖拽
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        var shopPanel = UIManger.Instance.GetPanel<ShopPanel>();
        if (shopPanel != null && shopPanel.IsVisible)
            return;

        // 销毁临时图标
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }

        // 恢复透明度
        canvasGroup.alpha = 1f;
        draggedItem = null;
    }

    /// <summary>
    /// 当拖拽物体在另一个物品格上方松开时触发
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        var shopPanel = UIManger.Instance.GetPanel<ShopPanel>();
        if (shopPanel != null && shopPanel.IsVisible)
            return;

        // 无效条件：没有拖拽源或拖放到自己身上
        if (draggedItem == null || draggedItem == this)
            return;

        // 获取来源与目标格子
        var fromSlot = draggedItem.slot;
        var toSlot = slot;

        var fromItem = fromSlot.currentItem;
        var toItem = toSlot.currentItem;

        var fromItemName = fromItem == null ? "" : fromItem.item.name;
        var ToItemName = toItem == null ? "" : toItem.item.name;

        var fromType = fromSlot.inventoryType;
        var toType = toSlot.inventoryType;

        var fromIndex = fromSlot.slotIndex;
        var toIndex = toSlot.slotIndex;

        var fromInventory = InventoryManager.Instance.GetInventory(fromType);
        var toInventory = InventoryManager.Instance.GetInventory(toType);

        var InLootType = fromType == InventoryType.Loot || toType == InventoryType.Loot;
        // ==================== 叠加逻辑 ====================
        if (toItem != null && fromItem != null &&
            toItem.item == fromItem.item &&
            toItem.count < toItem.item.maxStack)
        {
            // 计算可以叠加的数量
            int canAdd = toItem.item.maxStack - toItem.count;
            int toAdd = Mathf.Min(canAdd, fromItem.count);

            // 更新数量
            toItem.count += toAdd;
            fromItem.count -= toAdd;

            // 来源用尽则清空
            if (fromItem.count <= 0)
            {
                fromInventory.items[fromIndex] = null;
                fromSlot.ClearSlot();
            }
            else
            {
                // 刷新来源格子显示
                fromSlot.SetItem(fromItem, fromType, fromIndex);
            }

            // 刷新目标格子显示
            toSlot.SetItem(toItem, toType, toIndex);

            InventoryManager.Instance.OnInventoryChanged(fromType);
            InventoryManager.Instance.OnInventoryChanged(toType);

            // 检查任务进度
            // 如果是将某一个物品拖拽到背包或装备栏，就增加fromItemName的的任务进度
            if (InLootType)
            {
                if (toType == InventoryType.Backpack || toType == InventoryType.Equipment)
                {
                    QuestManager.Instance.UpdateQuestProgress(fromItemName, toAdd);
                }
                else if (toType == InventoryType.Loot)
                {
                    QuestManager.Instance.UpdateQuestProgress(fromItemName, -toAdd);
                }
            }

            return;
        }

        // ==================== 普通交换逻辑 ====================

        // 检查任务进度
        // 如果是将某一个物品拖拽到背包或装备栏，就增加fromItemName的的任务进度
        if (InLootType)
        {
            if (fromType == InventoryType.Loot)
            {
                if (fromItemName != "")
                {
                    QuestManager.Instance.UpdateQuestProgress(fromItemName, fromItem.count);
                }
                if (ToItemName != "")
                {
                    QuestManager.Instance.UpdateQuestProgress(ToItemName, -toItem.count);
                }
            }

            if (toType == InventoryType.Loot)
            {
                if (fromItemName != "")
                {
                    QuestManager.Instance.UpdateQuestProgress(fromItemName, -fromItem.count);
                }
                if (ToItemName != "")
                {
                    QuestManager.Instance.UpdateQuestProgress(ToItemName, toItem.count);
                }
            }
        }
        
        var temp = toInventory.items[toIndex];
        toInventory.items[toIndex] = fromInventory.items[fromIndex];
        fromInventory.items[fromIndex] = temp;

        // 刷新两边显示
        toSlot.SetItem(toInventory.items[toIndex], toType, toIndex);
        fromSlot.SetItem(fromInventory.items[fromIndex], fromType, fromIndex);

        // 通知数据更新
        InventoryManager.Instance.OnInventoryChanged(fromType);
        InventoryManager.Instance.OnInventoryChanged(toType);


    }
}
