using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
/// <summary>
/// 装备栏（参考 LoopScrollView 的实现方式）
/// - 使用 StorageItemPref 为模板（该模板应在场景/预制中禁用）
/// - 创建 equipmentData.maxCount 个槽位（装备栏不可滚动）
/// - 支持点击选中、刷新、监听数据变更
/// </summary>
public class EquipmentBar : MonoBehaviour
{
    [Header("模板 StorageItem")]
    public StorageItem StorageItemPref; // 模板预制体（在Hierarchy里放一份并禁用）

    [Header("槽位背景")]
    public RectTransform EquipSlotCellPref; // EquipSlotCell 预制（包含背景图）
    [SerializeField] private int defaultSlotCount = 8;
    [SerializeField] private string equipSlotSpriteResourcesPath = "Icon/Equipment";

    private InventoryDataSO equipmentData;  // 装备栏数据
    private readonly List<StorageItem> slotList = new List<StorageItem>();
    private readonly List<Image> slotBackgroundList = new List<Image>();
    private Sprite[] equipSlotSprites;

    [Header("选中信息显示")]
    public GameObject InfoNode;  // 选中物品面板
    public Image selectIcon;     // 选中图标
    public TextMeshProUGUI selectItemName;  // 选中物品名称
    public TextMeshProUGUI selectNum;   // 选中物品数量
    public TextMeshProUGUI selectItemDescription; // 选中物品描述

    // 当前选中索引（默认 -1 表示无选中）
    public int curSelectIndex = -1;

    // 外部可能需要订阅点击事件（例如打开详情/装备/卸下）
    private UnityAction<int> onSlotClicked;

    private void Awake()
    {
        equipmentData = InventoryManager.Instance.GetInventory(InventoryType.Equipment);
        int targetCount = Mathf.Max(defaultSlotCount, equipmentData != null ? equipmentData.maxCount : 0);
        equipmentData.EnsureSlotCount(targetCount);

        LoadEquipSlotSprites();
    }

    private void LoadEquipSlotSprites()
    {
        if (string.IsNullOrEmpty(equipSlotSpriteResourcesPath)) return;

        equipSlotSprites = Resources.LoadAll<Sprite>(equipSlotSpriteResourcesPath);
        if (equipSlotSprites == null || equipSlotSprites.Length == 0)
        {
            Debug.LogWarning($"[EquipmentBar] 未在 Resources/{equipSlotSpriteResourcesPath} 找到任何槽位背景图。");
        }
    }

    private void Start()
    {
        InitSlots();
        RefreshAllSlots();

        // 监听全局 InventoryManager 的事件
        InventoryManager.Instance.OnInventoryChangedEvent += OnInventoryChanged;
    }

    /// <summary>
    /// 初始化槽位：创建 equipmentData.maxCount 个 StorageItem 实例并绑定点击回调
    /// </summary>
    private void InitSlots()
    {
        // 清理旧节点
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        slotList.Clear();
        slotBackgroundList.Clear();

        if (StorageItemPref == null)
        {
            Debug.LogError("[EquipmentBar] StorageItemPref 未设置");
            return;
        }
        if (EquipSlotCellPref == null)
        {
            Debug.LogError("[EquipmentBar] EquipSlotCellPref 未设置");
            return;
        }

        StorageItemPref.gameObject.SetActive(false);

        int count = Mathf.Max(defaultSlotCount, equipmentData != null ? equipmentData.maxCount : 0);
        for (int i = 0; i < count; i++)
        {
            var slotCell = Instantiate(EquipSlotCellPref, transform);
            slotCell.name = $"EquipSlotCell_{i}";
            slotCell.gameObject.SetActive(true);

            Image background = slotCell.GetComponent<Image>();
            if (background == null)
            {
                background = slotCell.GetComponentInChildren<Image>(true);
            }
            if (background != null && equipSlotSprites != null && equipSlotSprites.Length > 0)
            {
                int spriteIndex = i % equipSlotSprites.Length;
                background.sprite = equipSlotSprites[spriteIndex];
            }
            slotBackgroundList.Add(background);

            Transform anchor = slotCell;
            var anchorTransform = slotCell.Find("ItemAnchor");
            if (anchorTransform != null)
            {
                anchor = anchorTransform;
            }

            var itemGo = Instantiate(StorageItemPref.gameObject, anchor);
            itemGo.name = $"EquipItem_{i}";
            itemGo.SetActive(true);

            // 拉伸到锚点范围
            var itemRect = itemGo.transform as RectTransform;
            if (itemRect != null)
            {
                itemRect.anchorMin = Vector2.zero;
                itemRect.anchorMax = Vector2.one;
                itemRect.offsetMin = Vector2.zero;
                itemRect.offsetMax = Vector2.zero;
                itemRect.localScale = Vector3.one;
                itemRect.localPosition = Vector3.zero;
            }

            var slot = itemGo.GetComponent<StorageItem>();
            if (slot == null) continue;

            slot.AddButtonClickListener(OnSlotClickedInternal);
            slotList.Add(slot);
        }
    }

