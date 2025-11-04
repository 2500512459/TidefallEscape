using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ItemTooltip : MonoBehaviour
{
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;

    RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetupItemTooltip(ItemDataSO itemData)
    {
        itemNameText.text = itemData.itemName;
        itemDescriptionText.text = itemData.description;
    }

    void OnEnable()
    {
        UpdataPosition();
    }
    void Update()
    {
        UpdataPosition();
    }
    
    public void UpdataPosition()
    {
        Vector3 mousePos = Input.mousePosition;

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        float width = corners[3].x - corners[0].x;
        float height = corners[1].y - corners[0].y;

        if (mousePos.y < height)
            rectTransform.position = mousePos + Vector3.up * height * 0.6f;
        else if (Screen.width - mousePos.x > width)
            rectTransform.position = mousePos + Vector3.left * width * 0.6f;
        else
            rectTransform.position = mousePos + Vector3.right * width * 0.6f;
    }
}
