using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Evacuation : MonoBehaviour
{
    [Header("UI设置")]
    [Tooltip("需要显示的剩余时间画布")]
    public GameObject RemainingTimePanel;

    private void Start()
    {
        var remainingTimeCanvas = GameObject.Find("RemainingTime Canvas");
        if(remainingTimeCanvas != null)
        {
            RemainingTimePanel = remainingTimeCanvas.transform.Find("RemainingTime")?.gameObject;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 检测是否是 Player 进入触发器范围
        if (other.GetComponent<Player>() != null)
        {
            // 启用 RemainingTimePanel，开始倒计时
            if (RemainingTimePanel != null)
            {
                RemainingTimePanel.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 检测是否是 Player 离开触发器范围
        if (other.GetComponent<Player>() != null)
        {
            // 关闭 RemainingTimePanel
            if (RemainingTimePanel != null)
            {
                RemainingTimePanel.SetActive(false);
            }
        }
    }
}
