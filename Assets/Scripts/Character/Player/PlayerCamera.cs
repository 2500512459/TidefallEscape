using System;
using System.Collections;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public event Action<CameraMode> CameraModeChanged;

    public Transform orientation;   // 移动方向

    [Header("摄像机模式")]
    public CameraMode cameraMode = CameraMode.FirstPerson;
    public bool IsTransitioning { get; private set; }
    
    [Header("切换参数")]
    public float transitionDuration = 1.0f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("第一人称参数")]
    public float sensX;
    public float sensY;
    public Transform cameraPosition;            // 相机位置
    public Transform FirstPersonAimPosition;   // 第一人称瞄准位置
    public Transform ThirdPersonAimPosition;   // 第三人称瞄准位置
    public Transform CrewmanCameraPosition;    // Crewman摄像机位置
    public Transform LookoutCameraPosition;    // Lookout摄像机位置
    public Transform CaptainCameraPosition;    // Captain摄像机位置
    public Transform ShipwrightCameraPosition;    // Shipwright摄像机位置
    
    [Header("第三人称参数")]
    public float rotateSpeed = 1.0f;
    public float scrollSpeed = 3.0f;
    public float lookRotateX = 60f;
    public float lookRotateY = 180f;
    public float lookDistance = 20f;
    public float minLookDistance = 2f;
    public float maxLookDistance = 16f;
    
    [Header("开船视角参数")]
    public float sailingMinLookDistance = 4f;
    public float sailingMaxLookDistance = 100f;

    public Transform target;            // 摄像机目标

    [Header("第一人称遮挡设置")]
    public Renderer[] playerBodyRenderers; // 需要在第一人称隐藏的渲染器
    
    [Header("开船状态")]
    public bool isSailing = false; // 是否在开船状态
    
    // 私有变量
    private float xRotation;
    private float yRotation;
    private Vector3 offset;
    private Coroutine transitionCoroutine;
    private Camera cachedCamera;

    public Camera UnityCamera => cachedCamera != null ? cachedCamera : Camera.main;
    
    public enum CameraMode
    {
        FirstPerson,
        ThirdPerson,
        AimPerson
    }

    void Awake()
    {
        cachedCamera = GetComponent<Camera>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        cameraMode = CameraMode.FirstPerson;
        InitializeCameraPosition();
        UpdatePlayerBodyVisibility();
        CameraModeChanged?.Invoke(cameraMode);

        if(PlayerDataManager.Instance.SelectedProfession == ProfessionType.Crewman)
        {
            cameraPosition = CrewmanCameraPosition;
            target = CrewmanCameraPosition;
        }
        else if(PlayerDataManager.Instance.SelectedProfession == ProfessionType.Lookout)
        {
            cameraPosition = LookoutCameraPosition;
            target = LookoutCameraPosition;
        }
        else if(PlayerDataManager.Instance.SelectedProfession == ProfessionType.Captain)
        {
            cameraPosition = CaptainCameraPosition;
            target = CaptainCameraPosition;
        }
        else if(PlayerDataManager.Instance.SelectedProfession == ProfessionType.Shipwright)
        {
            cameraPosition = ShipwrightCameraPosition;
            target = ShipwrightCameraPosition;
        }
    }

    void Update()
    {
        if (IsTransitioning) return;
        
        // 根据当前模式执行不同的更新逻辑
        switch (cameraMode)
        {
            case CameraMode.FirstPerson:
                UpdateFirstPerson();
                break;
            case CameraMode.ThirdPerson:
                UpdateThirdPerson();
                break;
        }
    }


    #region 第一人称逻辑
    private void UpdateFirstPerson()
    {
        float mouseX = PlayerInput.Instance.inputLook.x * sensX;
        float mouseY = PlayerInput.Instance.inputLook.y * sensY;
        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);

        transform.position = cameraPosition.position;
    }
    #endregion

    #region 第三人称逻辑
    private void UpdateThirdPerson()
    {
        // 如果在开船状态，必须按下RotatePressed才能旋转视角
        if (isSailing && !PlayerInput.Instance.RotatePressed)
        {
            // 不开船时仍然更新距离（允许缩放）
            UpdateDistance();
            
            // 保持当前视角不变，只更新位置
            lookRotateX = Mathf.Clamp(lookRotateX, 60, 80);
            Quaternion currentRot = Quaternion.Euler(lookRotateX, lookRotateY, 0);
            offset = currentRot * Vector3.forward * lookDistance;
            transform.position = target.transform.position - offset;
            transform.LookAt(target);
            
            // 使用摄像机的水平方向来更新orientation
            Vector3 currentCameraForward = transform.forward;
            currentCameraForward.y = 0; // 忽略Y轴，只保留水平方向
            if (currentCameraForward != Vector3.zero)
            {
                orientation.rotation = Quaternion.LookRotation(currentCameraForward.normalized);
            }
            return;
        }
        
        float verticalDelta = PlayerInput.Instance.inputLook.y * rotateSpeed;
        float horizontal = PlayerInput.Instance.inputLook.x * rotateSpeed;
        lookRotateX -= verticalDelta;
        lookRotateY += horizontal;
        UpdateDistance();

        lookRotateX = Mathf.Clamp(lookRotateX, 60, 80);
        Quaternion rot = Quaternion.Euler(lookRotateX, lookRotateY, 0);
        offset = rot * Vector3.forward * lookDistance;

        transform.position = target.transform.position - offset;
        transform.LookAt(target);

        // 使用摄像机的水平方向来更新orientation
        Vector3 cameraForward = transform.forward;
        cameraForward.y = 0; // 忽略Y轴，只保留水平方向
        if (cameraForward != Vector3.zero)
        {
            orientation.rotation = Quaternion.LookRotation(cameraForward.normalized);
        }
    }

    void UpdateDistance()
    {
        float currentMin = isSailing ? sailingMinLookDistance : minLookDistance;
        float currentMax = isSailing ? sailingMaxLookDistance : maxLookDistance;

        lookDistance -= Input.mouseScrollDelta.y * scrollSpeed;
        lookDistance = Mathf.Clamp(lookDistance, currentMin, currentMax);
    }
    #endregion

    #region 摄像机切换功能
    public void SwitchCamera()
    {
        CameraMode targetMode = cameraMode == CameraMode.FirstPerson ?
            CameraMode.ThirdPerson : CameraMode.FirstPerson;

        StartCameraTransition(targetMode);
    }

    private void StartCameraTransition(CameraMode targetMode)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);
        
        // 在切换前同步角度
        if (targetMode == CameraMode.ThirdPerson)
        {
            // 从第一人称切换到第三人称
            lookRotateY = orientation.eulerAngles.y;
        }
        else
        {
            // 从第三人称切换到第一人称
            yRotation = lookRotateY;
        }

        transitionCoroutine = StartCoroutine(CameraTransitionRoutine(targetMode));
    }

    private IEnumerator CameraTransitionRoutine(CameraMode targetMode)
    {
        IsTransitioning = true;
        
        // 存储初始位置和旋转
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        
        // 计算目标位置和旋转
        Vector3 targetPosition = GetTargetCameraPosition(targetMode);
        Quaternion targetRotation = GetTargetCameraRotation(targetMode);
        
        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsedTime / transitionDuration);
            
            // 平滑插值位置和旋转
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
            
            yield return null;
        }

        // 确保最终位置准确
        transform.position = targetPosition;
        transform.rotation = targetRotation;
        
        // 更新摄像机模式
        cameraMode = targetMode;
        IsTransitioning = false;
        UpdatePlayerBodyVisibility();
        CameraModeChanged?.Invoke(cameraMode);
    }

    private Vector3 GetTargetCameraPosition(CameraMode targetMode)
    {
        if (targetMode == CameraMode.FirstPerson)
        {
            // 第一人称摄像机位置是 cameraPosition 的位置
            return cameraPosition != null ? cameraPosition.position : (target != null ? target.position : transform.position);
        }
        else
        {
            // 计算第三人称摄像机位置（与原有逻辑一致）
            lookRotateX = Mathf.Clamp(lookRotateX, 60, 80);
            Quaternion rot = Quaternion.Euler(lookRotateX, lookRotateY, 0);
            return target.transform.position - (rot * Vector3.forward * lookDistance);
        }
    }

    private Quaternion GetTargetCameraRotation(CameraMode targetMode)
    {
        if (targetMode == CameraMode.FirstPerson)
        {
            // 第一人称使用当前玩家朝向
            return Quaternion.Euler(xRotation, yRotation, 0f);
        }
        else
        {
            // 第三人称看向目标
            Vector3 directionToTarget = (target.position - GetTargetCameraPosition(targetMode)).normalized;
            return Quaternion.LookRotation(directionToTarget);
        }
    }

    public void InitializeCameraPosition()
    {
        // 根据初始模式设置摄像机位置
        if (cameraMode == CameraMode.FirstPerson)
        {
            transform.position = GetTargetCameraPosition(CameraMode.FirstPerson);
            transform.rotation = GetTargetCameraRotation(CameraMode.FirstPerson);
        }
        else
        {
            transform.position = GetTargetCameraPosition(CameraMode.ThirdPerson);
            transform.rotation = GetTargetCameraRotation(CameraMode.ThirdPerson);
        }
    }
    
    // 将欧拉角归一化到 -180 到 180 度范围
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    /// <summary>
    /// 动态调整第一人称摄像机的高度
    /// </summary>
    /// <param name="height">新的高度（本地坐标Y轴）</param>
    public void SetFirstPersonHeight(float height)
    {
        if (cameraPosition != null)
        {
            Vector3 localPos = cameraPosition.localPosition;
            localPos.y = height;
            cameraPosition.localPosition = localPos;
        }
    }

    /// <summary>
    /// 设置新的摄像机目标点（用于切换角色模型时更新头部锚点）
    /// </summary>
    /// <param name="newCameraPosition">新的摄像机位置锚点</param>
    public void SetCameraPositionAnchor(Transform newCameraPosition)
    {
        cameraPosition = newCameraPosition;
        target = newCameraPosition;
    }

    /// <summary>
    /// 更新玩家身体渲染器列表（切换模型后需要调用此方法以正确隐藏/显示身体）
    /// </summary>
    /// <param name="newRenderers">新的渲染器数组</param>
    public void UpdatePlayerRenderers(Renderer[] newRenderers)
    {
        playerBodyRenderers = newRenderers;
        UpdatePlayerBodyVisibility();
    }

    #endregion

    #region 玩家身体可见性控制
    private void UpdatePlayerBodyVisibility()
    {
        if (playerBodyRenderers == null || playerBodyRenderers.Length == 0) return;

        bool shouldHide = (cameraMode == CameraMode.FirstPerson);

        foreach (Renderer renderer in playerBodyRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = !shouldHide;
                // 同时禁用阴影投射，避免第一人称模式下产生奇怪的阴影
                renderer.shadowCastingMode = shouldHide ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }
    }
    #endregion
}