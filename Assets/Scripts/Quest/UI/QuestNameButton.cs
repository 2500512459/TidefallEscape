using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 任务名称按钮UI组件，用于在任务列表中显示任务名称并响应点击事件
/// </summary>
public class QuestNameButton : MonoBehaviour
{
    public TextMeshProUGUI questNameText;   // 任务名称文本
    public QuestDataSO currentData;         // 关联的任务数据

    /// <summary>
    /// 初始化按钮点击事件监听
    /// </summary>
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(UpdataQuestContent);
    }

    /// <summary>
    /// 点击按钮时更新任务详情内容
    /// </summary>
    void UpdataQuestContent()
    {
        // 更新任务要求列表
        QuestUI.Instance.SetupRequireList(currentData);

        // 清空奖励列表
        foreach (Transform item in QuestUI.Instance.rewardTransform)
        {
            Destroy(item.gameObject);
        }

        // 根据任务奖励数据创建奖励项
        foreach (var reward in currentData.questRewards)
        {
            QuestUI.Instance.SetupRewardItem(reward.item, reward.count);
        }

        // 判断是否显示获取奖励按钮
        if (currentData != null && currentData.isCompleted)
        {
            QuestUI.Instance.getRewardButton.questData = currentData;
            QuestUI.Instance.getRewardButton.gameObject.SetActive(true);
        }
        else
        {
            QuestUI.Instance.getRewardButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 设置按钮显示的任务名称
    /// </summary>
    /// <param name="questData">任务数据</param>
    public void SetupNameButton(QuestDataSO questData)
    {
        currentData = questData;

        // 根据任务完成状态显示不同的名称
        if (questData.isCompleted)
        {
            questNameText.text = questData.questName + "(完成)";
        }
        else
        {
            questNameText.text = questData.questName;
        }
    }

    // 卸载点击事件
    public void DestroyListener()
    {
        GetComponent<Button>().onClick.RemoveAllListeners();
    }
}