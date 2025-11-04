using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 任务管理器，负责管理所有任务的状态和进度更新
/// 使用单例模式确保全局唯一实例
/// </summary>
public class QuestManager : MonoSingleton<QuestManager>
{
    /// <summary>
    /// 任务项数据结构，封装任务数据和状态
    /// </summary>
    [System.Serializable]
    public class QuestTask
    {
        public QuestDataSO questData;   // 关联的任务数据
        // 通过属性封装，直接操作QuestDataSO中的状态字段
        public bool IsStarted { get { return questData.isStarted; } set { questData.isStarted = value; } }
        public bool IsCompleted { get { return questData.isCompleted; } set { questData.isCompleted = value; } }
        public bool IsFinished { get { return questData.isFinished; } set { questData.isFinished = value; } }
    }

    public List<QuestTask> tasks = new List<QuestTask>();   // 所有已接取的任务列表
    public List<QuestTask> CompleteTaskList = new List<QuestTask>();    // 已完成的任务列表

    /// <summary>
    /// 更新任务进度，当发生相关事件时调用（如击杀怪物、收集物品等）
    /// </summary>
    /// <param name="requireName">要求名称</param>
    /// <param name="amount">增加的数量</param>
    public void UpdateQuestProgress(string requireName, int amount)
    {
        // 遍历所有任务
        foreach (var task in tasks)
        {
            // 查找匹配的任务要求
            var matchTask = task.questData.questRequires.Find(r => r.name == requireName);
            if (matchTask != null)
                // 更新当前完成数量
                matchTask.currentAmount += amount;
            
            // 检查任务是否完成
            task.questData.CheckQuestProgress();    
        }
    }
    
    /// <summary>
    /// 检查是否已接取指定任务
    /// </summary>
    /// <param name="data">任务数据</param>
    /// <returns>是否已接取该任务</returns>
    public bool HaveQuest(QuestDataSO data)
    {
        if (data != null)
            // 通过任务名称判断是否已存在
            return tasks.Any(q => q.questData.questName == data.questName);
        else
            return false;
    }

    /// <summary>
    /// 获取指定任务的任务项
    /// </summary>
    /// <param name="data">任务数据</param>
    /// <returns>对应的任务项</returns>
    public QuestTask GetQuestTask(QuestDataSO data)
    {
        return tasks.Find(q => q.questData.questName == data.questName);
    }
}