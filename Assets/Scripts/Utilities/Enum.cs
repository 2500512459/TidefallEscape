//��������
public enum ItemType
{
    Weapon,         //����
    Ammo,           //��ҩ
    Consumable,     //����Ʒ
    Material        //����
}
//����ϡ�ж�
public enum ItemRarity
{
    Common,         //����
    Rare,           //����
    Epic,           //ʷʫ��
    Legendary       //���漶
}
//�������
public enum InventoryType
{
    Backpack,
    Equipment,
    Storage,
    Loot,
    Shop
}
// �ֿ�״̬
public enum InventoryContext
{
    Default,    // ��ͨ״̬��ֻ�ܴ򿪱���+װ����
    Home,       // �ڼң�����+װ��+�ֿ⣩
    Looting     // �򿪱���ʱ������+װ��+�����
}

public enum ShopType
{
    WeaponShop,     //������
    AmmoShop,       //��ҩ��
    ConsumableShop,     //����Ʒ��
    MaterialShop        //���ϵ�
}
