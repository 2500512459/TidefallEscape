using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

/// <summary>
/// 宝箱（继承 Character）
/// 增加 ShowHint/HideHint 接口，确保 TryOpen 时关闭提示并填充掉落栏
/// </summary>
public class TreasureBox : Character
{
    [Header("物品生成库")]
    public LootContainerSO lootContainerData;

    [Header("提示UI")]
    public TreasureHintUI HintUI;

    [Header("掉落数量设置")]
    [Tooltip("生成的最大物品数量")]
    public int lootMaxSlotCount = 5;
    [Tooltip("是否允许重复掉落同一物品")]
    public bool allowDuplicates = true;

    [Header("当前宝箱掉落数据")]
    private InventoryDataSO LootData;   // 每个宝箱自己的掉落表
    // UI 是否当前可见（外部只读）
    private bool isUIVisible  = false;

    // 是否已被打开（避免重复开箱）
    private bool opened = false;

    protected override void Start()
    {
        base.Start();
        if (HintUI != null)
            HintUI.Init(transform);

        // 确保每个宝箱有独立实例（防止多个宝箱共享同一个 ScriptableObject）
        if (LootData == null)
            LootData = ScriptableObject.CreateInstance<InventoryDataSO>();
        else
            LootData = Instantiate(LootData);

        InitializeEmptySlots();
    }
    /// <summary>
    /// 初始化 LootData 的空格子
    /// </summary>
    private void InitializeEmptySlots()
    {
        LootData.items ??= new List<ItemStack>();
        LootData.items.Clear();

        for (int i = 0; i < LootData.maxCount; i++)
        {
            LootData.items.Add(new ItemStack(null, 0)); // 空物品槽
        }
    }

    /// <summary>
    /// 显示提示 UI（外部调用）
    /// </summary>
    public void ShowHint()
    {
        if (HintUI == null) return;
        if (isUIVisible) return;
        isUIVisible = true;
        HintUI.ShowUI();
    }

    /// <summary>
    /// 隐藏提示 UI（外部调用）
    /// </summary>
    public void HideHint()
    {
        if (HintUI == null) return;
        if (!isUIVisible) return;
        isUIVisible = false;
        HintUI.HideUI();
    }

    /// <summary>
    /// 打开宝箱
    /// </summary>
    public void TryOpen()
    {
        if (!InputManager.Instance.isLootOpen)
        {
            if (!opened)
                GenerateLootItems();

            // 将 InventoryManager 的 LootData 指向本宝箱的 LootData
            InventoryManager.Instance.LootData = LootData;

            // 打开Loot界面
            InventoryManager.Instance.currenContext = InventoryContext.Looting;
            InventoryManager.Instance.OnInventoryChanged(InventoryType.Loot);
            UIManger.Instance.ShowPanel<InventoryPanel>();
        }
    }
    // ===================== 生成掉落物 =====================
    private void GenerateLootItems()
    {
        opened = true;

        if (lootContainerData == null)
        {
            Debug.LogWarning($"[TreasureBox] {name} 缺少 lootContainerData。");
            return;
        }
        // 生成掉落物列表
        int lootSlotCount = Random.Range(1, lootMaxSlotCount + 1);
        List<ItemStack> lootItems = lootContainerData.GenerateLoot(lootSlotCount, allowDuplicates);

        if (lootItems == null || lootItems.Count == 0)
        {
            Debug.Log($"[TreasureBox] {name} 未生成任何掉落物。");
            return;
        }

        // 将掉落按顺序写入前 N 个格子
        for (int i = 0; i < lootItems.Count; i++)
        {
            LootData.items[i] = lootItems[i];
        }
    }

}
