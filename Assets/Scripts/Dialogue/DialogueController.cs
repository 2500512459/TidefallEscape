using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueController : MonoBehaviour
{
    public DialogueDataSO currentData;
    public bool canTalk = true; // 是否可以进入对话界面

    public void OpenDialogue()
    {
        canTalk = false;
        DialogueUI.Instance.UpdataDialogueData(currentData, this); // 传递自身引用
        DialogueUI.Instance.UpdateMainDialogue(currentData.dialoguePieces[0]);

        // 禁止移动和视角旋转，保留 E 键交互事件继续有效
        PlayerInput.Instance.DisableMovementAndLook(disableQuestInput: true);
        PlayerInput.Instance.FireInput?.Disable();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void CloseDialogue()
    {
        canTalk = true;
        DialogueUI.Instance.dialoguePanel.SetActive(false);

        PlayerInput.Instance.EnableMovementAndLook(enableQuestInput: true);
        PlayerInput.Instance.FireInput?.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
