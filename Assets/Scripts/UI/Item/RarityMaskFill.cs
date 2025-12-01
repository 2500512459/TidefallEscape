using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 根据物品稀有度播放遮罩填充动画的组件
/// </summary>
[RequireComponent(typeof(Image))]
public class RarityMaskFill : MonoBehaviour
{
    [System.Serializable]
    public struct RarityFillConfig
    {
        public ItemRarity rarity;
        [Min(0f)] public float duration;
    }

    [Header("遮罩动画配置")]
    [SerializeField] private Image maskImage;
    [SerializeField, Min(0f)] private float defaultDuration = 1f;
    [SerializeField] private AnimationCurve fillCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private RarityFillConfig[] rarityDurations;
    [SerializeField, Tooltip("动画结束后是否禁用整个GameObject（推荐）")] 
    private bool disableGameObjectOnComplete = true;

    private Coroutine fillRoutine;
    private bool originalRaycastTarget;

    /// <summary>
    /// 动画完成时的回调事件
    /// </summary>
    public System.Action OnFillComplete;

    /// <summary>
    /// 检查是否正在播放动画
    /// </summary>
    public bool IsPlaying => fillRoutine != null;

    private void Awake()
    {
        if (maskImage == null)
            maskImage = GetComponent<Image>();

        if (maskImage != null)
            originalRaycastTarget = maskImage.raycastTarget;

        HideMaskImmediate();
    }

    /// <summary>
    /// 根据稀有度播放一次填充动画
    /// </summary>
    public void Play(ItemRarity rarity)
    {
        if (maskImage == null)
            return;

        // 确保GameObject激活
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
            
            // 如果仍然未激活（例如父物体被禁用了），则无法启动协程，直接返回并触发完成回调
            if (!gameObject.activeInHierarchy)
            {
                // 恢复状态以防万一
                HideMaskImmediate();
                
                OnFillComplete?.Invoke();
                OnFillComplete = null;
                return;
            }
        }

        if (fillRoutine != null)
            StopCoroutine(fillRoutine);

        // 启用遮罩并设置初始状态
        maskImage.enabled = true;
        maskImage.fillAmount = 0f;
        maskImage.raycastTarget = true; // 动画期间允许射线检测（如果需要）
        
        fillRoutine = StartCoroutine(FillRoutine(GetDuration(rarity)));
    }

    /// <summary>
    /// 立即停止动画并隐藏遮罩
    /// </summary>
    public void StopAndHide()
    {
        if (fillRoutine != null)
            StopCoroutine(fillRoutine);

        fillRoutine = null;
        OnFillComplete = null; // 清除回调，避免意外触发
        HideMaskImmediate();
    }

    private void HideMaskImmediate()
    {
        if (maskImage == null)
            return;

        maskImage.fillAmount = 0f;
        maskImage.enabled = false;
        maskImage.raycastTarget = originalRaycastTarget; // 恢复原始射线检测设置

        // 如果配置为禁用GameObject，则禁用整个对象
        if (disableGameObjectOnComplete)
            gameObject.SetActive(false);
    }

    private IEnumerator FillRoutine(float duration)
    {
        float safeDuration = Mathf.Max(0.0001f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            t = fillCurve != null ? fillCurve.Evaluate(t) : t;
            maskImage.fillAmount = t;
            yield return null;
        }

        // 动画完成：隐藏遮罩并禁用
        maskImage.fillAmount = 1f;
        maskImage.enabled = false;
        maskImage.raycastTarget = originalRaycastTarget; // 恢复原始射线检测设置
        fillRoutine = null;

        // 如果配置为禁用GameObject，则禁用整个对象
        if (disableGameObjectOnComplete)
            gameObject.SetActive(false);

        // 通知动画完成
        OnFillComplete?.Invoke();
        OnFillComplete = null; // 清除回调
    }

    private float GetDuration(ItemRarity rarity)
    {
        if (rarityDurations != null)
        {
            for (int i = 0; i < rarityDurations.Length; i++)
            {
                if (rarityDurations[i].rarity == rarity)
                    return Mathf.Max(0f, rarityDurations[i].duration);
            }
        }
        return Mathf.Max(0f, defaultDuration);
    }
}

