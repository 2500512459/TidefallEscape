using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class ShowTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RewardItem currentItemUI;

    void Awake()
    {
        currentItemUI = GetComponent<RewardItem>();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        QuestUI.Instance.tooltip.gameObject.SetActive(true);
        QuestUI.Instance.tooltip.SetupItemTooltip(currentItemUI.currentItem.item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        QuestUI.Instance.tooltip.gameObject.SetActive(false);
    }

}
