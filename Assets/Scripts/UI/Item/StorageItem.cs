using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 背包/仓库专用物品格
/// - 支持点击、选中、索引记录等功能
/// </summary>
public class StorageItem : ItemSlot
{
    [Header("交互组件")]
    public Button button;

    [Header("掉落物遮罩")]
    public RarityMaskFill lootMaskFill;  // 仅掉落物格使用的遮罩动画
    public Image obstructionImage;  // 阻挡图片（遮罩播放时失能）

    [Header("状态数据")]
    public InventoryType inventoryType;     // 所属背包类型
    public int slotIndex;                   // 格子索引

    private UnityAction<int> onClickAction;
    
    // 记录已经播放过遮罩动画的格子（使用 slotIndex+itemID 作为唯一标识）
    // 这样每个格子都会独立播放遮罩，即使物品ID相同
    private static HashSet<string> playedMaskSlots = new HashSet<string>();
    
    // 记录正在队列中等待播放的格子（用于区分"已播放"和"待播放"状态）
    private static HashSet<string> queuedMaskSlots = new HashSet<string>();

    // 顺序播放队列系统
    private struct MaskPlayRequest
    {
        public StorageItem item;
        public ItemRarity rarity;
        public int slotIndex;
        public string slotKey; // 保存格子的唯一标识，用于完成后更新状态
    }

    private static List<MaskPlayRequest> maskPlayQueue = new List<MaskPlayRequest>();
    private static bool isPlayingSequence = false;
    private static MaskQueueManager queueManagerInstance;
    
    // 全局记录：当前这一轮播放到了哪个索引（用于回传给 TreasureBox）
    private static int currentMaxPlayedIndex = -1;
    // 全局记录：本次打开时的起始忽略索引（小于等于此索引的不播放）
    private static int startIgnoreIndex = -1;

    // 队列管理器辅助类
    private class MaskQueueManager : MonoBehaviour { }

    /// <summary>
    /// 设置本次打开宝箱的起始播放索引（外部调用）
    /// 小于等于此索引的物品将直接显示，不播放动画
    /// </summary>
    public static void SetStartPlayIndex(int index)
    {
        startIgnoreIndex = index;
        currentMaxPlayedIndex = index; // 从上次进度继续
    }

    /// <summary>
    /// 获取当前播放到的最大索引（外部调用，用于保存进度）
    /// </summary>
    public static int GetCurrentPlayedIndex()
    {
        return currentMaxPlayedIndex;
    }

    protected override void Awake()
    {
        base.Awake();
        if (button != null)
        {
            button.onClick.AddListener(() => onClickAction?.Invoke(slotIndex));
        }

        // 初始化：根据背包类型设置遮罩状态
        UpdateMaskVisibility(null);
        
        // 确保队列管理器存在
        EnsureQueueManager();
    }

    protected void OnDisable()
    {
        // 当UI被禁用时，立即停止当前的遮罩动画
        if (lootMaskFill != null)
        {
            lootMaskFill.StopAndHide();
        }
    }

    /// <summary>
    /// 根据背包类型和物品状态更新遮罩和阻挡图片的可见性
    /// 只有掉落物类型且物品不为空时才显示
    /// </summary>
    private void UpdateMaskVisibility(ItemStack itemStack, bool checkPlayedStatus = false)
    {
        // 如果正在播放动画，不改变状态（保持当前播放状态）
        if (lootMaskFill != null && lootMaskFill.IsPlaying)
        {
            return;
        }

        // 必须是掉落物类型，且物品不为空
        bool shouldShow = inventoryType == InventoryType.Loot && 
                          itemStack != null && 
                          itemStack.item != null;

        // 如果需要检查播放状态，且物品已播放过，则不显示
        if (checkPlayedStatus && shouldShow)
        {
            string slotKey = GetSlotKey(itemStack);
            
            // 如果已播放过，则不显示遮罩和阻挡图片
            // 但如果还在队列中等待播放，则应该显示（保持可见状态）
            if (playedMaskSlots.Contains(slotKey) && !queuedMaskSlots.Contains(slotKey))
            {
                shouldShow = false;
            }
        }

        // Mask：只有掉落物类型且物品不为空时才显示，默认失能
        if (lootMaskFill != null && lootMaskFill.gameObject != null)
        {
            lootMaskFill.gameObject.SetActive(shouldShow);
        }

        // ObstructionImage：只有掉落物类型且物品不为空时才显示，默认使能
        if (obstructionImage != null)
        {
            obstructionImage.enabled = shouldShow;
        }
    }

    /// <summary>
    /// 确保队列管理器存在（用于启动协程）
    /// </summary>
    private static void EnsureQueueManager()
    {
        if (queueManagerInstance == null)
        {
            GameObject managerObj = new GameObject("MaskFillQueueManager");
            queueManagerInstance = managerObj.AddComponent<MaskQueueManager>();
            Object.DontDestroyOnLoad(managerObj);
        }
    }

    /// <summary>
    /// 设置物品信息 + 类型与索引
    /// </summary>
    public void SetItem(ItemStack itemStack, InventoryType type, int index)
    {
        inventoryType = type;
        slotIndex = index;
        
        base.SetItem(itemStack);
        
        // 先更新遮罩可见性（检查播放状态，避免已播放过的物品显示遮罩）
        UpdateMaskVisibility(itemStack, checkPlayedStatus: true);
        
        // 然后处理掉落遮罩逻辑
        UpdateLootMask(itemStack);
    }

    /// <summary>
    /// 添加点击回调
    /// </summary>
    public void AddButtonClickListener(UnityAction<int> callback)
    {
        onClickAction += callback;
    }

