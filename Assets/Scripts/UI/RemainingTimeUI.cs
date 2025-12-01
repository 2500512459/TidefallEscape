using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RemainingTimeUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI remainingTimeText;
    [SerializeField] float remainingTime;
    
    // 存储初始时间以便重置
    private float initialTime;
    private bool isTimerEnded = false;

    private void Awake()
    {
        // 在Awake中记录初始设定的时间（Inspector中设置的值）
        initialTime = remainingTime;
    }

    private void OnEnable()
    {
        // 每次启用时，重置时间为初始值，并重置结束标记
        ResetTimer();
    }

    private void ResetTimer()
    {
        remainingTime = initialTime;
        isTimerEnded = false;
        UpdateUI();
    }

    void Update()
    {
        if (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
        }
        else
        {
            remainingTime = 0f;
            if (!isTimerEnded)
            {
                isTimerEnded = true;
                // 倒计时结束，调用 LoadManager 切换场景
                if (LoadManager.Instance != null)
                {
                    LoadManager.Instance.LoadScene("HomeScene");
                }
            }
        }
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        if (remainingTimeText != null)
        {
            remainingTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}
