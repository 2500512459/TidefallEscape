using UnityEngine;

/// <summary>
/// ���������벢��/�رձ���UI
/// </summary>
public class InventoryUIController : MonoBehaviour
{
    private bool isInventoryOpen = false;

    private void Update()
    {
        // ��� Tab ������
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    private void ToggleInventory()
    {
        if (isInventoryOpen)
        {
            // �رձ������
            UIManger.Instance.HidePanel<InventoryPanel>();
            isInventoryOpen = false;
        }
        else
        {
            // �򿪱������
            UIManger.Instance.ShowPanel<InventoryPanel>();
            isInventoryOpen = true;
        }
    }
}
