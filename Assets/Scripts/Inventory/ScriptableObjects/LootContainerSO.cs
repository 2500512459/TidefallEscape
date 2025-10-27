using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LootContainerSO", menuName = "Inventory/LootContainerSO")]
public class LootContainerSO : ScriptableObject
{
    [Header("按稀有度分类的掉落条目")]
    public List<LootEntry> commonEntries = new List<LootEntry>();
    public List<LootEntry> rareEntries = new List<LootEntry>();
    public List<LootEntry> epicEntries = new List<LootEntry>();
    public List<LootEntry> legendaryEntries = new List<LootEntry>();

    [Header("稀有度概率（默认总和=1）")]
    [Tooltip("常见 概率 (默认 0.80)")]
    [Range(0f, 1f)] public float commonChance = 0.80f;
    [Tooltip("罕见 概率 (默认 0.14)")]
    [Range(0f, 1f)] public float rareChance = 0.14f;
    [Tooltip("史诗 概率 (默认 0.05)")]
    [Range(0f, 1f)] public float epicChance = 0.05f;
    [Tooltip("传奇 概率 (默认 0.01)")]
    [Range(0f, 1f)] public float legendaryChance = 0.01f;

    // ================================================================
    //  一、稀有度随机选择
    // ================================================================
    /// <summary>
    /// 根据当前设置的概率随机返回一个 ItemRarity。
    /// 如果四个概率总和不等于 1，会按权重自动归一化。
    /// </summary>
    public ItemRarity PickRarity()
    {
        // 汇总并允许总和 != 1（进行归一化）
        float c = Mathf.Max(0f, commonChance);
        float r = Mathf.Max(0f, rareChance);
        float e = Mathf.Max(0f, epicChance);
        float l = Mathf.Max(0f, legendaryChance);

        float total = c + r + e + l;
        if (total <= 0f)
        {
            // 万一都设成0，退化为常见
            return ItemRarity.Common;
        }

        float roll = Random.value * total; // [0, total)
        float accum = 0f;

        accum += c;
        if (roll < accum) return ItemRarity.Common;

        accum += r;
        if (roll < accum) return ItemRarity.Rare;

        accum += e;
        if (roll < accum) return ItemRarity.Epic;

        // 剩下即为传奇（或当作默认）
        return ItemRarity.Legendary;
    }

