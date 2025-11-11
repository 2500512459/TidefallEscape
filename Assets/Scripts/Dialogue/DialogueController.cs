using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueController : MonoBehaviour
{
    public DialogueDataSO currentData;
    public bool canTalk = true;

    public void OpenDialogue()
    {
        canTalk = false;
        DialogueUI.Instance.UpdataDialogueData(currentData);
        DialogueUI.Instance.UpdateMainDialogue(currentData.dialoguePieces[0]);

        // 禁止移动和视角旋转
        PlayerInput.Instance.DisableMovementAndLook();

        // 保留 E 键交互事件继续有效
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void CloseDialogue()
    {
        canTalk = true;
        DialogueUI.Instance.dialoguePanel.SetActive(false);

        PlayerInput.Instance.EnableMovementAndLook();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
