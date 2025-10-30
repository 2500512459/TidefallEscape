using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemComparer : IComparer<ItemStack>
{
    public int Compare(ItemStack a, ItemStack b)
    {
        // �ո��ӷ����
        bool aEmpty = (a == null || a.item == null);
        bool bEmpty = (b == null || b.item == null);

        if (aEmpty && bEmpty) return 0;   // ���� -> ���
        if (aEmpty) return 1;             // a�� -> �ź���
        if (bEmpty) return -1;            // b�� -> ��ǰ��

        // ������Ʒʱ���� itemID ����
        return string.Compare(a.item.itemID, b.item.itemID, System.StringComparison.Ordinal);


    }
}