    /// <summary>
    /// 根据稀有度返回对应的条目列表引用（可能为空或长度为0）
    /// </summary>
    public List<LootEntry> GetListByRarity(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return commonEntries;
            case ItemRarity.Rare: return rareEntries;
            case ItemRarity.Epic: return epicEntries;
            case ItemRarity.Legendary: return legendaryEntries;
            default: return commonEntries;
        }
    }
    // ================================================================
    //  二、从稀有度列表中按概率 + 权重抽取一个条目
    // ================================================================
    /// <summary>
    /// 从指定稀有度的列表中，根据 probability + weight 抽取一个物品条目
    /// </summary>
    public LootEntry PickEntryFromRarity(ItemRarity rarity)
    {
        List<LootEntry> list = GetListByRarity(rarity);
        if (list == null || list.Count == 0)
        {
            Debug.LogWarning($"[LootContainerSO] 稀有度 {rarity} 没有可用条目！");
            return null;
        }

        // 概率筛选（根据概率加入候选名单）
        List<LootEntry> filtered = new List<LootEntry>();
        foreach (var entry in list)
        {
            if (entry.item == null) continue;
            if (Random.value <= entry.probability)
                filtered.Add(entry);
        }

        if (filtered.Count == 0)
            return null;

        // 权重加权随机
        int totalWeight = 0;
        foreach (var e in filtered)
            totalWeight += Mathf.Max(1, e.weight);

        int roll = Random.Range(0, totalWeight);
        int accum = 0;
        foreach (var e in filtered)
        {
            accum += Mathf.Max(1, e.weight);
            if (roll < accum)
                return e;
        }

        // 随机失败，从候选名单中拿取
        return filtered[filtered.Count - 1];
    }
    // ================================================================
    //  三、完整掉落逻辑
    // ================================================================
    /// <summary>
    /// 抽取指定数量的物品,数量+是否去重
    /// </summary>
    /// <param name="slotCount"></param>
    /// <param name="allowDuplicates"></param>
    /// <returns></returns>
    public List<ItemStack> GenerateLoot(int slotCount, bool allowDuplicates)
    {
        List<ItemStack> results = new List<ItemStack>();
        HashSet<string> pickedItemIDs = new HashSet<string>(); // 用于去重

        for (int i = 0; i < slotCount; i++)
        {
            // 1 随机稀有度
            ItemRarity rarity = PickRarity();

            // 2 从该稀有度表中抽物品
            LootEntry entry = PickEntryFromRarity(rarity);
            if (entry == null) continue;

            // 3 如果不允许重复，跳过已抽过的物品
            if (!allowDuplicates && pickedItemIDs.Contains(entry.item.itemID))
                continue;

            // 4 确定数量并添加
            int amount = entry.GetRandomAmount();
            results.Add(new ItemStack(entry.item, amount));

            pickedItemIDs.Add(entry.item.itemID);
        }

        return results;
    }
    // ================================================================
    //  四、测试接口
    // ================================================================
    /// <summary>
    /// 编辑器右键测试：多次抽取稀有度并输出分布（用于校验概率）
    /// </summary>
    [ContextMenu("测试稀有度采样(10000 次)")]
    private void TestPickRaritySample()
    {
        int runs = 10000;
        int c = 0, r = 0, e = 0, l = 0;
        for (int i = 0; i < runs; i++)
        {
            var pick = PickRarity();
            switch (pick)
            {
                case ItemRarity.Common: c++; break;
                case ItemRarity.Rare: r++; break;
                case ItemRarity.Epic: e++; break;
                case ItemRarity.Legendary: l++; break;
            }
        }

        Debug.Log($"[LootContainerSO] 抽样 {runs} 次结果：Common={c} ({(c / (float)runs * 100f):F2}%)  Rare={r} ({(r / (float)runs * 100f):F2}%)  Epic={e} ({(e / (float)runs * 100f):F2}%)  Legendary={l} ({(l / (float)runs * 100f):F2}%)");
    }
    /// <summary>
    /// 编辑器中右键测试：随机选稀有度 + 从中抽100个条目
    /// </summary>
    [ContextMenu("测试稀有度物品抽取100次")]
    private void TestPickEntry()
    {
        int runs = 100;
        for (int i = 0; i < runs; i++)
        {
            ItemRarity rarity = PickRarity();
            LootEntry entry = PickEntryFromRarity(rarity);

            if (entry != null)
            {
                int amount = entry.GetRandomAmount();
                Debug.Log($"[LootContainerSO] 掉落结果：稀有度={rarity}，物品={entry.item.itemName} × {amount}");
            }
            else
            {
                Debug.Log($"[LootContainerSO] 掉落结果：稀有度={rarity}，但无可选物品。");
            }
        }
    }
    /// <summary>
    /// 编辑器中右键测试：随机生成3项的物品
    /// </summary>
    [ContextMenu("测试完整掉落生成（3槽）")]
    private void TestGenerateLoot()
    {
        var loot = GenerateLoot(3, true);
        if (loot.Count == 0)
        {
            Debug.Log("[LootContainerSO] 没有生成任何物品");
            return;
        }

        foreach (var stack in loot)
        {
            Debug.Log($" - [{stack.item.rarity}] {stack.item.itemName} × {stack.count}");
        }
    }
}

[System.Serializable]
public class LootEntry
{
    [Tooltip("引用的物品")]
    public ItemDataSO item;

    [Tooltip("最小掉落数量（包含）")]
    public int minAmount = 1;
    [Tooltip("最大掉落数量（包含）")]
    public int maxAmount = 1;
    [Tooltip("该条目加入掉落候选名单的概率（0-1）")]
    [Range(0f, 1f)]
    public float probability = 1f;
    [Tooltip("权重（用于在同稀有度中加权选择）")]
    public int weight = 1;
    public int GetRandomAmount()
    {
        if (maxAmount <= minAmount) return minAmount;
        return Random.Range(minAmount, maxAmount + 1);
    }
}
