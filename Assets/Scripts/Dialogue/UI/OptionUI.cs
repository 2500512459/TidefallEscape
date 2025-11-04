using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionUI : MonoBehaviour
{
    public TextMeshProUGUI optionText;
    private Button optionButton;
    private DialoguePiece currentPiece;

    private string nextPieceID;
    private bool takeQuest;

    void Awake()
    {
        optionButton = GetComponent<Button>();
        optionButton.onClick.AddListener(OnOptionClicked);
    }

    public void UpdateOption(DialoguePiece piece, DialogueOption option)
    {
        currentPiece = piece;
        optionText.text = option.text;
        nextPieceID = option.targetID;
        takeQuest = option.takeQuest;
    }

    public void OnOptionClicked()
    {
        if (currentPiece.quest != null)
        {
            var newTask = new QuestManager.QuestTask
            {
                questData = Instantiate(currentPiece.quest)
            };

            if (takeQuest)
            {
                //添加到任务列表
                //检查是否已有该任务
                if (QuestManager.Instance.HaveQuest(newTask.questData))
                {
                    //判断是否完成任务
                }
                else
                {
                    //没有该任务，添加新任务
                    QuestManager.Instance.tasks.Add(newTask);
                    QuestManager.Instance.GetQuestTask(newTask.questData).IsStarted = true;
                    //检查任务物品
                    foreach (var require in newTask.questData.GetQuestRequireNames())
                    {
                        InventoryManager.Instance.CheckQuestItem(require);
                    }
                }
            }
        }


        if (nextPieceID == "")
        {
            DialogueUI.Instance.dialoguePanel.SetActive(false);
            return;
        }
        else
        {
            Debug.Log(nextPieceID);
            DialogueUI.Instance.UpdateMainDialogue(DialogueUI.Instance.currentData.dialogueIndex[nextPieceID]);
        }
    }
}
