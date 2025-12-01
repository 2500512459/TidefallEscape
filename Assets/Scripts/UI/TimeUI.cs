using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimeUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timeText;
    // 移除本地的 elapsedTime，改为直接使用 PlayerDataManager 中的数据
    // float elapsedTime = 0f;

    void Update()
    {
        // 确保 PlayerDataManager 存在
        if (PlayerDataManager.Instance != null)
        {
            // 在这里累加时间到全局管理器中
            PlayerDataManager.Instance.CurrentMatchTime += Time.deltaTime;
            
            float currentTime = PlayerDataManager.Instance.CurrentMatchTime;
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            
            if (timeText != null)
            {
                timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }
    }
}
