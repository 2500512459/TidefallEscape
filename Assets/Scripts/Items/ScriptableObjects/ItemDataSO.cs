using UnityEngine;

[CreateAssetMenu(fileName = "ItemDataSO", menuName = "Item/ItemDataSO")]
public class ItemDataSO : ScriptableObject
{
    public string itemID;
    public string itemName;
    public Sprite icon;
    public ItemType type;           //���ͣ�����������Ʒ��
    public ItemRarity rarity;       //ϡ�ж�
    public int maxStack = 99;       //��������
    public GameObject worldPrefab;

    [TextArea]
    public string description;      //������Ϣ
}
