using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 任务奖励领取按钮
public class GetRewardButton : MonoBehaviour
{
    public QuestDataSO questData;  // 任务数据
    [SerializeField] private CurrencyDataSO currencyData;

    /// <summary>
    /// 初始化按钮点击事件监听
    /// </summary>
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(GetReward);
        if (currencyData != null)
        {
            currencyData.EnsureLoaded();
        }
        else
        {
            Debug.LogWarning("[GetRewardButton] 未配置 CurrencyDataSO，奖励货币将不会被记录。", this);
        }
    }

    private void GetReward()
    {
        if (questData == null)
        {
            Debug.LogWarning("[GetRewardButton] QuestDataSO 缺失，无法结算奖励。", this);
            return;
        }

        var task = QuestManager.Instance.GetQuestTask(questData);
        if (task == null)
        {
            Debug.LogWarning($"[GetRewardButton] 未能在任务列表中找到 {questData.questName}。", this);
            return;
        }

        foreach (var item in questData.questRewards)
        {
            InventoryManager.Instance.AddItem(item.item, item.count, InventoryType.Storage);
        }

        if (currencyData != null)
        {
            if (questData.goldCoinReward > 0)
            {
                currencyData.AddGoldCoins(questData.goldCoinReward, false);
            }
            if (questData.gemstoneReward > 0)
            {
                currencyData.AddGemstones(questData.gemstoneReward, false);
            }
            currencyData.Broadcast();
        }
        else
        {
            Debug.LogWarning("[GetRewardButton] CurrencyDataSO 缺失，无法增加货币奖励。", this);
        }

        bool consumeSuccess = true;
        List<string> failedRequires = null;
        foreach (var require in questData.questRequires)
        {
            int availableCount = InventoryManager.Instance.GetQuestItemCount(require.name);
            if (availableCount <= 0)
            {
                continue;
            }

            if (!InventoryManager.Instance.ConsumeQuestItems(require.name, require.requiteAmount))
            {
                consumeSuccess = false;
                if (failedRequires == null)
                {
                    failedRequires = new List<string>();
                }
                failedRequires.Add(require.name);
            }
        }

        if (!consumeSuccess && failedRequires != null)
        {
            Debug.LogWarning($"[GetRewardButton] 消耗任务物品时未满足全部需求：{questData.questName} - {string.Join(", ", failedRequires)}", this);
        }

        foreach (var require in questData.questRequires)
        {
            require.currentAmount = require.requiteAmount;
        }
        questData.CheckQuestProgress();

        // 将当前任务标记为完成
        task.IsFinished = true;
        QuestManager.Instance.CompleteTaskList.Add(task);
        QuestManager.Instance.tasks.Remove(task);

        // 保存任务状态
        QuestManager.Instance.SaveState();

        // 刷新任务列表
        QuestUI.Instance.SetupQuestList();
    }

}
