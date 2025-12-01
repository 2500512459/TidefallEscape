using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.Animations.Rigging;

public class ThirdPersonShooterController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    [SerializeField] private LayerMask aimColliderLayerMask = new LayerMask();
    [SerializeField] private Transform debugTransform;
    [SerializeField] private Transform bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private Canvas crosshairCanvas;
    private Vector3 mouseWorldPosition = Vector3.zero;
    [SerializeField] private float aimSmoothTime = 0.02f;   // 瞄准点平滑时间
    private Vector3 aimSmoothVelocity = Vector3.zero;       // SmoothDamp 速度缓存
    private PlayerInput playerInput;
    private PlayerCtrl playerCtrl;
    private PlayerCamera playerCamera;
    private PlayerAimCamera playerAimCamera;
    private PlayerStateMachine playerStateMachine;

    private PlayerCamera.CameraMode lastCameraMode;
    private bool isAiming = false;  // ← 用于检测状态变化

    private Rig aimRig; //整体瞄准Rig
    private MultiAimConstraint bodyAimRig; //身体瞄准Rig
    private MultiAimConstraint handleAimRig; //抓握瞄准Rig
    private TwoBoneIKConstraint handAimRig; //手瞄准Rig
    public Transform AimTarget => debugTransform;

    private Coroutine aimRigCoroutine;
    private Coroutine bodyAimCoroutine;
    private Coroutine handleAimCoroutine;
    private Coroutine handAimCoroutine;

    public void SetAimRig(Rig aimRig, MultiAimConstraint bodyAimRig, MultiAimConstraint handleAimRig, TwoBoneIKConstraint handAimRig)
    {
        this.aimRig = aimRig;
        this.bodyAimRig = bodyAimRig;
        this.handleAimRig = handleAimRig;
        this.handAimRig = handAimRig;
    }
    public void SetBodyAimRig(float weight)
    {
        if (bodyAimCoroutine != null) StopCoroutine(bodyAimCoroutine);
        bodyAimCoroutine = StartCoroutine(SmoothSetWeight(() => bodyAimRig.weight, x => bodyAimRig.weight = x, weight));
    }
    public void SetHandleAimRig(float weight)
    {
        if (handleAimCoroutine != null) StopCoroutine(handleAimCoroutine);
        handleAimCoroutine = StartCoroutine(SmoothSetWeight(() => handleAimRig.weight, x => handleAimRig.weight = x, weight));
    }
    public void SetHandAimRig(float weight)
    {
        if (handAimCoroutine != null) StopCoroutine(handAimCoroutine);
        handAimCoroutine = StartCoroutine(SmoothSetWeight(() => handAimRig.weight, x => handAimRig.weight = x, weight));
    }
    public void SetAimRig(float weight)
    {
        if (aimRigCoroutine != null) StopCoroutine(aimRigCoroutine);
        aimRigCoroutine = StartCoroutine(SmoothSetWeight(() => aimRig.weight, x => aimRig.weight = x, weight));
    }

    private IEnumerator SmoothSetWeight(System.Func<float> getWeight, System.Action<float> setWeight, float targetWeight)
    {
        while (Mathf.Abs(getWeight() - targetWeight) > 0.001f)
        {
            float newWeight = Mathf.Lerp(getWeight(), targetWeight, Time.deltaTime * 15f);
            setWeight(newWeight);
            yield return null;
        }
        setWeight(targetWeight);
    }

    private void Awake()
    {
        playerInput = PlayerInput.Instance;
        playerCtrl = GetComponent<PlayerCtrl>();
        playerCamera = playerCtrl.PlayerCamera;
        playerAimCamera = playerCtrl.PlayerAimCamera;
        playerStateMachine = GetComponent<PlayerStateMachine>();
    }

    private void Start()
    {
        if (bulletPrefab != null)
        {
            ObjectPool.CreatePool(bulletPrefab, 20);
        }
    }

    private void Update()
    {
        // mouseWorldPosition = Vector3.zero;
        // Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        // Ray ray = Camera.main.ScreenPointToRay(screenCenter);
        
        // // 注意：请确保 aimColliderLayerMask 排除了 Player 层，否则射线会打中玩家自己
        // if (Physics.Raycast(ray, out RaycastHit hit, 999f, aimColliderLayerMask))
        // {
        //     mouseWorldPosition = hit.point;
        //     if (debugTransform != null) debugTransform.position = hit.point;
        // }
        // else
        // {
        //     // 如果没打中任何东西，默认目标设为射线前方 20 米处
        //     mouseWorldPosition = ray.GetPoint(20f);
        //     if (debugTransform != null) debugTransform.position = mouseWorldPosition;
        // }

        // 检查是否应该进入/保持瞄准状态
        bool shouldAim = CheckAimCondition();

        if (shouldAim)
        {
            if (!isAiming)
            {
                EnterAimMode();
            }
        }
        else
        {
            if (isAiming)
            {
                ExitAimMode();
            }
        }
    }
    private void LateUpdate()
    {
        UpdateAimTarget();
    }

    private void UpdateAimTarget()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);

        // 先算出本帧的目标点
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, 999f, aimColliderLayerMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(20f);
        }

        // 使用 SmoothDamp 对瞄准点做平滑
        mouseWorldPosition = Vector3.SmoothDamp(
            mouseWorldPosition,
            targetPoint,
            ref aimSmoothVelocity,
            aimSmoothTime
        );

        if (debugTransform != null)
        {
            debugTransform.position = mouseWorldPosition;
        }
    }
    public void Shoot()
    {
        Vector3 aimDir = (mouseWorldPosition - bulletSpawnPoint.position).normalized;
        ObjectPool.Spawn(bulletPrefab, bulletSpawnPoint.position, Quaternion.LookRotation(aimDir, Vector3.up));
    }


    private bool CheckAimCondition()
    {
        // 1. 限制职业为 Lookout
        if (PlayerDataManager.Instance.GetSelectedProfession() != ProfessionType.Lookout)
            return false;

        // 2. 限制特定状态
        if (playerStateMachine == null || playerStateMachine.CurrentState == null)
            return false;

        var state = playerStateMachine.CurrentState;
        
        // 只要处于这些状态之一，就应该保持瞄准视角
        return state is PlayerState_DrawArrow || 
               state is PlayerState_AimIdle || 
               state is PlayerState_AimWalk || 
               state is PlayerState_AimRecoil;
    }

    private void EnterAimMode()
    {
        crosshairCanvas.gameObject.SetActive(true);

        // 同步摄像机角度，防止角色突然转向
        SyncAimCameraOrientation();

        isAiming = true;
        lastCameraMode = playerCamera.cameraMode; // 记录进入前的模式
        aimVirtualCamera.gameObject.SetActive(true);
        playerCamera.cameraMode = PlayerCamera.CameraMode.AimPerson;
        SetAimRig(1f);
    }

    private void SyncAimCameraOrientation()
    {
        var pov = aimVirtualCamera.GetCinemachineComponent<CinemachinePOV>();
        if (pov == null) return;
    
        Vector3 camForward = Camera.main.transform.forward;
    
        // 水平角（yaw）
        float yaw = Mathf.Atan2(camForward.x, camForward.z) * Mathf.Rad2Deg;
    
        // 垂直角（pitch）
        float pitch = -Mathf.Asin(camForward.y) * Mathf.Rad2Deg;
    
        pov.m_HorizontalAxis.Value = yaw;
        pov.m_VerticalAxis.Value = pitch;
    }

    private void ExitAimMode()
    {
        crosshairCanvas.gameObject.SetActive(false);
        isAiming = false;
        aimVirtualCamera.gameObject.SetActive(false);
        
        // 恢复模式前检查一下，避免覆盖了其他逻辑（比如被动切回时）
        // 这里简单恢复到进入前的模式
        playerCamera.cameraMode = lastCameraMode;
        playerCamera.InitializeCameraPosition(); // 恢复 MainCamera 位置
        SetAimRig(0f);
    }
}
