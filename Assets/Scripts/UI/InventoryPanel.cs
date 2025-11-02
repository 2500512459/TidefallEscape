using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ������壺����װ�������������ֿ���������
/// </summary>
public class InventoryPanel : UIPanelBase
{
    // [Header("����UI�ڵ�")]
    // public Transform equipmentGridRoot;     // װ������2x5��
    // public Transform backpackContent;       // ���� ScrollView ���ݽڵ�
    // public Transform storageContent;        // �ֿ� ScrollView ���ݽڵ�
    // public Transform lootGridRoot;          // ս��Ʒ

    [Header("Panel�ڵ�")]
    public Transform LootGridText;
    public Transform LootGrid;
    public Transform InfoNodeRoot;
    public Transform RightPanel;

    // [Header("��Ʒ��Ԥ����")]
    // public GameObject itemSlotPrefab;

    // private List<ItemSlot> equipmentSlots = new List<ItemSlot>();
    // private List<ItemSlot> backpackSlots = new List<ItemSlot>();
    // private List<ItemSlot> storageSlots = new List<ItemSlot>();
    // private List<ItemSlot> lootSlots = new List<ItemSlot>();


    public override void OnInit()
    {
        base.OnInit();
        //InitSlots();

        //�����¼������������ݱ仯ʱ�Զ�ˢ��
        // InventoryManager.Instance.OnInventoryChangedEvent += OnInventoryUpdated;
    }
    public override void OnClose()
    {
        base.OnClose();
        // if (InventoryManager.Instance != null)
        //     InventoryManager.Instance.OnInventoryChangedEvent -= OnInventoryUpdated;
    }
    /// <summary>
    /// ��ʼ������
    /// </summary>
    // private void InitSlots()
    // {
    //     var inv = InventoryManager.Instance;

    //     // ȷ��SO��UI��������һ��
    //     inv.EquipmentData.EnsureSlotCount(10);
    //     // inv.BackpackData.EnsureSlotCount(inv.BackpackData.maxCount);
    //     // inv.StorageData.EnsureSlotCount(inv.StorageData.maxCount);

    //     // ��ʼ��װ�������̶� 2x5 = 10��
    //     CreateEmptySlots(equipmentSlots, equipmentGridRoot, 10);

    //     // ��ʼ������Ĭ�ϸ���
    //     //CreateEmptySlots(backpackSlots, backpackContent, inv.BackpackData.maxCount);

    //     // ��ʼ���ֿ�Ĭ�ϸ���
    //     //CreateEmptySlots(storageSlots, storageContent, inv.StorageData.maxCount);
    // }

    public override void OnShow()
    {
        base.OnShow();
        //RefreshAll();

        // ���ݳ���������ʾ
        var ctx = InventoryManager.Instance.currenContext;
        bool isHome = ctx == InventoryContext.Home;
        bool isLooting = ctx == InventoryContext.Looting;

        RightPanel.gameObject.SetActive(isHome);

        LootGridText.gameObject.SetActive(isLooting);
        LootGrid.gameObject.SetActive(isLooting);

        InfoNodeRoot.gameObject.SetActive(false);
    }

    // /// <summary>
    // /// ˢ������UI����
    // /// ÿ�ζ��� InventoryManager �� SO �����¼�����������
    // /// </summary>
    // public void RefreshAll()
    // {
    //     // ȷ������ʵʱ����
    //     var inv = InventoryManager.Instance;
    //     if (inv == null)
    //     {
    //         Debug.LogError("[InventoryPanel] InventoryManager.Instance Ϊ null");
    //         return;
    //     }

    //     RefreshEquipment(inv.EquipmentData);
    //     RefreshBackpack(inv.BackpackData);
    //     RefreshStorage(inv.StorageData);
    //     RefreshLoot(inv.LootData);
    // }

    // /// <summary>
    // /// ˢ��װ����
    // /// </summary>
    // private void RefreshEquipment(InventoryDataSO equipmentData)
    // {
    //     var data = equipmentData != null ? equipmentData.items : new List<ItemStack>();

