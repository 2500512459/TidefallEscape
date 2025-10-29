using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

/// <summary>
/// ���䣨�̳� Character��
/// ���� ShowHint/HideHint �ӿڣ�ȷ�� TryOpen ʱ�ر���ʾ����������
/// </summary>
public class TreasureBox : Character
{
    [Header("��Ʒ���ɿ�")]
    public LootContainerSO lootContainerData;

    [Header("��ʾUI")]
    public TreasureHintUI HintUI;

    [Header("������������")]
    [Tooltip("���ɵ������Ʒ����")]
    public int lootMaxSlotCount = 5;
    [Tooltip("�Ƿ������ظ�����ͬһ��Ʒ")]
    public bool allowDuplicates = true;

    [Header("��ǰ�����������")]
    private InventoryDataSO LootData;   // ÿ�������Լ��ĵ����
    // UI �Ƿ�ǰ�ɼ����ⲿֻ����
    private bool isUIVisible  = false;

    // �Ƿ��ѱ��򿪣������ظ����䣩
    private bool opened = false;

    protected override void Start()
    {
        base.Start();
        if (HintUI != null)
            HintUI.Init(transform);

        // ȷ��ÿ�������ж���ʵ������ֹ������乲��ͬһ�� ScriptableObject��
        if (LootData == null)
            LootData = ScriptableObject.CreateInstance<InventoryDataSO>();
        else
            LootData = Instantiate(LootData);

        InitializeEmptySlots();
    }
    /// <summary>
    /// ��ʼ�� LootData �Ŀո���
    /// </summary>
    private void InitializeEmptySlots()
    {
        LootData.items ??= new List<ItemStack>();
        LootData.items.Clear();

        for (int i = 0; i < LootData.maxCount; i++)
        {
            LootData.items.Add(new ItemStack(null, 0)); // ����Ʒ��
        }
    }

    /// <summary>
    /// ��ʾ��ʾ UI���ⲿ���ã�
    /// </summary>
    public void ShowHint()
    {
        if (HintUI == null) return;
        if (isUIVisible) return;
        isUIVisible = true;
        HintUI.ShowUI();
    }

    /// <summary>
    /// ������ʾ UI���ⲿ���ã�
    /// </summary>
    public void HideHint()
    {
        if (HintUI == null) return;
        if (!isUIVisible) return;
        isUIVisible = false;
        HintUI.HideUI();
    }

    /// <summary>
    /// �򿪱���
    /// </summary>
    public void TryOpen()
    {
        if (!InputManager.Instance.isLootOpen)
        {
            if (!opened)
                GenerateLootItems();

            // �� InventoryManager �� LootData ָ�򱾱���� LootData
            InventoryManager.Instance.LootData = LootData;

            // ��Loot����
            InventoryManager.Instance.currenContext = InventoryContext.Looting;
            InventoryManager.Instance.OnInventoryChanged(InventoryType.Loot);
            UIManger.Instance.ShowPanel<InventoryPanel>();
        }
    }
    // ===================== ���ɵ����� =====================
    private void GenerateLootItems()
    {
        opened = true;

        if (lootContainerData == null)
        {
            Debug.LogWarning($"[TreasureBox] {name} ȱ�� lootContainerData��");
            return;
        }
        // ���ɵ������б�
        int lootSlotCount = Random.Range(1, lootMaxSlotCount + 1);
        List<ItemStack> lootItems = lootContainerData.GenerateLoot(lootSlotCount, allowDuplicates);

        if (lootItems == null || lootItems.Count == 0)
        {
            Debug.Log($"[TreasureBox] {name} δ�����κε����");
            return;
        }

        // �����䰴˳��д��ǰ N ������
        for (int i = 0; i < lootItems.Count; i++)
        {
            LootData.items[i] = lootItems[i];
        }
    }

}
