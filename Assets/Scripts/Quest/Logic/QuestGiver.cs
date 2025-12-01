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
    
    [Header("任务链（可选）：按顺序配置每个分支的四段对话")]
    public List<QuestDialogueSetSO> questBranches = new List<QuestDialogueSetSO>();
    int currentBranchIndex = 0;

    // 不同任务状态对应的对话数据
    public DialogueDataSO startDialogue;        // 初始对话
    public DialogueDataSO progressDialogue;     // 进行中对话
    public DialogueDataSO completeDialogue;     // 完成对话
    public DialogueDataSO finishDialogue;       // 结束对话

    bool UseBranches()
    {
        return questBranches != null && questBranches.Count > 0;
    }

    QuestDialogueSetSO CurrentSet()
    {
        if (!UseBranches()) return null;
        if (currentBranchIndex < 0 || currentBranchIndex >= questBranches.Count) return null;
        return questBranches[currentBranchIndex];
    }

    void ApplySetToFields(QuestDialogueSetSO set)
    {
        if (set == null) return;
        startDialogue = set.startDialogue;
        progressDialogue = set.progressDialogue;
        completeDialogue = set.completeDialogue;
        finishDialogue = set.finishDialogue;
    }

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
            // 领取奖励后，任务会从进行中列表移至完成列表
            // 因此这里需要在 CompleteTaskList 中检查对应任务的结束状态
            if (currentQuest == null) return false;
            var finishedTask = QuestManager.Instance.CompleteTaskList
                .Find(q => q.questData.questName == currentQuest.questName);
            return finishedTask != null && finishedTask.IsFinished;
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
        if (UseBranches())
        {
            // 根据已完成的任务推断当前应该处于任务链的哪一个分支
            ResolveBranchIndexFromQuestProgress();
            var set = CurrentSet();
            ApplySetToFields(set);
        }

        controller.currentData = startDialogue;
        currentQuest = startDialogue != null ? startDialogue.GetQuest() : null;
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
            // 任务链：将下一个分支提升为当前分支（finish 展示完后，下一次交互进入新分支的 start）
            if (UseBranches() && currentBranchIndex < questBranches.Count - 1)
            {
                currentBranchIndex++;
                var nextSet = CurrentSet();
                ApplySetToFields(nextSet);
                currentQuest = startDialogue != null ? startDialogue.GetQuest() : null;
            }
            else
            {
                // 单分支：允许把 finish 作为新的 start（兼容旧用法）
                if (startDialogue != finishDialogue)
                {
                    startDialogue = finishDialogue;
                    currentQuest = startDialogue != null ? startDialogue.GetQuest() : null;
                }
            }
        }
    }

    /// <summary>
    /// 根据 QuestManager 中已完成的任务，推断当前任务链应该处于哪一段分支
    /// 规则：从第 0 条分支开始，只要该分支的 finish 对应任务已 Finished，就推进到下一条分支
    /// </summary>
    void ResolveBranchIndexFromQuestProgress()
    {
        if (!UseBranches()) return;
        if (QuestManager.Instance == null) return;

        int resolvedIndex = 0;
        for (int i = 0; i < questBranches.Count; i++)
        {
            var set = questBranches[i];
            if (set == null || set.finishDialogue == null)
            {
                break;
            }

            var finishQuest = set.finishDialogue.GetQuest();
            if (finishQuest == null)
            {
                break;
            }

            // 在完成任务列表中查找是否存在对应的已完成任务
            var finishedTask = QuestManager.Instance.CompleteTaskList
                .Find(q => q.questData.questName == finishQuest.questName && q.IsFinished);

            if (finishedTask != null)
            {
                // 该分支的任务链已经走完，尝试推进到下一条
                resolvedIndex = Mathf.Min(i + 1, questBranches.Count - 1);
            }
            else
            {
                // 当前分支尚未完成，停止推进
                break;
            }
        }

        currentBranchIndex = resolvedIndex;
    }

}