    /// <summary>
    /// 清除回调
    /// </summary>
    public void ClearClickListener()
    {
        onClickAction = null;
    }

    /// <summary>
    /// 根据格子类型与物品稀有度控制掉落遮罩
    /// 每个物品只在第一次显示时播放一次动画，按slotIndex顺序播放
    /// </summary>
    private void UpdateLootMask(ItemStack itemStack)
    {
        // 如果正在播放动画，不改变状态（保持当前播放状态）
        if (lootMaskFill != null && lootMaskFill.IsPlaying)
        {
            return;
        }

        // 非掉落物类型或物品为空：隐藏遮罩和阻挡图片
        if (inventoryType != InventoryType.Loot || itemStack == null || itemStack.item == null)
        {
            if (lootMaskFill != null)
                lootMaskFill.StopAndHide();
            if (obstructionImage != null)
                obstructionImage.enabled = false;
            return;
        }

        // 检查该格子是否已经播放过遮罩动画（使用 slotIndex+itemID 作为唯一标识）
        string slotKey = GetSlotKey(itemStack);

        // 新增逻辑：如果当前索引 <= 起始忽略索引，直接显示物品，不播放动画
        // 这代表之前已经播放过了
        if (slotIndex <= startIgnoreIndex)
        {
             if (lootMaskFill != null)
                lootMaskFill.StopAndHide();
            if (obstructionImage != null)
                obstructionImage.enabled = false;
            return;
        }

        // 如果已经播放过且不在队列中，则不重复播放，失能ObstructionImage
        if (playedMaskSlots.Contains(slotKey) && !queuedMaskSlots.Contains(slotKey))
        {
            if (lootMaskFill != null)
                lootMaskFill.StopAndHide();
            if (obstructionImage != null)
                obstructionImage.enabled = false; // 已播放过，失能ObstructionImage
            return;
        }

        // 如果已经在队列中，保持当前状态，不重复加入
        if (queuedMaskSlots.Contains(slotKey))
        {
            return;
        }

        // 标记为已加入队列（但还未播放）
        queuedMaskSlots.Add(slotKey);
        
        // 确保遮罩和阻挡图片可见（因为要播放动画）
        if (lootMaskFill != null && lootMaskFill.gameObject != null)
            lootMaskFill.gameObject.SetActive(true);
        if (obstructionImage != null)
            obstructionImage.enabled = true;
        
        EnqueueMaskPlay(itemStack.item.rarity, slotKey);
    }

    /// <summary>
    /// 将遮罩播放请求加入队列
    /// </summary>
    private void EnqueueMaskPlay(ItemRarity rarity, string slotKey)
    {
        EnsureQueueManager();

        maskPlayQueue.Add(new MaskPlayRequest
        {
            item = this,
            rarity = rarity,
            slotIndex = slotIndex,
            slotKey = slotKey
        });

        // 按slotIndex排序
        maskPlayQueue.Sort((a, b) => a.slotIndex.CompareTo(b.slotIndex));

        // 如果当前没有在播放序列，则开始播放
        if (!isPlayingSequence)
        {
            queueManagerInstance.StartCoroutine(PlayMaskSequence());
        }
    }

    /// <summary>
    /// 按顺序播放遮罩动画序列
    /// </summary>
    private static IEnumerator PlayMaskSequence()
    {
        isPlayingSequence = true;

        while (maskPlayQueue.Count > 0)
        {
            // 取出队列第一个（slotIndex最小的）
            MaskPlayRequest request = maskPlayQueue[0];
            maskPlayQueue.RemoveAt(0);

            // 检查物品和遮罩组件是否仍然有效
            if (request.item == null || request.item.lootMaskFill == null)
                continue;

            // 使能Mask，失能ObstructionImage
            if (request.item.lootMaskFill.gameObject != null)
                request.item.lootMaskFill.gameObject.SetActive(true);
            
            if (request.item.obstructionImage != null)
                request.item.obstructionImage.enabled = false;

            // 设置完成回调，播放下一个
            bool isComplete = false;
            request.item.lootMaskFill.OnFillComplete = () => { isComplete = true; };

            // 播放当前遮罩动画
            request.item.lootMaskFill.Play(request.rarity);

            // 等待动画完成
            while (!isComplete)
            {
                yield return null;
            }
            
            // 动画完成后，从队列记录中移除，添加到已播放记录
            queuedMaskSlots.Remove(request.slotKey);
            playedMaskSlots.Add(request.slotKey);
            
            // 更新当前最大播放进度
            if (request.slotIndex > currentMaxPlayedIndex)
            {
                currentMaxPlayedIndex = request.slotIndex;
            }
        }

        isPlayingSequence = false;
    }

    /// <summary>
    /// 获取格子的唯一标识（slotIndex + itemID）
    /// </summary>
    public string GetSlotKey(ItemStack itemStack)
    {
        if (itemStack == null || itemStack.item == null)
            return $"{slotIndex}_null";
        
        string itemID = itemStack.item.itemID;
        if (string.IsNullOrEmpty(itemID))
            itemID = itemStack.item.itemName;
        
        return $"{slotIndex}_{itemID}";
    }

    /// <summary>
    /// 清除已播放记录（例如切换场景或重置时调用）
    /// </summary>
    public static void ClearPlayedMaskRecords()
    {
        playedMaskSlots.Clear();
        queuedMaskSlots.Clear();
        maskPlayQueue.Clear();
        isPlayingSequence = false;
        
        // 重置索引
        currentMaxPlayedIndex = -1;
        startIgnoreIndex = -1;
    }
}
