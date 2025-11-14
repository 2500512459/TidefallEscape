using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一个任务分支包含四段对话：开始、进行中、完成、结束
/// 用于任务链中逐个分支地配置对话与任务
/// </summary>
[CreateAssetMenu(fileName = "QuestDialogueSetSO", menuName = "Quest/QuestDialogueSetSO")]
public class QuestDialogueSetSO : ScriptableObject
{
    public DialogueDataSO startDialogue;
    public DialogueDataSO progressDialogue;
    public DialogueDataSO completeDialogue;
    public DialogueDataSO finishDialogue;
}



