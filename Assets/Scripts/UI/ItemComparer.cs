using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemComparer : IComparer<ItemStack>
{
    public int Compare(ItemStack a, ItemStack b)
    {
        // 空格子放最后
        bool aEmpty = (a == null || a.item == null);
        bool bEmpty = (b == null || b.item == null);

        if (aEmpty && bEmpty) return 0;   // 都空 -> 相等
        if (aEmpty) return 1;             // a空 -> 放后面
        if (bEmpty) return -1;            // b空 -> 放前面

        // 都有物品时，按 itemID 排序
        return string.Compare(a.item.itemID, b.item.itemID, System.StringComparison.Ordinal);


    }
}