    //     for (int i = 0; i < equipmentSlots.Count; i++)
    //     {
    //         if (i < data.Count)
    //             equipmentSlots[i].SetItem(data[i], InventoryType.Equipment, i);
    //         else
    //             equipmentSlots[i].ClearSlot();
    //     }
    // }

    // /// <summary>
    // /// ˢ�±��������ɶ�̬������
    // /// </summary>
    // private void RefreshBackpack(InventoryDataSO backpackData)
    // {
    //     var data = backpackData != null ? backpackData.items : new List<ItemStack>();

    //     EnsureSlotCount(backpackSlots, backpackContent, data.Count);

    //     for (int i = 0; i < backpackSlots.Count; i++)
    //     {
    //         if (i < data.Count)
    //             backpackSlots[i].SetItem(data[i], InventoryType.Backpack, i);
    //         else
    //             backpackSlots[i].ClearSlot();
    //     }
    // }

    // // <summary>
    // // ˢ�²ֿ������ɶ�̬������
    // // </summary>
    // private void RefreshStorage(InventoryDataSO storageData)
    // {
    //    var data = storageData != null ? storageData.items : new List<ItemStack>();

    //    EnsureSlotCount(storageSlots, storageContent, data.Count);

    //    for (int i = 0; i < storageSlots.Count; i++)
    //    {
    //        if (i < data.Count)
    //            storageSlots[i].SetItem(data[i], InventoryType.Storage, i);
    //        else
    //            storageSlots[i].ClearSlot();
    //    }
    // }
    // /// <summary>
    // /// ˢ�µ�����
    // /// </summary>
    // private void RefreshLoot(InventoryDataSO data)
    // {
    //     EnsureSlotCount(lootSlots, lootGridRoot, data.items.Count);
    //     for (int i = 0; i < lootSlots.Count; i++)
    //     {
    //         if (i < data.items.Count)
    //             lootSlots[i].SetItem(data.items[i], InventoryType.Loot, i);
    //         else
    //             lootSlots[i].ClearSlot();
    //     }
    // }
    // /// <summary>
    // /// ��ʼ��ʱ�����ո���
    // /// </summary>
    // private void CreateEmptySlots(List<ItemSlot> list, Transform parent, int count)
    // {
    //     for (int i = 0; i < count; i++)
    //     {
    //         var slot = Instantiate(itemSlotPrefab, parent).GetComponent<ItemSlot>();
    //         slot.ClearSlot(); // Ĭ����ʾ�������ױߣ�
    //         list.Add(slot);
    //     }
    // }

    // /// <summary>
    // /// ȷ��Slot�������ã�������̬����
    // /// </summary>
    // private void EnsureSlotCount(List<ItemSlot> list, Transform parent, int targetCount)
    // {
    //     while (list.Count < targetCount)
    //     {
    //         var slot = Instantiate(itemSlotPrefab, parent).GetComponent<ItemSlot>();
    //         list.Add(slot);
    //         slot.ClearSlot(); // Ĭ����ʾ����
    //     }
    // }
    // /// <summary>
    // /// ������������¼�
    // /// </summary>
    // /// <param name="type"></param>
    // private void OnInventoryUpdated(InventoryType type)
    // {
    //     var inv = InventoryManager.Instance;
    //     switch (type)
    //     {
    //         case InventoryType.Equipment:
    //             RefreshEquipment(inv.EquipmentData); break;
    //         case InventoryType.Backpack:
    //             RefreshBackpack(inv.BackpackData); break;
    //         case InventoryType.Storage:
    //             RefreshStorage(inv.StorageData);
    //             break;
    //         case InventoryType.Loot:
    //             RefreshLoot(inv.LootData); break;
    //     }
    // }


    // public void OnSortBackpackButtonClicked()
    // {
    //     InventoryManager.Instance.BackpackData.SortItems();
    //     InventoryManager.Instance.OnInventoryChanged(InventoryType.Backpack);
    // }
    // public void OnSortStorageButtonClicked()
    // {
    //     InventoryManager.Instance.StorageData.SortItems();
    //     InventoryManager.Instance.OnInventoryChanged(InventoryType.Storage);
    // }

}
