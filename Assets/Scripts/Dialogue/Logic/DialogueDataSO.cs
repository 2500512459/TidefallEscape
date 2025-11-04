using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Dialogue/DialogueDataSO")]
public class DialogueDataSO : ScriptableObject
{
    public List<DialoguePiece> dialoguePieces = new List<DialoguePiece>();
    public Dictionary<string, DialoguePiece> dialogueIndex = new Dictionary<string, DialoguePiece>();

#if UNITY_EDITOR
    // OnValidate 是当前脚本被保存时或者在编辑器中修改时调用的函数
    private void OnValidate()
    {
        dialogueIndex.Clear();
        foreach (var piece in dialoguePieces)
        {
            if (!dialogueIndex.ContainsKey(piece.ID))
            {
                dialogueIndex.Add(piece.ID, piece);
            }
        }
    }
#endif

    /// <summary>
    /// 获取对话中的任务
    /// </summary>
    /// <returns></returns>
    public QuestDataSO GetQuest()
    {
        QuestDataSO currentQuest = null;
        foreach (var piece in dialoguePieces)
        {
            if (piece.quest != null)
            {
                currentQuest = piece.quest;
                break;
            }
        }
        return currentQuest;
    }
}
