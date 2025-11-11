using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseInteractable : MonoBehaviour, IInteractable
{
    [Header("交互提示文字")]
    [SerializeField] protected string hintText = "交互";
    [SerializeField] protected string key = "E";
    public string HintText => hintText;
    public string Key => key;
    public Transform Transform => transform;

    public virtual void Interact(Character player)
    {
        InteractHintUI.Instance.HideHint();
    }

    public virtual void OnFocus(Character player)
    {
        InteractHintUI.Instance.ShowHint(hintText, key);
    }

    public virtual void OnLoseFocus(Character player)
    {
        InteractHintUI.Instance.HideHint();
    }
}
