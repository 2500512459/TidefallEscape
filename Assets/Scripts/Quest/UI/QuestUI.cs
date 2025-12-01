using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 任务系统UI管理器，负责任务界面的显示和交互
/// 使用单例模式确保全局唯一实例
/// </summary>
public class QuestUI : MonoSingleton<QuestUI>
{
    [Header("Elements")]
    public GameObject questPanel;           // 任务面板
    public ItemTooltip tooltip;             // 物品提示组件
    bool isOpen = false;                    // 面板是否打开
    PlayerInput playerInput;

    [Header("Quest Name")]
    public RectTransform questListTransform;    // 任务列表容器
    public QuestNameButton questNameButton;     // 任务名称按钮预制体

    [Header("Text Content")]
    public TextMeshProUGUI questContentText;    // 任务描述文本

    [Header("Requirement")]
    public RectTransform requireTransform;      // 要求列表容器
    public QuestRequirement requirement;        // 要求项预制体

    [Header("Reward Panel")]
    public RectTransform rewardTransform;       // 奖励列表容器
    public RewardItem rewardUI;                 // 奖励项预制体

    [Header("GetReward Button")]
    public GetRewardButton getRewardButton;         // 领取奖励按钮

    [Header("QuestList Button")]
    public Button questListButton;                // 进入任务列表按钮
    public Button questCompleteButton;            // 进入任务完成列表按钮

    void Start()
    {
        playerInput = PlayerInput.Instance;
        if (playerInput != null)
        {
            playerInput.QuestPressedEvent += OnQuestPressed;
        }
        questListButton.onClick.AddListener(() =>
        {
            SetupQuestList();
        });
        questCompleteButton.onClick.AddListener(() =>
        {
            SetupCompleteQuestList();
        });
    }

    void OnDestroy()
    {
        if (playerInput != null)
        {
            playerInput.QuestPressedEvent -= OnQuestPressed;
        }
    }
    void OnQuestPressed()
    {
        if (isOpen)
        {
            CloseQuestPanel();
        }
        else
        {
            OpenQuestPanel();
        }
    }

    void OpenQuestPanel()
    {
        isOpen = true;
        questPanel.SetActive(true);
        questContentText.text = "";
        SetupQuestList();
        playerInput?.DisableMovementAndLook(disableInteractionInput: true);
        if (playerInput != null && playerInput.FireInput != null)
        {
            playerInput.FireInput.Disable();
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseQuestPanel()
    {
        isOpen = false;
        questPanel.SetActive(false);
        tooltip.gameObject.SetActive(false);
        playerInput?.EnableMovementAndLook(enableInteractionInput: true);
        if (playerInput != null && playerInput.FireInput != null)
        {
            playerInput.FireInput.Enable();
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// 设置任务列表内容
    /// </summary>
    public void SetupQuestList()
    {
        // 清空现有列表内容
        foreach (Transform item in questListTransform)
        {
            Destroy(item.gameObject);
        }
        foreach (Transform item in requireTransform)
        {
            Destroy(item.gameObject);
        }
        foreach (Transform item in rewardTransform)
        {
            Destroy(item.gameObject);
        }
        // 隐藏领取按钮
        getRewardButton.gameObject.SetActive(false);
        // 清空任务描述
        questContentText.text = "";

        // 根据任务管理器中的任务创建列表项
        foreach (var task in QuestManager.Instance.tasks)
        {
            var newTask = Instantiate(questNameButton, questListTransform);
            newTask.SetupNameButton(task.questData);
        }
    }
    public void SetupCompleteQuestList()
    {
        // 清空现有列表内容
        foreach (Transform item in questListTransform)
        {
            Destroy(item.gameObject);
        }
        foreach (Transform item in requireTransform)
        {
            Destroy(item.gameObject);
        }
        foreach (Transform item in rewardTransform)
        {
            Destroy(item.gameObject);
        }
        // 隐藏领取按钮
        getRewardButton.gameObject.SetActive(false);
        // 清空任务描述
        questContentText.text = "";
        
        // 根据任务管理器中的任务创建列表项
        foreach (var task in QuestManager.Instance.CompleteTaskList)
        {
            var newTask = Instantiate(questNameButton, questListTransform);
            newTask.SetupNameButton(task.questData);
        }
    }
    /// <summary>
    /// 设置任务要求列表内容
    /// </summary>
    /// <param name="questData">任务数据</param>
    public void SetupRequireList(QuestDataSO questData)
    {
        // 显示任务描述
        questContentText.text = questData.description;
        
        // 清空现有要求列表
        foreach (Transform item in requireTransform)
        {
            Destroy(item.gameObject);
        }

        // 在展示前刷新一次收集类需求的当前数量（基于背包/仓库实际物品）
        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
        {
            bool progressUpdated = false;
            foreach (var require in questData.questRequires)
            {
                int actualCount = inventoryManager.GetQuestItemCount(require.name);
                if (actualCount > 0)
                {
                    int clampedAmount = Mathf.Min(actualCount, require.requiteAmount);
                    if (require.currentAmount != clampedAmount)
                    {
                        require.currentAmount = clampedAmount;
                        progressUpdated = true;
                    }
                }
            }
            if (progressUpdated)
            {
                questData.CheckQuestProgress();
            }
        }

        // 根据任务要求创建列表项
        bool showCurrentProgress = !questData.isFinished;
        foreach (var require in questData.questRequires)
        {
            var newRequire = Instantiate(requirement, requireTransform);
            newRequire.SetupRequirement(require.name, require.requiteAmount, require.currentAmount, showCurrentProgress);
        }
    }

    /// <summary>
    /// 设置奖励项显示
    /// </summary>
    /// <param name="itemData">物品数据</param>
    /// <param name="amount">数量</param>
    public void SetupRewardItem(ItemDataSO itemData, int amount)
    {
        // 创建物品堆叠数据
        var newItemData = new ItemStack(itemData, amount);
        // 实例化奖励项并设置数据
        var item = Instantiate(rewardUI, rewardTransform);
        item.SetItem(newItemData);
    }
}