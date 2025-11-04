using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GetRewardButton : MonoBehaviour
{
    public QuestDataSO questData;  // 任务数据

    /// <summary>
    /// 初始化按钮点击事件监听
    /// </summary>
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(GetReward);
    }

    private void GetReward()
    {
        foreach (var item in questData.questRewards)
        {
            InventoryManager.Instance.StorageData.AddItem(item.item, item.count, InventoryType.Storage);
        }

        // 将当前任务标记为完成
        var task = QuestManager.Instance.GetQuestTask(questData);
        task.IsFinished = true;
        QuestManager.Instance.CompleteTaskList.Add(task);
        QuestManager.Instance.tasks.Remove(task);

        // 刷新任务列表
        QuestUI.Instance.SetupQuestList();
    }

}
