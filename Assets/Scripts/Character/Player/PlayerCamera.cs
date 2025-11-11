using System.Collections;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
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
    public Transform cameraPosition;    // 摄像机位置
    
    [Header("第三人称参数")]
    public float rotateSpeed = 1.0f;
    public float scrollSpeed = 3.0f;
    public float lookRotateX = 60f;
    public float lookRotateY = 180f;
    public float lookDistance = 20f;
    public Transform target;            // 摄像机目标
    
    // 私有变量
    private float xRotation;
    private float yRotation;
    private Vector3 offset;
    private Coroutine transitionCoroutine;
    
    public enum CameraMode
    {
        FirstPerson,
        ThirdPerson
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        cameraMode = CameraMode.FirstPerson;
        InitializeCameraPosition();
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
        lookDistance += Input.mouseScrollDelta.y * scrollSpeed;
        lookDistance = Mathf.Clamp(lookDistance, 4, 100);
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
    }

    private Vector3 GetTargetCameraPosition(CameraMode targetMode)
    {
        if (targetMode == CameraMode.FirstPerson)
        {
            // 第一人称摄像机位置是玩家的眼睛位置
            return target.position; 
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

    private void InitializeCameraPosition()
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

    #endregion
}