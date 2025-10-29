using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoSingleton<InventoryManager>
{
    [Header("��������")]
    public InventoryDataSO BackpackData;
    [Header("װ������")]
    public InventoryDataSO EquipmentData;
    [Header("�ֿ�����")]
    public InventoryDataSO StorageData;
    [Header("��ǰ��������ʱ����")]
    public InventoryDataSO LootData;                // ��ʱ����ʱ����

    [Header("��ǰ����")]
    public InventoryContext currenContext = InventoryContext.Home;

    public event Action<InventoryType> OnInventoryChangedEvent;

    [ContextMenu("������չ�������� +5")]
    private void TestExpand()
    {
        BackpackData.maxCount += 5;
        Debug.Log($"[InventoryDataSO] ����չ����: {BackpackData.maxCount}");
    }

    /// <summary>
    /// ���
    /// </summary>
    /// <param name="item"></param>
    /// <param name="count"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public bool AddItem(ItemDataSO item, int count, InventoryType type)
    {
        var inv = GetInventory(type);
        if (inv == null)
        {
            Debug.LogWarning($"[InventoryManager] AddItem ʧ�ܣ�{type} ��Ч��");
            return false;
        }

        bool result = inv.AddItem(item, count, type);
        OnInventoryChanged(type);
        return result;
    }
    // �������ͻ�ÿ�����
    public InventoryDataSO GetInventory(InventoryType type)
    {
        return type switch
        {
            InventoryType.Backpack => BackpackData,
            InventoryType.Equipment => EquipmentData,
            InventoryType.Storage => StorageData,
            InventoryType.Loot => LootData,
            _ => null
        };
    }
    // �ֿ����ݸ����¼��㲥
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
