using UnityEngine;

/// <summary>
/// 奖励物品格（仅展示）
/// - 只显示物品图标、名称、数量
/// </summary>
public class RewardItem : ItemSlot
{
    protected override void Awake()
    {
        base.Awake();
        // 奖励展示不需要交互按钮
        if (selectNode != null)
            selectNode.SetActive(false);
    }

    /// <summary>
    /// 设置奖励信息
    /// </summary>
    public override void SetItem(ItemStack itemStack)
    {
        base.SetItem(itemStack);
        // 你可以在这里额外加特效或动画逻辑
    }

    public override void UpdateCellSelect(bool select)
    {
        // 奖励不支持选中状态
    }
}
