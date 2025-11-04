using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
/// <summary>
/// 掉落栏（LootBar）
/// - 仅在 LootData 存在时初始化
/// - 当宝箱打开时（LootData 赋值并触发 OnInventoryChanged）才刷新显示
/// </summary>
public class LootBar : MonoBehaviour
{
    [Header("模板 StorageItem")]
    public StorageItem StorageItemPref; // 模板预制体（Hierarchy里放一份并禁用）
    [Header("选中信息显示")]
    public GameObject InfoNode;  // 选中物品信息节点
    public Image selectIcon;     // 图标
    public TextMeshProUGUI selectItemName;  // 名称
    public TextMeshProUGUI selectNum;       // 数量
    public TextMeshProUGUI selectItemDescription; // 描述

    [Header("InfoNode 偏移设置")]
    private Vector2 originalInfoNodePos;  // 记录 InfoNode 原始位置
    public Vector2 offset = new Vector2(0f, 0f);    // InfoNode 偏移

    private InventoryDataSO LootData;  // 当前掉落数据（可能为 null）
    private List<StorageItem> slotList = new List<StorageItem>();

    public int curSelectIndex = -1; // 当前选中索引
    private UnityAction<int> onSlotClicked; // 点击回调

    private void Awake()
    {
        // 此时 LootData 可能还没被赋值
        LootData = InventoryManager.Instance.GetInventory(InventoryType.Loot);
    }

    private void Start()
    {
        // 初始阶段不要初始化格子（LootData 可能为空）
        // 监听全局事件
        InventoryManager.Instance.OnInventoryChangedEvent += OnInventoryChanged;
    }
    private void OnEnable()
    {
        // 保证打开面板时立即刷新当前LootData
        LootData = InventoryManager.Instance.GetInventory(InventoryType.Loot);
        if (LootData != null)
        {
            if (slotList.Count == 0)
                InitSlots();
            RefreshAllSlots();
        }

        // 当LootBar激活时，向右偏移 InfoNode
        if (InfoNode != null)
        {
            // 记录初始位置（只在第一次时保存）
            if (originalInfoNodePos == Vector2.zero)
                originalInfoNodePos = InfoNode.GetComponent<RectTransform>().anchoredPosition;

            // 向右偏移一定距离
            var rect = InfoNode.GetComponent<RectTransform>();
            rect.anchoredPosition = originalInfoNodePos + offset;
        }
    }
    private void OnDisable()
    {
        // 当LootBar隐藏时，恢复 InfoNode 原始位置
        if (InfoNode != null)
        {
            var rect = InfoNode.GetComponent<RectTransform>();
            rect.anchoredPosition = originalInfoNodePos;
        }
    }
    /// <summary>
    /// 当全局库存变化时触发
    /// </summary>
    private void OnInventoryChanged(InventoryType changedType)
    {
        if (changedType != InventoryType.Loot)
            return;

        // 每次宝箱打开时，LootData 会被重新赋值
        LootData = InventoryManager.Instance.GetInventory(InventoryType.Loot);

        // 还没有掉落栏数据，不刷新
        if (LootData == null)
            return;

        // 如果还没创建槽位，先初始化
        if (slotList.Count == 0)
        {
            InitSlots();
        }

        // 然后刷新显示
        RefreshAllSlots();
    }

    /// <summary>
    /// 初始化槽位
    /// </summary>
    private void InitSlots()
    {
        // 清理旧节点
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        slotList.Clear();

        if (StorageItemPref == null)
        {
            Debug.LogError("[LootBar] StorageItemPref 未设置");
            return;
        }

        if (LootData == null)
        {
            Debug.LogWarning("[LootBar] LootData 为空，等待宝箱赋值后再初始化");
            return;
        }

        StorageItemPref.gameObject.SetActive(false);

        int count = LootData.maxCount;
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(StorageItemPref.gameObject, transform);
            go.name = $"LootCell_{i}";
            go.SetActive(true);

            var slot = go.GetComponent<StorageItem>();
            if (slot != null)
            {
                slot.AddButtonClickListener(OnSlotClickedInternal);
                slotList.Add(slot);
            }
        }
    }

    /// <summary>
    /// 内部点击槽位回调
    /// </summary>
    private void OnSlotClickedInternal(int index)
    {
        if (index < 0 || index >= slotList.Count) return;
        if (index == curSelectIndex) return;

        var data = LootData.items[index];

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
    /// 刷新所有槽位
    /// </summary>
    private void RefreshAllSlots()
    {
        if (LootData == null || LootData.items == null) return;

        // 如果掉落数量变化，可重新初始化（例如宝箱切换）
        if (slotList.Count != LootData.maxCount)
        {
            InitSlots();
        }

        for (int i = 0; i < slotList.Count; i++)
        {
            var slot = slotList[i];
            if (slot == null) continue;

            var stack = LootData.items[i];
            slot.SetItem(stack, InventoryType.Loot, i);
            slot.UpdateCellSelect(i == curSelectIndex);
        }
    }

    /// <summary>
    /// 更新选中物品详情（可扩展）
    /// </summary>
    private void UpdateSelectItemInfo()
    {
        if (LootData == null || curSelectIndex < 0 || curSelectIndex >= LootData.items.Count)
        {
            InfoNode?.SetActive(false);
            return;
        }

        var data = LootData.items[curSelectIndex];
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
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChangedEvent -= OnInventoryChanged;
        }

        onSlotClicked = null;
    }
}
