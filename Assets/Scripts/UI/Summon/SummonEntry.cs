using UnityEngine;

/// <summary>
/// 召唤条目数据类
/// 使用ScriptableObject存储单个召唤项的所有信息
/// 可在Unity编辑器中通过右键菜单创建：UI -> Summon Entry
/// </summary>
[CreateAssetMenu(fileName = "New Summon Entry", menuName = "UI/Summon Entry", order = 0)]
public class SummonEntry : ScriptableObject
{
    /// <summary>
    /// 召唤项的图片资源
    /// </summary>
    [field:SerializeField] public Sprite EntryGraphic { get; private set; }
    /// <summary>
    /// 要召唤的船只类型
    /// </summary>
    [field:SerializeField] public ShipType ShipType { get; private set; }
    /// <summary>
    /// 船只攻击力
    /// </summary>
    [field:SerializeField] public float AttackPower { get; private set; }
    /// <summary>
    /// 船只防御力
    /// </summary>
    [field:SerializeField] public float DefensePower { get; private set; }
    /// <summary>
    /// 船只航行速度
    /// </summary>
    [field:SerializeField] public float SailingSpeed { get; private set; }
    
    /// <summary>
    /// 获取格式化的描述文本（包含攻击力、防御力、航行速度）
    /// </summary>
    /// <returns>格式化的描述字符串</returns>
    public string GetDescriptionText()
    {
        return $"攻击力: {AttackPower}\n防御力: {DefensePower}\n航行速度: {SailingSpeed}";
    }
    
    /// <summary>
    /// 获取船只类型的显示名称
    /// </summary>
    /// <returns>船只类型名称字符串</returns>
    public string GetShipTypeName()
    {
        return ShipType.ToString();
    }
}

