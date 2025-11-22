using System.Linq;
using UnityEngine;

/// <summary>
/// 轮播图背景动画循环控制器。
/// 支持顺序播放多个动画状态，并在每个动画播放结束后自动重新开始。
/// </summary>
[DisallowMultipleComponent]
public class CarouselBackgroundLooper : MonoBehaviour
{
    [Header("Animator 引用")]
    [SerializeField] private Animator animator;

    [Header("动画状态配置")]
    [Tooltip("希望顺序播放的动画状态名，至少填写一个。")]
    [SerializeField] private string[] stateNames;

    [Tooltip("启用后在组件激活时立即播放动画。")]
    [SerializeField] private bool playOnEnable = true;

    [Tooltip("状态切换的平滑时间（秒）。0 表示立即切换。")]
    [SerializeField, Range(0f, 1f)] private float transitionDuration = 0.1f;

    [Tooltip("判断动画结束的阈值，1 表示完整播放。")]
    [SerializeField, Range(0.9f, 1.1f)] private float completionThreshold = 0.99f;

    private int[] _stateHashes = System.Array.Empty<int>();
    private int _currentIndex;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (stateNames != null && stateNames.Length > 0)
            _stateHashes = stateNames.Where(name => !string.IsNullOrWhiteSpace(name))
                                     .Select(Animator.StringToHash)
                                     .ToArray();
    }

    private void OnEnable()
    {
        if (playOnEnable)
            PlayCurrentState(true);
    }

    private void Update()
    {
        if (!CanProcess())
            return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.shortNameHash != _stateHashes[_currentIndex])
            return;

        // 如果动画本身已经配置为循环，则无需脚本干预
        if (stateInfo.loop)
            return;

        if (stateInfo.normalizedTime >= completionThreshold)
            PlayNextState();
    }

    private void PlayNextState()
    {
        _currentIndex = (_currentIndex + 1) % _stateHashes.Length;
        PlayCurrentState(false);
    }

    private void PlayCurrentState(bool restartCurrentIndex)
    {
        if (!CanProcess())
            return;

        if (restartCurrentIndex)
            _currentIndex = Mathf.Clamp(_currentIndex, 0, _stateHashes.Length - 1);

        if (transitionDuration > 0f)
            animator.CrossFade(_stateHashes[_currentIndex], transitionDuration, 0, 0f);
        else
            animator.Play(_stateHashes[_currentIndex], 0, 0f);
    }

    private bool CanProcess()
    {
        return animator != null &&
               animator.runtimeAnimatorController != null &&
               _stateHashes.Length > 0;
    }
}

