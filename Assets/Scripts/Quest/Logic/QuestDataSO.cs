using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 任务数据脚本对象，用于定义任务的基本信息和要求
/// </summary>
[CreateAssetMenu(fileName = "QuestDataSO", menuName = "Quest/QuestDataSO")]
public class QuestDataSO : ScriptableObject
{
    /// <summary>
    /// 任务要求数据结构，定义任务需要完成的内容
    /// </summary>
    [System.Serializable]
    public class QuestRequire
    {
        public string name;             // 要求的名称（如怪物名、物品名等）
        public int requiteAmount;       // 需要完成的数量
        public int currentAmount;       // 当前已完成的数量
    }

    public string questName;            // 任务名称
    [TextArea]
    public string description;          // 任务描述

    public bool isStarted;              // 任务是否已开始
    public bool isCompleted;            // 任务是否已完成（满足所有要求）
    public bool isFinished;             // 任务是否已结束（已领取奖励）

    public List<QuestRequire> questRequires = new List<QuestRequire>();   // 任务要求列表
    public List<ItemStack> questRewards = new List<ItemStack>();          // 任务奖励列表
    [Header("金钱奖励")]
    public int goldCoinReward;                                            // 金币奖励数量
    public int gemstoneReward;                                            // 宝石奖励数量

    /// <summary>
    /// 检查任务进度，判断任务是否已完成
    /// </summary>
    public void CheckQuestProgress()
    {
        // 筛选出已完成的要求（当前数量>=需要数量）
        var finishRequires = questRequires.Where(r => r.requiteAmount <= r.currentAmount);
        // 当所有要求都完成时，任务状态设为完成
        isCompleted = finishRequires.Count() == questRequires.Count;
    }

    // 当前任务需要搜集/消灭的名称列表
    public List<string> GetQuestRequireNames()
    {
        List<string> targetNameList = new List<string>();

        foreach (var require in questRequires)
        {
            targetNameList.Add(require.name);
        }
        return targetNameList;
    }
}