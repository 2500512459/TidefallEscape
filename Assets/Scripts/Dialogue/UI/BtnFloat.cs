using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 让按钮在垂直方向轻微浮动，可直接挂在 Button 或任意 RectTransform 上。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class BtnFloat : MonoBehaviour
{
    [Header("浮动设置")]
    public float amplitude = 10f;     // 上下位移幅度（像素）
    public float duration = 1.5f;     // 单次往返时间
    public Ease easeType = Ease.InOutSine;

    private RectTransform rectTransform;
    private Tweener floatTween;
    private Vector2 initialAnchoredPos;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        initialAnchoredPos = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        StartFloat();
    }

    private void OnDisable()
    {
        StopFloat();
    }

    private void StartFloat()
    {
        StopFloat(); // 确保只有一个 tween
        floatTween = rectTransform
            .DOAnchorPosY(initialAnchoredPos.y + amplitude, duration / 2f)
            .SetEase(easeType)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void StopFloat()
    {
        if (floatTween != null && floatTween.IsActive())
        {
            floatTween.Kill();
        }
        rectTransform.anchoredPosition = initialAnchoredPos;
    }
}

