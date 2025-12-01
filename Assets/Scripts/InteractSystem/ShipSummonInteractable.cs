using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipSummonInteractable : BaseInteractable
{
    public GameObject SummonPanel;
    
    public override void Interact(Character player)
    {
        if (SummonPanel.activeSelf)
        {
            SummonPanel.SetActive(false);
            InteractHintUI.Instance.ShowHint(hintText, key);
            
            PlayerInput.Instance.EnableMovementAndLook(enableQuestInput: true);
            PlayerInput.Instance.FireInput?.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }
        else
        {
            SummonPanel.SetActive(true);
            InteractHintUI.Instance.ShowHint("关闭召唤界面", key);
            // 禁止移动和视角旋转，保留 E 键交互事件继续有效
            PlayerInput.Instance.DisableMovementAndLook(disableQuestInput: true);
            PlayerInput.Instance.FireInput?.Disable();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}

