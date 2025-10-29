//道具类型
public enum ItemType
{
    Weapon,         //武器
    Ammo,           //弹药
    Consumable,     //消耗品
    Material        //材料
}
//道具稀有度
public enum ItemRarity
{
    Common,         //常见
    Rare,           //罕见
    Epic,           //史诗级
    Legendary       //传奇级
}
//库的类型
public enum InventoryType 
{ 
    Backpack, 
    Equipment, 
    Storage,
    Loot
}
// 仓库状态
public enum InventoryContext
{
    Default,    // 普通状态（只能打开背包+装备）
    Home,       // 在家（背包+装备+仓库）
    Looting     // 打开宝箱时（背包+装备+掉落物）
}