//物品类型
public enum ItemType
{
    Weapon,         //武器
    Helmets,        //头盔
    Armor,          //盔甲
    backpacks,      //背包
    necklaces,      //项链
    Ammo,           //弹药
    Cannonball,     //炮弹
    Consumable,     //消耗品
    Material        //材料
}

// 炮弹类型
public enum CannonballType
{
    Normal,         //普通弹
    ArmorPiercing   //穿甲弹
}

//物品稀有度
public enum ItemRarity
{
    Common,         //普通
    Rare,           //稀有
    Epic,           //史诗级
    Legendary       //传奇级
}
//库类型
public enum InventoryType
{
    Backpack,
    Equipment,
    Storage,
    Loot,
    Shop,
    CannonBall
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
    MaterialShop,        //材料店
    FoodShop,          //食物店
}
// 职业类型
public enum ProfessionType
{
    Crewman,        //船员
    Lookout,        //瞭望员
    Captain,        //船长
    Shipwright      //船工
}

// 船只类型
public enum ShipType
{
    None,           //无船
    Sloop,          //单桅帆船
    Brig,           //双桅帆船
    Galleon,        //大型帆船
    Warship         //战舰
}

public enum EnemyShipType
{
    Small,
    Medium,
    Large
}