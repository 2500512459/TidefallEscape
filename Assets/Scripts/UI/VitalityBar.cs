using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 控制角色体力（活力）环形UI的脚本。
/// 会根据摄像机模式（第一/第三人称）自动调整UI布局，
/// 并在第三人称时可选择跟随角色的世界空间位置。
/// </summary>
public class VitalityBar : MonoBehaviour
{
    // ======================
    // 可配置参数
    // ======================

    [Header("填充动画参数")]
    [SerializeField] Image radialFillImage;      // 圆形体力条的填充图片
    [SerializeField] float fillDuration = 1f;    // 填充动画持续时间（秒）
    [SerializeField] CanvasGroup canvasGroup;    // 用于控制体力条显示/隐藏的透明度组

    [Header("摄像机模式布局响应")]
    [SerializeField] PlayerCamera playerCamera;  // 引用当前玩家使用的摄像机组件
    [SerializeField, Tooltip("第一人称模式下使用的UI布局")] 
    RectTransform firstPersonLayout;
    [SerializeField, Tooltip("第三人称模式下使用的UI布局（当不跟随世界目标时）")] 
    RectTransform thirdPersonLayout;
    [SerializeField, Tooltip("是否在第三人称下跟随角色位置")] 
    bool followPlayerInThirdPerson = true;
    [SerializeField, Tooltip("第三人称跟随时的世界偏移（相对于目标）")] 
    Vector3 thirdPersonWorldOffset = new Vector3(0.7f, 1.9f, 0f);
    [SerializeField, Tooltip("是否使用局部偏移（相对角色朝向）")] 
    bool thirdPersonOffsetInLocalSpace = true;
    [SerializeField, Tooltip("屏幕空间偏移，在计算世界投影后应用")] 
    Vector2 thirdPersonScreenOffset = new Vector2(0f, 90f);
    [SerializeField, Tooltip("第三人称模式下UI跟随平滑速度（0则立即跟随）")] 
    float thirdPersonFollowSmoothing = 12f;
    [SerializeField, Tooltip("从一种布局过渡到另一种布局的持续时间")] 
    float layoutTransitionDuration = 0.3f;
    [SerializeField, Tooltip("布局过渡使用的插值曲线")] 
    AnimationCurve layoutTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("第三人称距离缩放")]
    [SerializeField, Tooltip("是否启用随距离缩放")]
    bool enableDistanceScaling = true;
    [SerializeField, Tooltip("参考距离（在此距离下缩放为1倍基准大小）")]
    float referenceDistance = 5f;
    [SerializeField, Tooltip("最小缩放值")]
    float minScale = 0.3f;
    [SerializeField, Tooltip("最大缩放值")]
    float maxScale = 1.5f;

    // ======================
    // 内部状态
    // ======================

    private Coroutine fillCoroutine;             // 当前填充动画的协程
    private Transform thirdPersonFollowTarget;   // 第三人称跟随的目标（一般是玩家Transform）
    private RectTransform rectTransform;         // 当前UI元素的RectTransform引用
    private Canvas rootCanvas;                   // 根Canvas（用于屏幕空间计算）
    private RectTransform canvasRectTransform;   // 根Canvas的RectTransform
    private PlayerCamera.CameraMode currentCameraMode = PlayerCamera.CameraMode.FirstPerson; // 当前摄像机模式
    private bool subscribedToCamera;             // 是否已订阅摄像机事件
    private bool usingWorldFollow;               // 当前是否启用世界坐标跟随模式
    private RectTransformState initialLayoutState; // 初始UI布局状态
    private bool capturedInitialLayout;          // 是否成功捕获初始布局
    private Coroutine layoutTransitionCoroutine; // 布局过渡动画协程
    private Vector3 currentBaseScale = Vector3.one; // 当前模式的基准缩放（用于距离缩放计算）

    // ======================
    // RectTransform 状态结构体
    // 用于保存和恢复UI布局
    // ======================
    [System.Serializable]
    private struct RectTransformState
    {
        public Vector2 anchorMin, anchorMax, pivot, sizeDelta;
        public Vector3 anchoredPosition3D, localScale;

        /// <summary>
        /// 捕获当前RectTransform的状态
        /// </summary>
        public static RectTransformState Capture(RectTransform rect)
        {
            return new RectTransformState
            {
                anchorMin = rect.anchorMin,
                anchorMax = rect.anchorMax,
                pivot = rect.pivot,
                sizeDelta = rect.sizeDelta,
                anchoredPosition3D = rect.anchoredPosition3D,
                localScale = rect.localScale
            };
        }

        /// <summary>
        /// 将捕获的状态应用到指定RectTransform上
        /// </summary>
        public void Apply(RectTransform rect)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition3D = anchoredPosition3D;
            rect.localScale = localScale;
        }
    }

    // ======================
    // 体力条更新逻辑
    // ======================

    /// <summary>
    /// 更新体力条的填充进度（带动画）
    /// </summary>
    public void UpdateRadialProgressCircle(float valueToFill, float maxFill)
    {
        // 计算目标填充比例（0~1）
        float targetFillAmount = Mathf.Clamp01(maxFill <= 0f ? 0f : valueToFill / maxFill);

        // 若上一个动画仍在进行则终止
        if (fillCoroutine != null)
            StopCoroutine(fillCoroutine);

        // 启动新的填充动画
        fillCoroutine = StartCoroutine(AnimateFill(targetFillAmount));
    }

    /// <summary>
    /// 初始化摄像机依赖和跟随目标（在玩家实例化时调用）
    /// </summary>
    public void InitializeCameraDependencies(PlayerCamera camera, Transform followTarget)
    {
        // 如果依赖已相同则不重复初始化
        if (playerCamera == camera && thirdPersonFollowTarget == followTarget)
            return;

        // 清理旧引用
        UnsubscribeFromCamera();

        playerCamera = camera;
        thirdPersonFollowTarget = followTarget;

        // 重新订阅摄像机事件
        SubscribeToCamera();

        // 立即应用当前摄像机模式的UI布局
        ApplyCameraMode(playerCamera != null ? playerCamera.cameraMode : currentCameraMode, true);
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
            canvasRectTransform = rootCanvas.GetComponent<RectTransform>();

        // 捕获初始布局以便之后恢复
        if (rectTransform != null)
        {
            initialLayoutState = RectTransformState.Capture(rectTransform);
            capturedInitialLayout = true;
        }
    }

    private void OnEnable()
    {
        // 重新订阅摄像机事件并应用当前模式
        SubscribeToCamera();
        ApplyCameraMode(playerCamera != null ? playerCamera.cameraMode : currentCameraMode, true);
    }

    private void OnDisable()
    {
        // 组件禁用时取消事件订阅
        UnsubscribeFromCamera();
    }

    private void LateUpdate()
    {
        // 若当前未启用世界跟随模式则不更新
        if (!usingWorldFollow)
            return;

        // 若无跟随目标或RectTransform为空则跳过
        if (thirdPersonFollowTarget == null || rectTransform == null)
            return;

        // 尝试计算目标在Canvas中的局部位置
        if (!TryGetWorldFollowLocalPoint(out Vector2 localPoint))
            return;

        // 应用屏幕偏移
        localPoint += thirdPersonScreenOffset;
        Vector2 targetPosition = localPoint;

        // 平滑移动UI到目标点
        if (thirdPersonFollowSmoothing > 0f)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(
                rectTransform.anchoredPosition,
                targetPosition,
                Time.deltaTime * thirdPersonFollowSmoothing);
        }
        else
        {
            rectTransform.anchoredPosition = targetPosition;
        }

        // 处理距离缩放
        if (enableDistanceScaling)
        {
            UpdateDistanceScale();
        }
    }

    private void UpdateDistanceScale()
    {
        if (thirdPersonFollowTarget == null || playerCamera == null)
            return;

        Camera activeCamera = playerCamera.UnityCamera != null ? playerCamera.UnityCamera : Camera.main;
        if (activeCamera == null)
            return;

        // 计算跟随目标的世界位置（复用TryGetWorldFollowLocalPoint中的逻辑）
        Vector3 offset = thirdPersonWorldOffset;
        if (thirdPersonOffsetInLocalSpace)
            offset = thirdPersonFollowTarget.TransformDirection(thirdPersonWorldOffset);

        Vector3 targetWorldPos = thirdPersonFollowTarget.position + offset;

        // 计算距离
        float distance = Vector3.Distance(activeCamera.transform.position, targetWorldPos);
        
        // 防止除以零
        if (distance < 0.01f) distance = 0.01f;

        // 计算缩放比例
        float scaleMultiplier = referenceDistance / distance;
        scaleMultiplier = Mathf.Clamp(scaleMultiplier, minScale, maxScale);

        // 应用缩放（基于当前模式的基准缩放）
        rectTransform.localScale = currentBaseScale * scaleMultiplier;
    }

    // ======================
    // 填充动画协程
    // ======================
    private IEnumerator AnimateFill(float targetFillAmount)
    {
        float initialFillAmount = radialFillImage.fillAmount;
        float elapsedTime = 0f;

        // 在fillDuration时间内逐渐插值到目标值
        while (elapsedTime < fillDuration)
        {
            elapsedTime += Time.deltaTime;
            radialFillImage.fillAmount = Mathf.Lerp(initialFillAmount, targetFillAmount, elapsedTime / fillDuration);

            // 当体力满时隐藏UI，否则显示
            canvasGroup.alpha = (radialFillImage.fillAmount >= 0.99f) ? 0f : 1f;
            yield return null;
        }

        // 确保最终数值正确
        radialFillImage.fillAmount = targetFillAmount;
        canvasGroup.alpha = (radialFillImage.fillAmount >= 0.99f) ? 0f : 1f;
        fillCoroutine = null;
    }

    // ======================
    // 摄像机模式事件订阅
    // ======================
    private void SubscribeToCamera()
    {
        if (playerCamera == null || subscribedToCamera)
            return;

        // 注册摄像机模式切换事件
        playerCamera.CameraModeChanged += OnCameraModeChanged;
        subscribedToCamera = true;
    }

    private void UnsubscribeFromCamera()
    {
        if (playerCamera != null && subscribedToCamera)
            playerCamera.CameraModeChanged -= OnCameraModeChanged;

        subscribedToCamera = false;
    }

    // 摄像机模式切换时回调
    private void OnCameraModeChanged(PlayerCamera.CameraMode mode)
    {
        ApplyCameraMode(mode);
    }

    /// <summary>
    /// 根据摄像机模式更新UI布局
    /// </summary>
    private void ApplyCameraMode(PlayerCamera.CameraMode mode, bool force = false)
    {
        if (!force && currentCameraMode == mode)
            return;

        PlayerCamera.CameraMode previousMode = currentCameraMode;
        currentCameraMode = mode;

        // 获取对应模式的目标布局状态
        RectTransformState targetState = mode == PlayerCamera.CameraMode.FirstPerson
            ? GetFirstPersonTargetState()
            : GetThirdPersonTargetState();

        // 记录目标状态的缩放作为基准缩放
        currentBaseScale = targetState.localScale;

        // 若为第三人称并启用跟随，则激活world follow
        bool enableWorldFollow = mode == PlayerCamera.CameraMode.ThirdPerson &&
                                 followPlayerInThirdPerson &&
                                 thirdPersonFollowTarget != null;

        if (previousMode == PlayerCamera.CameraMode.ThirdPerson && mode == PlayerCamera.CameraMode.FirstPerson)
        {
            StartThirdToFirstPersonTransition(targetState);
            return;
        }

        StartLayoutTransition(targetState, enableWorldFollow);
    }

    // 特殊处理：第三人称切换到第一人称的过渡动画
    private void StartThirdToFirstPersonTransition(RectTransformState targetState)
    {
        if (rectTransform == null)
            return;

        if (layoutTransitionCoroutine != null)
            StopCoroutine(layoutTransitionCoroutine);

        usingWorldFollow = false;

        Vector3 worldPosition = rectTransform.position;

        rectTransform.anchorMin = targetState.anchorMin;
        rectTransform.anchorMax = targetState.anchorMax;
        rectTransform.pivot = targetState.pivot;
        rectTransform.sizeDelta = targetState.sizeDelta;
        rectTransform.localScale = targetState.localScale;

        rectTransform.position = worldPosition;

        Vector3 startPosition = rectTransform.anchoredPosition3D;

        if (layoutTransitionDuration <= 0.0001f)
        {
            rectTransform.anchoredPosition3D = targetState.anchoredPosition3D;
            layoutTransitionCoroutine = null;
            return;
        }

        layoutTransitionCoroutine = StartCoroutine(ThirdToFirstPersonRoutine(startPosition, targetState.anchoredPosition3D));
    }

    private IEnumerator ThirdToFirstPersonRoutine(Vector3 startPosition, Vector3 targetPosition)
    {
        float elapsed = 0f;

        while (elapsed < layoutTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / layoutTransitionDuration);
            t = layoutTransitionCurve != null ? layoutTransitionCurve.Evaluate(t) : t;
            Vector3 lerpPosition = Vector3.Lerp(startPosition, targetPosition, t);
            lerpPosition.z = targetPosition.z;
            rectTransform.anchoredPosition3D = lerpPosition;
            yield return null;
        }

        rectTransform.anchoredPosition3D = targetPosition;
        layoutTransitionCoroutine = null;
    }

    // 启动布局过渡动画
    private void StartLayoutTransition(RectTransformState targetState, bool enableWorldFollow)
    {
        if (rectTransform == null)
            return;

        // 停止已有过渡动画
        if (layoutTransitionCoroutine != null)
            StopCoroutine(layoutTransitionCoroutine);

        var startState = RectTransformState.Capture(rectTransform);

        // 若过渡时间极短则直接应用
        if (layoutTransitionDuration <= 0.0001f)
        {
            targetState.Apply(rectTransform);
            usingWorldFollow = enableWorldFollow;
            return;
        }

        usingWorldFollow = false;
        layoutTransitionCoroutine = StartCoroutine(LayoutTransitionRoutine(startState, targetState, enableWorldFollow));
    }

    // ======================
    // 布局插值过渡逻辑
    // ======================
    private IEnumerator LayoutTransitionRoutine(RectTransformState startState, RectTransformState targetState, bool enableWorldFollow)
    {
        float elapsed = 0f;

        while (elapsed < layoutTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / layoutTransitionDuration);
            t = layoutTransitionCurve != null ? layoutTransitionCurve.Evaluate(t) : t;
            ApplyInterpolatedState(startState, targetState, t, enableWorldFollow);
            yield return null;
        }

        // 结束时强制应用目标布局
        targetState.Apply(rectTransform);

        // 若启用world follow，更新到正确位置
        if (enableWorldFollow && TryGetWorldFollowLocalPoint(out Vector2 finalPoint))
        {
            finalPoint += thirdPersonScreenOffset;
            Vector3 position = rectTransform.anchoredPosition3D;
            position.x = finalPoint.x;
            position.y = finalPoint.y;
            rectTransform.anchoredPosition3D = position;
        }

        layoutTransitionCoroutine = null;
        usingWorldFollow = enableWorldFollow;
    }

    // 插值应用布局状态
    private void ApplyInterpolatedState(RectTransformState startState, RectTransformState targetState, float t, bool worldFollow)
    {
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = Vector2.Lerp(startState.anchorMin, targetState.anchorMin, t);
        rectTransform.anchorMax = Vector2.Lerp(startState.anchorMax, targetState.anchorMax, t);
        rectTransform.pivot = Vector2.Lerp(startState.pivot, targetState.pivot, t);
        rectTransform.sizeDelta = Vector2.Lerp(startState.sizeDelta, targetState.sizeDelta, t);
        rectTransform.localScale = Vector3.Lerp(startState.localScale, targetState.localScale, t);

        Vector3 startPos = startState.anchoredPosition3D;
        Vector3 targetPos = targetState.anchoredPosition3D;

        // 若启用世界跟随，则实时更新目标位置
        if (worldFollow && TryGetWorldFollowLocalPoint(out Vector2 worldPoint))
        {
            worldPoint += thirdPersonScreenOffset;
            targetPos.x = worldPoint.x;
            targetPos.y = worldPoint.y;
        }

        rectTransform.anchoredPosition3D = Vector3.Lerp(startPos, targetPos, t);
    }

    // ======================
    // 获取不同模式下的布局状态
    // ======================

    // 第一人称布局：优先使用指定layout，否则使用初始布局
    private RectTransformState GetFirstPersonTargetState()
    {
        if (firstPersonLayout != null)
            return RectTransformState.Capture(firstPersonLayout);
        if (capturedInitialLayout)
            return initialLayoutState;
        return RectTransformState.Capture(rectTransform);
    }

    // 第三人称布局：优先使用指定layout，否则使用中心点布局
    private RectTransformState GetThirdPersonTargetState()
    {
        if (thirdPersonLayout != null)
            return RectTransformState.Capture(thirdPersonLayout);

        var state = new RectTransformState
        {
            anchorMin = new Vector2(0.5f, 0.5f),
            anchorMax = new Vector2(0.5f, 0.5f),
            pivot = new Vector2(0.5f, 0.5f),
            sizeDelta = capturedInitialLayout ? initialLayoutState.sizeDelta : rectTransform.sizeDelta,
            anchoredPosition3D = Vector3.zero,
            localScale = capturedInitialLayout ? initialLayoutState.localScale : rectTransform.localScale
        };
        return state;
    }

    // ======================
    // 将世界坐标转换为UI坐标
    // ======================
    private bool TryGetWorldFollowLocalPoint(out Vector2 localPoint)
    {
        localPoint = Vector2.zero;

        if (thirdPersonFollowTarget == null || rectTransform == null)
            return false;

        // 确保Canvas引用存在
        if (canvasRectTransform == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
                return false;
            canvasRectTransform = rootCanvas.GetComponent<RectTransform>();
        }

        // 使用PlayerCamera的真实Camera或主相机
        Camera activeCamera = playerCamera != null ? playerCamera.UnityCamera : Camera.main;
        if (activeCamera == null)
            return false;

        // 计算跟随目标在世界空间的位置（带偏移）
        Vector3 offset = thirdPersonWorldOffset;
        if (thirdPersonOffsetInLocalSpace)
            offset = thirdPersonFollowTarget.TransformDirection(thirdPersonWorldOffset);

        Vector3 worldPos = thirdPersonFollowTarget.position + offset;

        // 将世界坐标转换为屏幕坐标
        Vector3 screenPos = activeCamera.WorldToScreenPoint(worldPos);

        // 若目标在摄像机背后（z<0）则不显示
        if (screenPos.z < 0f)
            return false;

        // 确定Canvas是否需要Camera参与坐标转换
        bool canvasNeedsCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay;

        // 将屏幕坐标转换为Canvas局部坐标
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            screenPos,
            canvasNeedsCamera ? activeCamera : null,
            out localPoint))
        {
            return false;
        }
        return true;
    }

    private Camera GetCanvasCamera()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas == null)
            return playerCamera != null ? playerCamera.UnityCamera : Camera.main;

        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (rootCanvas.worldCamera != null)
            return rootCanvas.worldCamera;

        return playerCamera != null ? playerCamera.UnityCamera : Camera.main;
    }
}
