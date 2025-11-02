using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TreasureHintUI : MonoBehaviour
{
    [Header("UI组件")]
    public CanvasGroup canvasGroup;
    public RectTransform rectTransform;

    [Header("浮动参数")]
    public float heightOffset = 3f;  // 离宝箱顶部的偏移
    public float lookLerpSpeed = 5f;   // UI朝向摄像机的平滑速度

    private Tween currentTween;
    private Transform target;          // 宝箱Transform
    private Camera mainCam;

    public void Init(Transform followTarget)
    {
        target = followTarget;
    }

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        canvasGroup.alpha = 0;
        rectTransform.localScale = Vector3.zero;
        mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if (target == null || mainCam == null) return;

        // 让UI跟随目标位置（不跟随旋转）
        transform.position = target.position + Vector3.up * heightOffset;

        // 平滑朝向摄像机
        Quaternion targetRot = Quaternion.LookRotation(transform.position - mainCam.transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lookLerpSpeed);
    }

    public void ShowUI()
    {
        currentTween?.Kill();
        gameObject.SetActive(true);

        canvasGroup.alpha = 0;
        rectTransform.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence();
        seq.Append(rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack))
           .Join(canvasGroup.DOFade(1, 0.3f))
           .OnComplete(() => currentTween = null);

        currentTween = seq;
    }

    public void HideUI()
    {
        currentTween?.Kill();

        Sequence seq = DOTween.Sequence();
        seq.Append(canvasGroup.DOFade(0, 0.25f))
           .Join(rectTransform.DOScale(Vector3.zero, 0.25f))
           .OnComplete(() =>
           {
               currentTween = null;
               gameObject.SetActive(false);
           });

        currentTween = seq;
    }
}
