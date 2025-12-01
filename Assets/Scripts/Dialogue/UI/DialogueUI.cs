using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class DialogueUI : MonoSingleton<DialogueUI>
{
    [Header("基本组件")]
    // 显示说话者姓名
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI mainText;
    public Button nextButton;
    public GameObject dialoguePanel;
    [Header("选项")]
    public RectTransform optionPanel;
    public OptionUI optionPrefab;
    [Header("对话数据")]
    public DialogueDataSO currentData;
    private DialogueController currentDialogueController; // 当前活动的对话控制器

    int currentIndex = 0;

    protected override void Awake()
    {
        base.Awake();
        nextButton.onClick.AddListener(ContinueDialogue);

    }

    void ContinueDialogue()
    {
        if (currentIndex >= currentData.dialoguePieces.Count)
        {
            // 对话结束，关闭对话并恢复玩家控制
            if (currentDialogueController != null)
            {
                currentDialogueController.CloseDialogue();
            }
            dialoguePanel.SetActive(false);
            currentIndex = 0;
            currentDialogueController = null;
            return;
        }
        DialoguePiece nextPiece = currentData.dialoguePieces[currentIndex];
        UpdateMainDialogue(nextPiece);
    }
    public void UpdataDialogueData(DialogueDataSO data, DialogueController controller = null)
    {
        currentData = data;
        currentIndex = 0;
        currentDialogueController = controller; // 保存对话控制器引用
    }

    public void UpdateMainDialogue(DialoguePiece piece)
    {
        dialoguePanel.SetActive(true);
        currentIndex++;

        // 更新说话者姓名（如果有）
        if (speakerNameText != null)
            speakerNameText.text = string.IsNullOrEmpty(piece.characterName) ? string.Empty : piece.characterName;
        
        string text = piece.text;

        // 根据字符数量计算显示时间（例如每个字符0.03秒）
        float duration = Mathf.Clamp(text.Length * 0.03f, 0.5f, 2f);

        var t = DOTween.To(() => string.Empty, value => mainText.text = value, text, duration).SetEase(Ease.Linear);
        t.SetOptions(true);

        if (piece.options.Count == 0 && currentData.dialoguePieces.Count > 0)
        {
            nextButton.gameObject.SetActive(true); //启用按钮
            ToggleNextButtonIndicator(true);

        }
        else
        {
            //nextButton.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(false);    //禁用按钮
            ToggleNextButtonIndicator(false);
        }

        // 创建options
        CreateOptions(piece);
    }
    
    private void CreateOptions(DialoguePiece piece)
    {
        // 清除现有选项
        if (optionPanel.childCount > 0)
        {
            for (int i = 0; i < optionPanel.childCount; i++)
            {
                Destroy(optionPanel.GetChild(i).gameObject);
            }
        }


        // 检查optionPrefab是否已被销毁
        if (optionPrefab == null)
        {
            Debug.LogError("OptionUI prefab is missing!");
                return;
        }
        // 创建新选项
        for (int i = 0; i < piece.options.Count; i++)
        {
            var option = Instantiate(optionPrefab, optionPanel);
            option.UpdateOption(piece, piece.options[i]);
        }
        
    }

    /// <summary>
    /// 结束对话并恢复玩家控制
    /// </summary>
    public void EndDialogue()
    {
        if (currentDialogueController != null)
        {
            currentDialogueController.CloseDialogue();
        }
        dialoguePanel.SetActive(false);
        currentIndex = 0;
        currentDialogueController = null;
    }

    /// <summary>
    /// 安全切换下一步箭头或文本的可见性
    /// </summary>
    /// <param name="active"></param>
    private void ToggleNextButtonIndicator(bool active)
    {
        if (nextButton.transform.childCount > 0)
        {
            nextButton.transform.GetChild(0).gameObject.SetActive(active);
        }
    }
}
