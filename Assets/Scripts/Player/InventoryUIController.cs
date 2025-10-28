using UnityEngine;

/// <summary>
/// 负责根据 InputManager 的状态打开/关闭背包UI
/// </summary>
public class InventoryUIController : MonoBehaviour
{
    private bool lastInventoryState = false; // 上一帧的背包状态缓存

    private void Update()
    {
        // 从 InputManager 获取当前背包开关状态
        bool currentState = InputManager.Instance.isInventoryOpen;

        // 如果状态发生变化
        if (currentState != lastInventoryState)
        {
            if (currentState)
            {
                // 打开背包面板
                UIManger.Instance.ShowPanel<InventoryPanel>();
            }
            else
            {
                // 关闭背包面板
                UIManger.Instance.HidePanel<InventoryPanel>();
            }

            // 更新缓存
            lastInventoryState = currentState;
        }
    }
}
