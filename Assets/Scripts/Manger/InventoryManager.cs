using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoSingleton<InventoryManager>
{
    [Header("��������")]
    public InventoryDataSO BackpackData;
    [Header("װ������")]
    public InventoryDataSO EquipmentData;
    [Header("�ֿ�����")]
    public InventoryDataSO StorageData;

    public event Action<InventoryType> OnInventoryChangedEvent;

    [ContextMenu("������չ�������� +5")]
    private void TestExpand()
    {
        BackpackData.maxCount += 5;
        Debug.Log($"[InventoryDataSO] ����չ����: {BackpackData.maxCount}");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <param name="count"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public bool AddItem(ItemDataSO item, int count, InventoryType type)
    {
        var inv = GetInventory(type);
        return inv.AddItem(item, count, type);
    }
    // �������ͻ�ÿ�����
    public InventoryDataSO GetInventory(InventoryType type)
    {
        return type switch
        {
            InventoryType.Backpack => BackpackData,
            InventoryType.Equipment => EquipmentData,
            InventoryType.Storage => StorageData,
            _ => null
        };
    }
    public void OnInventoryChanged(InventoryType type)
    {
        OnInventoryChangedEvent?.Invoke(type);
        // ��ѡ������ UI ˢ�»򱣴��¼�
        Debug.Log($"{type} �����Ѹ���");
    }

    /// <summary>
    /// ������
    /// </summary>
    /// <param name="type"></param>
    public void SortInventory(InventoryType type)
    {
        var data = GetInventory(type);
        if (data == null) return;

        // ȥ���ո���ǰ�ȸ���һ��
        var items = new List<ItemStack>(data.items);

        // ����
        items.Sort(new ItemComparer());

        // ���¸�ֵ
        data.items.Clear();
        data.items.AddRange(items);

        // ֪ͨ����
        OnInventoryChanged(type);
    }
}
