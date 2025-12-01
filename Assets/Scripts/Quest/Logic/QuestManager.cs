using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 任务管理器，负责管理所有任务的状态和进度更新
/// 使用单例模式确保全局唯一实例，并在场景切换时保持不被销毁
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("存档设置")]
    [Tooltip("用于保存到 PlayerPrefs 的唯一键名")]
    [SerializeField] private string saveKey = "QuestManagerState";

    [Header("任务模板列表（用于存档恢复，根据 questName 匹配）")]
    [Tooltip("请把所有可能使用到的 QuestDataSO 拖到这里，用于从存档恢复任务状态")]
    public List<QuestDataSO> questDatabase = new List<QuestDataSO>();

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
    /// 确保任务管理器在场景切换时不被销毁，同时避免重复实例
    /// </summary>
    protected void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        // 使用DontDestroyOnLoad确保场景切换时对象不被销毁
        DontDestroyOnLoad(this.gameObject);

        // 从存档恢复任务状态
        LoadState();
    }

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

        // 每次进度更新后保存一次任务状态
        SaveState();
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

    #region 存档结构与逻辑

    [System.Serializable]
    private class QuestRequireState
    {
        public string name;
        public int currentAmount;
    }

    [System.Serializable]
    private class QuestTaskState
    {
        public string questName;
        public bool isStarted;
        public bool isCompleted;
        public bool isFinished;
        public List<QuestRequireState> requires = new List<QuestRequireState>();
    }

    [System.Serializable]
    private class QuestManagerState
    {
        public List<QuestTaskState> activeTasks = new List<QuestTaskState>();
        public List<QuestTaskState> completedTasks = new List<QuestTaskState>();
    }

    /// <summary>
    /// 对外公开的保存接口，在任务发生变化时调用
    /// </summary>
    public void SaveState()
    {
        var state = new QuestManagerState();

        foreach (var task in tasks)
        {
            state.activeTasks.Add(CreateStateFromTask(task));
        }

        foreach (var task in CompleteTaskList)
        {
            state.completedTasks.Add(CreateStateFromTask(task));
        }

        string key = GetSaveKey();
        var saveManager = SaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.Save(state, key);
        }
        else
        {
            string jsonData = JsonUtility.ToJson(state);
            PlayerPrefs.SetString(key, jsonData);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// 从 PlayerPrefs / SaveManager 读取任务状态
    /// </summary>
    private void LoadState()
    {
        string key = GetSaveKey();
        if (!PlayerPrefs.HasKey(key))
        {
            // 没有存档，保持默认空列表
            return;
        }

        var state = new QuestManagerState();
        var saveManager = SaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.Load(state, key);
        }
        else
        {
            string jsonData = PlayerPrefs.GetString(key);
            JsonUtility.FromJsonOverwrite(jsonData, state);
        }

        RestoreFromState(state);
    }

    private QuestTaskState CreateStateFromTask(QuestTask task)
    {
        var data = task.questData;
        var state = new QuestTaskState
        {
            questName = data.questName,
            isStarted = data.isStarted,
            isCompleted = data.isCompleted,
            isFinished = data.isFinished,
            requires = new List<QuestRequireState>()
        };

        foreach (var require in data.questRequires)
        {
            state.requires.Add(new QuestRequireState
            {
                name = require.name,
                currentAmount = require.currentAmount
            });
        }

        return state;
    }

    private void RestoreFromState(QuestManagerState state)
    {
        tasks.Clear();
        CompleteTaskList.Clear();
        if (state == null) return;

        // 恢复进行中的任务
        foreach (var taskState in state.activeTasks)
        {
            var questInstance = CreateQuestFromState(taskState);
            if (questInstance != null)
            {
                tasks.Add(new QuestTask { questData = questInstance });
            }
        }

        // 恢复已完成的任务
        foreach (var taskState in state.completedTasks)
        {
            var questInstance = CreateQuestFromState(taskState);
            if (questInstance != null)
            {
                CompleteTaskList.Add(new QuestTask { questData = questInstance });
            }
        }
    }

    private QuestDataSO CreateQuestFromState(QuestTaskState state)
    {
        if (state == null || string.IsNullOrEmpty(state.questName))
            return null;

        var template = FindQuestTemplate(state.questName);
        if (template == null)
        {
            Debug.LogWarning($"[QuestManager] 无法从 questDatabase 中找到任务模板: {state.questName}，该任务将不会从存档恢复。");
            return null;
        }

        // 使用模板创建运行时实例
        var instance = ScriptableObject.Instantiate(template);
        instance.isStarted = state.isStarted;
        instance.isCompleted = state.isCompleted;
        instance.isFinished = state.isFinished;

        // 恢复每个任务需求的 currentAmount
        foreach (var requireState in state.requires)
        {
            var require = instance.questRequires.Find(r => r.name == requireState.name);
            if (require != null)
            {
                require.currentAmount = requireState.currentAmount;
            }
        }

        return instance;
    }

    private QuestDataSO FindQuestTemplate(string questName)
    {
        if (string.IsNullOrEmpty(questName)) return null;
        return questDatabase.FirstOrDefault(q => q != null && q.questName == questName);
    }

    private string GetSaveKey()
    {
        if (!string.IsNullOrEmpty(saveKey))
        {
            return saveKey;
        }
        return "QuestManagerState";
    }

    #endregion
}