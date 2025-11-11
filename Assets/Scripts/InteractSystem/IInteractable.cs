using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    Transform Transform { get; } // 方便获取位置
    string HintText { get; }        // 交互提示文字
    void Interact(Character player); // 执行交互逻辑
    void OnFocus(Character player);  // 当玩家靠近时
    void OnLoseFocus(Character player); // 当玩家离开时
}
