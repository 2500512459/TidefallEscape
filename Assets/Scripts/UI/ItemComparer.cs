using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemComparer : IComparer<ItemStack>
{
    public int Compare(ItemStack a, ItemStack b)
    {
        // 空格子放到最后
        if (a == null || a.item == null) return 1;
        if (b == null || b.item == null) return -1;

        // 先按 itemName 排序
        int nameCompare = string.Compare(a.item.itemName, b.item.itemName);
        if (nameCompare != 0)
            return nameCompare;

        // 再按数量降序
        return b.count.CompareTo(a.count);
    }
}
