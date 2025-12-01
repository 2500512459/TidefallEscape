using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialoguePiece
{
    public string ID;
    // 角色姓名（用于替代原来的头像 Sprite）
    public string characterName;
    [TextArea]
    public string text;
    public QuestDataSO quest;

    public List<DialogueOption> options = new List<DialogueOption>();
}
