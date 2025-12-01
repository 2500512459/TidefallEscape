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
    protected InventoryDataSO LootData;   // 每个宝箱自己的掉落数据
    // UI 是否当前可见（外部只读）
    private bool isUIVisible  = false;

    // 是否已被打开（避免重复开箱）
    protected bool opened = false;

    // 记录该宝箱遮罩动画已播放到的索引位置（下一次打开时从这里继续）
    // 初始为 -1 表示从未播放过，0 表示第0格已播放或正要播放
    public int lastPlayedMaskIndex = -1;

    protected override void Start()
    {
        base.Start();

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
    public virtual void TryOpen()
    {
        if (!PlayerInput.Instance.isLootOpen)
        {
            if (!opened)
                GenerateLootItems();

            // 将 InventoryManager 的 LootData 指向本宝箱的 LootData
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.LootData = LootData;
                InventoryManager.Instance.currenContext = InventoryContext.Looting;
                InventoryManager.Instance.OnInventoryChanged(InventoryType.Loot);
            }
            else
            {
                Debug.LogError("InventoryManager instance is null!");
            }

            // 临时修改 PlayerDataManager 的 context 为 Looting
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.currentContext = InventoryContext.Looting;
            }

            // 每次打开新宝箱时，清除旧的播放记录，确保动画能正常播放
            StorageItem.ClearPlayedMaskRecords();
            // 设置起始播放索引：只有大于此索引的格子才播放动画
            StorageItem.SetStartPlayIndex(lastPlayedMaskIndex);

            // 打开Loot界面
            InventoryUI.Instance?.ShowPanel();
        }
    }
    // ===================== 生成掉落物 =====================
    private void GenerateLootItems()
    {
        opened = true;

        if (QuestManager.Instance != null)
        {
            Debug.Log($"[TreasureBox] {name} 通知任务系统已开启宝箱。");
            QuestManager.Instance.UpdateQuestProgress("TreasureBox", 1);
        }

        if (lootContainerData == null)
        {
            Debug.LogWarning($"[TreasureBox] {name} 缺少 lootContainerData。");
            return;
        }
        // 生成掉落物列表
        int lootSlotCount = Random.Range(1, lootMaxSlotCount + 1);  // 生成掉落数量
        List<ItemStack> lootItems = lootContainerData.GenerateLoot(lootSlotCount, allowDuplicates);

        if (lootItems == null || lootItems.Count == 0)
        {
            Debug.Log($"[TreasureBox] {name} 未生成任何掉落物。");
            return;
        }        

        // 将掉落按顺序写入前 N 个格子
        for (int i = 0; i < lootItems.Count; i++)
        {
            if (i < LootData.items.Count)
            {
                LootData.items[i] = lootItems[i];
            }
            else
            {
                Debug.LogWarning($"[TreasureBox] 掉落物品数量超出容量限制，部分物品被丢弃。LootItems: {lootItems.Count}, Capacity: {LootData.items.Count}");
                break;
            }
        }
    }

}
