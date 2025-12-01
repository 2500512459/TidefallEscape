using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_Quest : BaseInteractable
{
    private DialogueController dialogueController;

    void Awake()
    {
        dialogueController = GetComponent<DialogueController>();
    }
    public override void Interact(Character player)
    {
        if (dialogueController.canTalk)
        {
            dialogueController.OpenDialogue();
            InteractHintUI.Instance.HideHint();
        }
        else
        {
            dialogueController.CloseDialogue();
            InteractHintUI.Instance.ShowHint(hintText, key);
        }
    }
}
