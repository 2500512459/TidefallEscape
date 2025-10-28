using UnityEngine;

/// <summary>
/// ������� InputManager ��״̬��/�رձ���UI
/// </summary>
public class InventoryUIController : MonoBehaviour
{
    private bool lastInventoryState = false; // ��һ֡�ı���״̬����

    private void Update()
    {
        // �� InputManager ��ȡ��ǰ��������״̬
        bool currentState = InputManager.Instance.isInventoryOpen;

        // ���״̬�����仯
        if (currentState != lastInventoryState)
        {
            if (currentState)
            {
                // �򿪱������
                UIManger.Instance.ShowPanel<InventoryPanel>();
            }
            else
            {
                // �رձ������
                UIManger.Instance.HidePanel<InventoryPanel>();
            }

            // ���»���
            lastInventoryState = currentState;
        }
    }
}
