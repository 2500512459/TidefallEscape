using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemComparer : IComparer<ItemStack>
{
    public int Compare(ItemStack a, ItemStack b)
    {
        // �ո��ӷŵ����
        if (a == null || a.item == null) return 1;
        if (b == null || b.item == null) return -1;

        // �Ȱ� itemName ����
        int nameCompare = string.Compare(a.item.itemName, b.item.itemName);
        if (nameCompare != 0)
            return nameCompare;

        // �ٰ���������
        return b.count.CompareTo(a.count);
    }
}
