using UnityEngine;

/// <summary>
/// 负责检测输入并打开/关闭背包UI
/// </summary>
public class InventoryUIController : MonoBehaviour
{
    private bool isInventoryOpen = false;

    private void Update()
    {
        // 检测 Tab 键开关
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    private void ToggleInventory()
    {
        if (isInventoryOpen)
        {
            // 关闭背包面板
            UIManger.Instance.HidePanel<InventoryPanel>();
            isInventoryOpen = false;
        }
        else
        {
            // 打开背包面板
            UIManger.Instance.ShowPanel<InventoryPanel>();
            isInventoryOpen = true;
        }
    }
}
