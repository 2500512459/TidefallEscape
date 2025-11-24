using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAimCamera : MonoBehaviour
{
    [Header("Cinemachine")]
    [Tooltip("设置的摄像机将跟随的目标")]
    public GameObject CinemachineCameraTarget;

    [Tooltip("摄像机向上移动的最大角度")]
    public float TopClamp = 70.0f;

    [Tooltip("摄像机向下移动的最大角度")]
    public float BottomClamp = -30.0f;

    [Tooltip("额外角度，用于覆盖摄像机位置。用于锁定摄像机位置时的微调")]
    public float CameraAngleOverride = 0.0f;

    [Tooltip("锁定摄像机位置的所有轴")]
    public bool LockCameraPosition = false;
    [Tooltip("摄像机灵敏度")]
    public float Sensitivity = 0.5f;

    private PlayerInput playerInput;
    
    // 摄像机目标的偏航和俯仰角度
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // 移动阈值
    private const float _threshold = 0.01f;

    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0;
#else
            return false;
#endif
        }
    }

    private void Awake()
    {
        playerInput = PlayerInput.Instance;
    }

    private void Start()
    {
        if (CinemachineCameraTarget != null)
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
        }
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void CameraRotation()
    {
        // 如果有输入且摄像机位置未锁定
        Vector2 lookInput = playerInput.inputLook;
        if (lookInput.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            // 不乘以Time.deltaTime;
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetYaw += lookInput.x * deltaTimeMultiplier * Sensitivity;
            _cinemachineTargetPitch -= lookInput.y * deltaTimeMultiplier * Sensitivity  ;
        }

        // 限制旋转角度，使其在360度范围内
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // 摄像机将跟随目标
        if (CinemachineCameraTarget != null)
        {
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}
