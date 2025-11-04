//装备类型
public enum ItemType
{
    Weapon,         //武器
    Ammo,           //弹药
    Consumable,     //消耗品
    Material        //材料
}
//物品稀有度
public enum ItemRarity
{
    Common,         //普通
    Rare,           //稀有
    Epic,           //史诗级
    Legendary       //传奇级
}
//背包类型
public enum InventoryType
{
    Backpack,
    Equipment,
    Storage,
    Loot,
    Shop
}
// 背包状态
public enum InventoryContext
{
    Default,    // 普通状态（只能打开背包+装备栏）
    Home,       // 在家时（背包+装备+仓库）
    Looting     // 打开战利品时（背包+装备+战利品））
}

// 商店类型
public enum ShopType
{
    WeaponShop,     //武器店
    AmmoShop,       //弹药店
    ConsumableShop,     //消耗品店
    MaterialShop        //材料店
}
