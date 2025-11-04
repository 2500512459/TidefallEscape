using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScrollViewPanel : MonoBehaviour
{
    protected LoopScrollView loopScrollView;  // 循环滚动视图
    protected virtual void Start()
    {
        loopScrollView = GetComponentInChildren<LoopScrollView>();
    }

    /// <summary>
    /// 更新item信息
    /// </summary>
    protected virtual void OnUpdateScrollItemAction(StorageItem item, int index)
    {

    }

    /// <summary>
    /// 点击item事件
    /// </summary>
    /// <param name="index"></param>
    protected virtual void OnClickScrollItemAction(int index)
    {

    }
}