    /// <summary>
    /// 内部点击槽位回调
    /// </summary>
    private void OnSlotClickedInternal(int index)
    {
        if (index < 0 || index >= slotList.Count) return;

        if (index == curSelectIndex) return; // 可选：重复点击不处理

        var data = equipmentData.items[index];
        // 点击空格子（无物品）
        if (data == null || data.item == null)
        {
            curSelectIndex = -1;
            if (InfoNode != null) InfoNode.SetActive(false);
            RefreshAllSlots();
            return;
        }
        curSelectIndex = index;
        RefreshAllSlots();
        UpdateSelectItemInfo();
        onSlotClicked?.Invoke(index);
    }

    /// <summary>
    /// 外部注册点击事件
    /// </summary>
    public void AddSlotClickAction(UnityAction<int> clickAction)
    {
        onSlotClicked += clickAction;
    }

    /// <summary>
    /// 当全局库存变化时触发
    /// </summary>
    private void OnInventoryChanged(InventoryType changedType)
    {
        // 只响应装备栏变化
        if (changedType == InventoryType.Equipment)
        {
            RefreshAllSlots();
        }
    }

    /// <summary>
    /// 刷新所有槽位
    /// </summary>
    private void RefreshAllSlots()
    {
        if (equipmentData == null) return;

        equipmentData.EnsureSlotCount(slotList.Count);
        for (int i = 0; i < slotList.Count; i++)
        {
            var slot = slotList[i];
            if (slot == null) continue;

            var stack = equipmentData.items[i];
            slot.SetItem(stack, InventoryType.Equipment, i);
            slot.UpdateCellSelect(i == curSelectIndex);

            bool hasItem = stack != null && stack.item != null;
            if (i < slotBackgroundList.Count && slotBackgroundList[i] != null)
            {
                slotBackgroundList[i].enabled = !hasItem;
            }
        }
    }

    /// <summary>
    /// 更新选中物品详情
    /// </summary>
    private void UpdateSelectItemInfo()
    {
        if (curSelectIndex < 0 || curSelectIndex >= equipmentData.items.Count)
        {
            InfoNode?.SetActive(false);
            return;
        }

        var data = equipmentData.items[curSelectIndex];
        if (data == null || data.item == null)
        {
            InfoNode?.SetActive(false);
            return;
        }

        if (InfoNode != null) InfoNode.SetActive(true);

        if (selectNum != null) selectNum.text = data.count.ToString();
        if (selectItemName != null) selectItemName.text = data.item.itemName;
        if (selectIcon != null) selectIcon.sprite = data.item.icon;
        if (selectItemDescription != null) selectItemDescription.text = data.item.description;
    }

    private void OnDestroy()
    {
        // 解除全局事件监听
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChangedEvent -= OnInventoryChanged;
        }

        onSlotClicked = null;
    }

}
