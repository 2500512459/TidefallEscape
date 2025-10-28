using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TreasureHintUI : MonoBehaviour
{
    [Header("UI���")]
    public CanvasGroup canvasGroup;
    public RectTransform rectTransform;

    [Header("��������")]
    public float heightOffset = 3f;  // �뱦�䶥����ƫ��
    public float lookLerpSpeed = 5f;   // UI�����������ƽ���ٶ�

    private Tween currentTween;
    private Transform target;          // ����Transform
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

        // ��UI����Ŀ��λ�ã���������ת��
        transform.position = target.position + Vector3.up * heightOffset;

        // ƽ�����������
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
