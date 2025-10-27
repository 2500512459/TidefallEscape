using UnityEngine;

[CreateAssetMenu(fileName = "ItemDataSO", menuName = "Item/ItemDataSO")]
public class ItemDataSO : ScriptableObject
{
    public string itemID;
    public string itemName;
    public Sprite icon;
    public ItemType type;           //类型（武器、消耗品）
    public ItemRarity rarity;       //稀有度
    public int maxStack = 99;       //最大叠加数
    public GameObject worldPrefab;

    [TextArea]
    public string description;      //描述信息
}
