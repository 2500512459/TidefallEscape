using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryDataSO", menuName = "Inventory/InventoryDataSO")]
public class InventoryDataSO : ScriptableObject
{
    public List<ItemStack> items = new List<ItemStack>();
    [Header("�������")]
    public InventoryType type;
    [Header("�������")]
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
    /// ��ָ�����������Ʒ���̶�����ģʽ��
    /// - �᳢�Ե�����ͬ��Ʒ
    /// - ����Ѱ�ҵ�һ���ո����
    /// </summary>
    public bool AddItem(ItemDataSO item, int count = 1, InventoryType type = InventoryType.Backpack)
    {
        // �� �ȵ��ӵ�������ͬ��Ʒ
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

        // �� �������ʣ�����������Է���ո���
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

        // �� ֪ͨ UI ����
        InventoryManager.Instance.OnInventoryChanged(type);

        // �� �����Ƿ���ȫ�ɹ�
        return count <= 0;
    }

    /// <summary>
    /// ��ָ��λ���Ƴ���Ʒ
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
    /// ����Ʒ��һ�������ƶ�����һ������
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
    /// ȷ�� items �б�����Ԥ��һ��
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
