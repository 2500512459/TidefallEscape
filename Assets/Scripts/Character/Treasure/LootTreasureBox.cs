using System.Collections;
using UnityEngine;

/// <summary>
/// 掉落宝箱：继承基础 TreasureBox，首次开启后旋转宝箱盖的 Transform。
/// </summary>
public class LootTreasureBox : TreasureBox
{
    [Header("宝箱盖动画")]
    [Tooltip("需要旋转的宝箱盖 Transform")]
    [SerializeField] private Transform lidTransform;
    [Tooltip("打开后宝箱盖的局部欧拉角")]
    [SerializeField] private Vector3 openedLocalEuler = new Vector3(-95f, 0f, 0f);
    [Tooltip("宝箱盖旋转时长（秒）")]
    [SerializeField] private float openDuration = 0.6f;
    [Tooltip("动画插值曲线（0~1）")]
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Quaternion closedLocalRotation;
    private Coroutine lidCoroutine;
    private bool cachedClosedRotation;

    protected override void Start()
    {
        base.Start();
        CacheClosedRotation();
    }

    public override void TryOpen()
    {
        bool wasOpened = opened;
        base.TryOpen();

        if (!wasOpened && opened)
            RotateLidOnce();
    }

    private void CacheClosedRotation()
    {
        if (lidTransform == null)
            return;

        closedLocalRotation = lidTransform.localRotation;
        cachedClosedRotation = true;
    }

    private void RotateLidOnce()
    {
        if (lidTransform == null)
            return;

        if (!cachedClosedRotation)
            CacheClosedRotation();

        if (lidCoroutine != null)
            StopCoroutine(lidCoroutine);

        if (openDuration <= 0f)
        {
            lidTransform.localRotation = Quaternion.Euler(openedLocalEuler);
            return;
        }

        lidCoroutine = StartCoroutine(RotateLidRoutine());
    }

    private IEnumerator RotateLidRoutine()
    {
        Quaternion startRot = closedLocalRotation;
        Quaternion targetRot = Quaternion.Euler(openedLocalEuler);

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            float eased = openCurve != null ? openCurve.Evaluate(t) : t;
            lidTransform.localRotation = Quaternion.Slerp(startRot, targetRot, eased);
            yield return null;
        }

        lidTransform.localRotation = targetRot;
        lidCoroutine = null;
    }
}
