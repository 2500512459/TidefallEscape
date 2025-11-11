using TMPro;
using UnityEngine;

public class InteractHintUI : MonoSingleton<InteractHintUI>
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text hintText;

    protected override void Awake()
    {
        HideHint();
    }

    public void ShowHint(string text, string key = "E")
    {
        hintText.text = $"按 <color=#FFFF66>{key}</color> {text}";
        canvasGroup.alpha = 1f;
    }

    public void HideHint()
    {
        canvasGroup.alpha = 0f;
    }
}
