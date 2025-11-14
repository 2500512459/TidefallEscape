using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 任务要求UI组件，用于在界面上显示任务要求信息
/// </summary>
public class QuestRequirement : MonoBehaviour
{
    public TextMeshProUGUI requireName;     // 要求名称文本
    public TextMeshProUGUI requireNumber;   // 要求数量文本

    /// <summary>
    /// 初始化UI组件引用
    /// </summary>
    private void Awake()
    {
        requireName = GetComponent<TextMeshProUGUI>();
        requireNumber = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// 设置任务要求显示内容
    /// </summary>
    /// <param name="name">要求名称</param>
    /// <param name="requireAmount">需要完成的数量</param>
    /// <param name="currentAmount">当前已完成的数量</param>
    public void SetupRequirement(string name, int requireAmount, int currentAmount, bool showCurrentProgress = true)
    {
        requireName.text = name;
        if (showCurrentProgress)
        {
            requireNumber.text = currentAmount.ToString() + " / " + requireAmount.ToString();
        }
        else
        {
            requireNumber.text = requireAmount.ToString();
        }
    }
}