using UnityEngine;

[CreateAssetMenu(fileName = "ItemDataSO", menuName = "Item/ItemDataSO")]
public class ItemDataSO : ScriptableObject
{
    public string itemID;
    public string itemName;
    public Sprite icon;
    public ItemType type;           //类型
    public ItemRarity rarity;       //稀有度
    public int maxStack = 99;       //最大堆叠数
    [Tooltip("仅 Cannonball 类型生效")]
    public CannonballType cannonballType; //炮弹类型
    public GameObject worldPrefab;

    [TextArea]
    public string description;      // 描述信息

    [Header("装备效果")]
    [Tooltip("仅 Weapon 类型生效，提升角色攻击力")]
    public float attackBonus;
    [Tooltip("仅 Helmets/Armor 类型生效，提升角色防御力")]
    public float defenseBonus;
    [Tooltip("仅 backpacks 类型生效，增加背包容量")]
    public int extraBackpackSlots;
    [Tooltip("仅 necklaces 类型生效，提升最大法力值")]
    public float extraMaxManaPoints;

    [Header("交易属性")]
    [Tooltip("基础价值，买卖价格将基于此值计算")]
    public int baseValue = 10;
}