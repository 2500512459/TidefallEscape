using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemComparer : IComparer<ItemStack>
{
    public int Compare(ItemStack a, ItemStack b)
    {
        // 空物品
        bool aEmpty = (a == null || a.item == null);
        bool bEmpty = (b == null || b.item == null);

        if (aEmpty && bEmpty) return 0;   // 空物品 -> 空物品
        if (aEmpty) return 1;             // a空物品 -> 当前物品
        if (bEmpty) return -1;            // b空物品 -> 当前物品

        // 比较物品ID
        return string.Compare(a.item.itemID, b.item.itemID, System.StringComparison.Ordinal);


    }
}
