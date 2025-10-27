using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryDataSO", menuName = "Inventory/InventoryDataSO")]
public class InventoryDataSO : ScriptableObject
{
    public List<ItemStack> items = new List<ItemStack>();
    [Header("库的类型")]
    public InventoryType type;
    [Header("库的容量")]
    [SerializeField] private int _maxCount = 10;
    public int maxCount
    {
        get => _maxCount;
        set
        {
            if (value != _maxCount)
            {
                _maxCount = value;
                EnsureSlotCount(_maxCount);
                InventoryManager.Instance.OnInventoryChanged(type);
            }
        }
    }
    /// <summary>
    /// 向指定背包添加物品（固定格子模式）
    /// - 会尝试叠加相同物品
    /// - 否则寻找第一个空格放入
    /// </summary>
    public bool AddItem(ItemDataSO item, int count = 1, InventoryType type = InventoryType.Backpack)
    {
        // ① 先叠加到已有相同物品
        for (int i = 0; i < items.Count && count > 0; i++)
        {
            var stack = items[i];
            if (stack != null && stack.item == item && stack.count < item.maxStack)
            {
                int space = item.maxStack - stack.count;
                int toAdd = Mathf.Min(space, count);
                stack.count += toAdd;
                count -= toAdd;
            }
        }

        // ② 如果还有剩余数量，尝试放入空格子
        for (int i = 0; i < items.Count && count > 0; i++)
        {
            var stack = items[i];
            if (stack == null || stack.item == null)
            {
                int toAdd = Mathf.Min(count, item.maxStack);
                items[i] = new ItemStack(item, toAdd);
                count -= toAdd;
            }
        }

        // ③ 通知 UI 更新
        InventoryManager.Instance.OnInventoryChanged(type);

        // ④ 返回是否完全成功
        return count <= 0;
    }

    /// <summary>
    /// 从指定位置移除物品
    /// </summary>
    public void RemoveItem(int index, int count, InventoryType type)
    {
        if (index < 0 || index >= items.Count) return;

        var stack = items[index];
        stack.count -= count;

        if (stack.count <= 0)
            items.RemoveAt(index);

        InventoryManager.Instance.OnInventoryChanged(type);
    }

    /// <summary>
    /// 将物品从一个容器移动到另一个容器
    /// </summary>
    public void MoveItem(int fromIndex, InventoryDataSO targetInventory, InventoryType fromType, InventoryType toType)
    {
        if (fromIndex < 0 || fromIndex >= items.Count) return;
        if (targetInventory == null) return;

        var movingItem = items[fromIndex];
        if (movingItem == null || movingItem.item == null) return;

        targetInventory.AddItem(movingItem.item, movingItem.count, toType);
        items.RemoveAt(fromIndex);

        InventoryManager.Instance.OnInventoryChanged(fromType);
        InventoryManager.Instance.OnInventoryChanged(toType);
    }

    /// <summary>
    /// 确保 items 列表长度与预期一致
    /// </summary>
    public void EnsureSlotCount(int targetCount)
    {
        while (items.Count < targetCount)
            items.Add(null);

        while (items.Count > targetCount)
            items.RemoveAt(items.Count - 1);
    }
}

[System.Serializable]
public class ItemStack
{
    public ItemDataSO item;
    public int count;

    public ItemStack(ItemDataSO item, int count)
    {
        this.item = item;
        this.count = count;
    }
}
