using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 任务发布者，负责与玩家交互并提供任务
/// 需要配合对话控制器使用
/// </summary>
[RequireComponent(typeof(DialogueController))]
public class QuestGiver : MonoBehaviour
{
    DialogueController controller;      // 对话控制器引用
    QuestDataSO currentQuest;           // 当前关联的任务数据

    // 不同任务状态对应的对话数据
    public DialogueDataSO startDialogue;        // 初始对话
    public DialogueDataSO progressDialogue;     // 进行中对话
    public DialogueDataSO completeDialogue;     // 完成对话
    public DialogueDataSO finishDialogue;       // 结束对话

#region 任务状态
    /// <summary>
    /// 任务是否已开始
    /// </summary>
    public bool IsStarted
    {
        get
        {
            // 检查任务管理器中是否存在该任务，并返回其开始状态
            if (QuestManager.Instance.HaveQuest(currentQuest))
            {
                return QuestManager.Instance.GetQuestTask(currentQuest).IsStarted;
            }
            else
                return false;
        }
    }
    
    /// <summary>
    /// 任务是否已完成（满足所有要求但未领取奖励）
    /// </summary>
    public bool IsCompleted
    {
        get
        {
            // 检查任务管理器中是否存在该任务，并返回其完成状态
            if (QuestManager.Instance.HaveQuest(currentQuest))
            {
                return QuestManager.Instance.GetQuestTask(currentQuest).IsCompleted;
            }
            else
                return false;
        }
    }
    
    /// <summary>
    /// 任务是否已结束（已领取奖励）
    /// </summary>
    public bool IsFinished
    {
        get
        {
            // 检查任务管理器中是否存在该任务，并返回其结束状态
            if (QuestManager.Instance.HaveQuest(currentQuest))
            {
                return QuestManager.Instance.GetQuestTask(currentQuest).IsFinished;
            }
            else
                return false;
        }
    }
    #endregion
    
    /// <summary>
    /// 初始化组件引用
    /// </summary>
    void Awake()
    {
        controller = GetComponent<DialogueController>();
    }

    /// <summary>
    /// 初始化任务发布者，设置初始对话和任务数据
    /// </summary>
    void Start()
    {
        controller.currentData = startDialogue;
        currentQuest = startDialogue.GetQuest();
    }

    /// <summary>
    /// 每帧检查任务状态，并更新对应的对话内容
    /// </summary>
    void Update()
    {
        // 如果任务已开始
        if (IsStarted)
        {
            // 根据完成状态设置对应的对话
            if (IsCompleted)
            {
                controller.currentData = completeDialogue;
            }
            else
            {
                controller.currentData = progressDialogue;
            }
        }

        // 如果任务已结束，设置结束对话
        if (IsFinished)
        {
            controller.currentData = finishDialogue;
        }
    }

}