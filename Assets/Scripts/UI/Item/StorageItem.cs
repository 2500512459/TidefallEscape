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

    [Header("状态数据")]
    public InventoryType inventoryType;     // 所属背包类型
    public int slotIndex;                   // 格子索引

    private UnityAction<int> onClickAction;

    protected override void Awake()
    {
        base.Awake();
        if (button != null)
        {
            button.onClick.AddListener(() => onClickAction?.Invoke(slotIndex));
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
}
