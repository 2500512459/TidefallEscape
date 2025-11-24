using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class PlayerCtrl : MonoBehaviour
{
    public InventoryContext setContext = InventoryContext.Default;  // 当前场景上下文
    [Header("移动参数")]
    private float moveSpeed = 7f;
    public float walkSpeed = 7f;
    public float sprintSpeed = 14f;
    public float climbSpeed = 2.5f;
    public float swimmingSpeed = 5f;
    public float groundDrag;

    public float currentSpeed => new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
    public bool isGround => groundDetector.IsGrounded;
    public bool isOnShip => false;
    public bool isFalling => !isGround && rb.velocity.y < 0f && !isSwimming;
    public bool isClimbing => wallDetector.IsTouchingWall && PlayerCamera.cameraMode != PlayerCamera.CameraMode.AimPerson;
    public bool isClimbOver => wallDetector.IsClimbOver;
    public bool isSwimming;
    public bool isAttacking;

    [Header("跳跃参数")]
    [SerializeField] float jumpForce = 6f;
    [SerializeField] float jumpCooldown = 0.25f;
    [SerializeField] float airMultiplier = 0.6f;
    [SerializeField] bool readyToJump = true;

    [Header("坡度控制")]
    public float maxSlopeAngle = 45f;
    public float currentAngle;
    private RaycastHit slopeHit;
    private bool isOnSlope;
    private bool exitingSlope = false;

    [SerializeField] LayerMask obstacleLayer = -1;

    public PlayerCamera PlayerCamera;
    public PlayerAimCamera PlayerAimCamera;
    private ThirdPersonShooterController thirdPersonShooterController;
    public Transform orientation;
    public PlayerGroundDetector groundDetector;
    public PlayerWallDetector wallDetector;
    private PlayerInput playerInput;
    public Rigidbody rb;
    private Player player;
    public Transform interactiveDetection;
    [Header("宝箱交互")]
    [SerializeField] private float lootDetectRadius = 2f;
    private readonly List<TreasureBox> nearbyTreasureBoxes = new List<TreasureBox>();
    private TreasureBox highlightedTreasureBox;
    private SphereCollider trigger;
    public MovementState state;
    public enum MovementState { walking, sprinting, climbing, swimming, air }
    public WeaponState weaponState = WeaponState.Sheathed;
    public enum WeaponState { Sheathed, Drawing, Armed, Sheathing }

    // 当前可交互的物体
    private List<IInteractable> interactables = new List<IInteractable>();
    private IInteractable currentInteractTarget;
    [Header("交互判定")]
    [Range(-1f, 1f)]
    [SerializeField] private float interactFovThreshold = 0.35f; // 约70度视场


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = PlayerInput.Instance;
        groundDetector = GetComponentInChildren<PlayerGroundDetector>();
        player = GetComponent<Player>();
        thirdPersonShooterController = GetComponent<ThirdPersonShooterController>();
    }
    private void OnEnable()
    {
        if (playerInput == null)
            playerInput = PlayerInput.Instance;

        // 启用时添加触发器
        if (trigger == null)
        {
            trigger = interactiveDetection.GetComponent<SphereCollider>();
            if (trigger == null)
                trigger = interactiveDetection.gameObject.AddComponent<SphereCollider>();
        }

        trigger.isTrigger = true;
        trigger.radius = lootDetectRadius;
        trigger.enabled = true;

        // 注册输入事件
        playerInput.OnInteractionEvent += HandleInteract;
        playerInput.OpenInventoryEvent += TryOpenInventory;
        playerInput.LootPressedEvent += TryToggleTreasureLoot;
        
        EventManager.Listen<CharacterDeathMessage>(this, OnCharacterDeath);
    }

    private void OnDisable()
    {
        // 禁用时移除或关闭触发器
        if (trigger != null)
            trigger.enabled = false;

        EventManager.Unlisten<CharacterDeathMessage>(this);
        if (playerInput != null)
        {
            playerInput.OnInteractionEvent -= HandleInteract;
            playerInput.OpenInventoryEvent -= TryOpenInventory;
            playerInput.LootPressedEvent -= TryToggleTreasureLoot;
        }
    }
    void Start()
    {
        playerInput.EnableControlInput();
    }

    private void Update()
    {
        if (isSwimming || isGround)
            rb.drag = groundDrag;
        else
            rb.drag = 0f;
        
        SpeedControl();
        StateHandler();
        HandleCombatInput();
        //HandleInteract();
        if (playerInput.Jump && readyToJump && isGround && player.CanUseVitality && PlayerCamera.cameraMode != PlayerCamera.CameraMode.AimPerson)
        {
            readyToJump = false;
            player.ConsumeVitality(15f); // 跳跃消耗体力
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if (Input.GetKeyDown(KeyCode.V) && !PlayerCamera.IsTransitioning)
            PlayerCamera.SwitchCamera();

        // 更新最近交互目标
        UpdateClosestInteractable();
        UpdateClosestTreasureBox();
    }

    private void HandleCombatInput()
    {
        if (playerInput.Fire)
        {
            if (weaponState == WeaponState.Sheathed)
                weaponState = WeaponState.Drawing;
            else if (weaponState == WeaponState.Armed)
                isAttacking = true;
        }
    }

    void FixedUpdate()
    {
        isOnSlope = OnSlope();
        isSwimming = OnSwimming();

        if (isGround && isOnShip && !playerInput.Jump)
        {
            rb.useGravity = false;
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            if (groundDetector.TryGetShipGroundPoint(out Vector3 shipPoint))
            {
                Vector3 targetPos = new Vector3(rb.position.x, shipPoint.y, rb.position.z);
                rb.MovePosition(Vector3.Lerp(rb.position, targetPos, Time.fixedDeltaTime * 10f));
            }
        }
        else
        {
            rb.useGravity = !isClimbing; // 攀爬状态下也关重力，其他情况开
        }
    }

    private bool OnSwimming()
    {
        if(isClimbing) return false;
        if(isGround) return false;
        if (Water.Instance == null) return false;
        float waterHeight = Water.Instance.GetWaterHeight(transform.position);
        return transform.position.y < waterHeight + 0.2f;
    }

    private void StateHandler()
    {
        if (isSwimming)
        {
            state = MovementState.swimming;

            if (player.CanUseVitality)
            {
                player.ConsumeVitality(30f * Time.deltaTime);
                moveSpeed = swimmingSpeed;
            }
            else
            {
                moveSpeed = swimmingSpeed * 0.5f;
            }
        }
        else if (isClimbing)
        {
            state = MovementState.climbing;

            if (player.CanUseVitality)
            {
                player.ConsumeVitality(30f * Time.deltaTime);
                moveSpeed = climbSpeed;
            }
            else
            {
                moveSpeed = 0f;
                rb.velocity = Vector3.zero; // 无体力时停止攀爬
            }
        }
        else if (isGround && playerInput.Sprint)
        {
            if (player.CanUseVitality)
            {
                state = MovementState.sprinting;
                player.ConsumeVitality(30f * Time.deltaTime);
                moveSpeed = sprintSpeed;
            }
            else
            {
                state = MovementState.walking;
                moveSpeed = walkSpeed;
            }
        }
        else if (isGround)
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }
        else
        {
            state = MovementState.air;
        }

        // 如果没在使用体力的状态，则自动恢复体力
        if (state == MovementState.walking)
        {
            player.RecoverVitality(player.vitalityRecoveryRate * Time.deltaTime);
        }
    }

    // ===================== 核心移动 =====================
    public void Move()
    {
        Vector3 moveDir = MoveDirection();
        Vector3 newVelocity = rb.velocity;

        if (isSwimming)
        {
            newVelocity = moveDir * moveSpeed + Vector3.up * rb.velocity.y;
        }
        else if (isClimbing)
        {
            if (isClimbOver)
            {
                rb.velocity = Vector3.zero; // 停止向上移动，等待翻越状态
                rb.mass = 0f;
            }
            else
            {
                newVelocity = Vector3.up * climbSpeed;
            }
        }
        else if (isOnSlope && !exitingSlope)
        {
            Vector3 slopeDir = GetSlopeMoveDirection();
            newVelocity = slopeDir * moveSpeed;
        }
        else if (isGround)
        {
            newVelocity = moveDir * moveSpeed + Vector3.up * rb.velocity.y;
        }
        else if (isFalling)
        {
            // 空中移动速度减弱
            Vector3 airVel = moveDir * moveSpeed * airMultiplier;
            newVelocity = new Vector3(airVel.x, rb.velocity.y, airVel.z);
        }
        Turn();
        rb.velocity = Vector3.Lerp(rb.velocity, newVelocity, Time.fixedDeltaTime * 10f);
    }

    public void SetVelocityY(float newVelocityY)
    {
        rb.velocity = new Vector3(rb.velocity.x, newVelocityY, rb.velocity.z);
    }

    private void SpeedControl()
    {
        if (isOnSlope && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    public void Turn()
    {
        // ================================
        // 第一人称：面向 orientation.forward
        // ================================
        if (PlayerCamera.cameraMode == PlayerCamera.CameraMode.FirstPerson)
        {
            Vector3 forward = orientation.forward;
            forward.y = 0f;

            if (forward != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(forward);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 15f);
            }
            return;
        }
        
        // ================================
        // 普通第三人称：根据移动方向转向
        // ================================
        if (PlayerCamera.cameraMode == PlayerCamera.CameraMode.ThirdPerson)
        {
            Vector3 moveDir = MoveDirection();
            if (moveDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 15f);
            }
        }
        
        // ================================
        // 瞄准模式：Orientation 跟随人物朝向
        // ================================
        if (PlayerCamera.cameraMode == PlayerCamera.CameraMode.AimPerson)
        {
            orientation.rotation = transform.rotation;
        }
    }

    /// <summary>
    /// 瞄准状态下的转向逻辑：
    /// 根据 TPS 控制器计算出的瞄准目标点旋转角色，仅在 AimPerson 模式下生效。
    /// 设计给各个瞄准状态的 PhysicsUpdate 调用，避免和普通移动转向混用。
    /// </summary>
    public void AimTurn()
    {
        // 只有瞄准相机模式才旋转
        if (PlayerCamera == null || PlayerCamera.cameraMode != PlayerCamera.CameraMode.AimPerson)
            return;

        if (thirdPersonShooterController == null || thirdPersonShooterController.AimTarget == null)
            return;

        Vector3 worldAimTarget = thirdPersonShooterController.AimTarget.position;
        worldAimTarget.y = transform.position.y;

        Vector3 aimDirection = (worldAimTarget - transform.position).normalized;
        if (aimDirection.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(aimDirection, Vector3.up);
        // 使用 fixedDeltaTime 以匹配状态机 PhysicsUpdate（通常在 FixedUpdate 中调用）
        float rotateLerpSpeed = 20f;
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * rotateLerpSpeed);

        // 确保 orientation 始终跟随角色朝向
        if (orientation != null)
        {
            orientation.rotation = transform.rotation;
        }
    }



    public Vector3 MoveDirection()
    {
        float horizontal = playerInput.AxesX;
        float vertical = playerInput.AxesY;
        return (orientation.forward * vertical + orientation.right * horizontal).normalized;
    }

    public void Jump()
    {
        exitingSlope = true;
        // 保留水平速度，直接修改垂直分量
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
    }

    public void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, 1f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            currentAngle = angle;
            return angle > maxSlopeAngle;
        }
        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(MoveDirection(), slopeHit.normal).normalized;
    }
    public void ClimbOver()
    {
        if(PlayerCamera.cameraMode != PlayerCamera.CameraMode.ThirdPerson)
            PlayerCamera.SwitchCamera();
        
        StartCoroutine(DoClimbOver(wallDetector.WallTopPoint));
    }

    private IEnumerator DoClimbOver(Vector3 topPoint)
    {
        rb.useGravity = false;
        playerInput.DisableControlInput(); // 禁用输入以防乱动

        Vector3 startPos = transform.position;

        // 人物目标点略高（让脚站在墙顶上）
        BoxCollider box = GetComponentInChildren<BoxCollider>();
        float playerHeight = box != null ? box.size.y * transform.localScale.y : 2f; // 取Y方向长度
        // 墙顶点 + 一半身高，让脚刚好踩在墙顶上
        Vector3 midPos;
        // 向前一点，避免卡在墙上
        Vector3 endPos;

        // 墙顶点 + 一半身高，让脚刚好踩在墙顶上
        midPos = new Vector3(topPoint.x, topPoint.y, topPoint.z);
        // 向前一点，避免卡在墙上
        endPos = midPos + transform.forward * 0.2f;

    
        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < 2.2f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = 0f;
        // 第一段：上升
        while (elapsed < duration * 0.8f)
        {
            transform.position = Vector3.Lerp(startPos, midPos, elapsed / (duration * 0.8f));
            elapsed += Time.deltaTime;
            yield return null;
        }
    
        // 第二段：前移
        elapsed = 0f;
        while (elapsed < duration * 0.2f)
        {
            transform.position = Vector3.Lerp(midPos, endPos, elapsed / (duration * 0.2f));
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        rb.useGravity = true;
        playerInput.EnableControlInput();
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * 0.5f);
    }


    private void HandleInteract()
    {
        if (currentInteractTarget == null)
            return;
        currentInteractTarget.Interact(player);
            
    }

    private void UpdateClosestInteractable()
    {
        // 清理已被销毁的对象
        interactables.RemoveAll(i => i == null || i.Transform == null);

        if (interactables.Count == 0)
        {
            if (currentInteractTarget != null)
            {
                currentInteractTarget.OnLoseFocus(player);
                currentInteractTarget = null;
            }
            return;
        }

        Vector3 originPos = interactiveDetection != null ? interactiveDetection.position : transform.position;
        Vector3 forward = orientation != null ? orientation.forward : transform.forward;
        forward.y = 0f;
        if (forward == Vector3.zero)
            forward = transform.forward;
        forward.Normalize();

        IInteractable bestInView = null;
        float bestInViewDist = float.MaxValue;
        IInteractable bestOverall = null;
        float bestOverallDist = float.MaxValue;

        foreach (var i in interactables)
        {
            if (i == null || i.Transform == null) continue;

            Vector3 toTarget = i.Transform.position - originPos;
            float sqrDistance = toTarget.sqrMagnitude;

            if (sqrDistance < bestOverallDist)
            {
                bestOverallDist = sqrDistance;
                bestOverall = i;
            }

            if (toTarget == Vector3.zero)
                continue;

            Vector3 dir = toTarget.normalized;
            float dot = Vector3.Dot(forward, dir);
            if (dot >= interactFovThreshold && sqrDistance < bestInViewDist)
            {
                bestInViewDist = sqrDistance;
                bestInView = i;
            }
        }

        IInteractable closest = bestInView ?? bestOverall;

        // 如果目标变化了，更新焦点
        if (closest != currentInteractTarget)
        {
            if (currentInteractTarget != null)
                currentInteractTarget.OnLoseFocus(player);

            currentInteractTarget = closest;

            if (currentInteractTarget != null)
                currentInteractTarget.OnFocus(player);
        }
    }
    private void OnCharacterDeath(CharacterDeathMessage msg)
    {
        if (msg.DeadCharacter == null)
            return;

        // 找到死亡对象对应的交互对象（如果有）
        var toRemove = interactables
            .FirstOrDefault(i =>
                i != null &&
                i.Transform != null &&
                i.Transform.root == msg.DeadCharacter.transform.root
            );

        if (toRemove != null)
        {
            interactables.Remove(toRemove);

            if (currentInteractTarget == toRemove)
            {
                currentInteractTarget.OnLoseFocus(player);
                currentInteractTarget = null;
            }
        }
    }
    // ===================== Trigger 检测 =====================
    private void OnTriggerEnter(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && !interactables.Contains(interactable))
            interactables.Add(interactable);

        TreasureBox treasure = other.GetComponent<TreasureBox>();
        if (treasure == null)
            treasure = other.GetComponentInParent<TreasureBox>();
        if (treasure != null && !nearbyTreasureBoxes.Contains(treasure))
            nearbyTreasureBoxes.Add(treasure);
    }

    private void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && interactables.Contains(interactable))
        {
            interactables.Remove(interactable);
            if (currentInteractTarget == interactable)
            {
                interactable.OnLoseFocus(player);
                currentInteractTarget = null;
            }
        }

        TreasureBox treasure = other.GetComponent<TreasureBox>();
        if (treasure == null)
            treasure = other.GetComponentInParent<TreasureBox>();
        if (treasure != null && nearbyTreasureBoxes.Contains(treasure))
        {
            nearbyTreasureBoxes.Remove(treasure);
            if (highlightedTreasureBox == treasure)
            {
                treasure.HideHint();
                highlightedTreasureBox = null;
            }
        }
    }

    // ===================== Tab键 打开/关闭背包 =====================
    public void TryOpenInventory(bool isOpen)
    {
        if (isOpen)
        {
            InventoryManager.Instance.currenContext = setContext;
            InventoryUI.Instance?.ShowPanel();
            playerInput.DisableAllInputsExcept(playerInput.playerInputAction.Control.OpenInventory);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            CloseInventoryUIAndRestoreHint();
        }
    }

    private void TryToggleTreasureLoot()
    {
        if (playerInput == null)
            return;

        if (playerInput.isLootOpen)
        {
            playerInput.isInventoryOpen = false;
            playerInput.isLootOpen = false;
            CloseInventoryUIAndRestoreHint();
            return;
        }

        if (highlightedTreasureBox == null)
            return;

        highlightedTreasureBox.TryOpen();
        highlightedTreasureBox.HideHint();

        playerInput.isInventoryOpen = true;
        playerInput.isLootOpen = true;
        playerInput.DisableAllInputsExcept(playerInput.playerInputAction.Control.OpenInventory, playerInput.playerInputAction.Control.OpenEvent);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UpdateClosestTreasureBox(bool forceRefresh = false)
    {
        nearbyTreasureBoxes.RemoveAll(t => t == null || !t.gameObject.activeInHierarchy);

        if (nearbyTreasureBoxes.Count == 0)
        {
            if (highlightedTreasureBox != null)
            {
                highlightedTreasureBox.HideHint();
                highlightedTreasureBox = null;
            }
            return;
        }

        TreasureBox nearest = null;
        float closestDistance = float.MaxValue;
        Vector3 playerPos = transform.position;

        foreach (var treasure in nearbyTreasureBoxes)
        {
            float distance = Vector3.Distance(playerPos, treasure.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearest = treasure;
            }
        }

        if (nearest == highlightedTreasureBox)
        {
            if (forceRefresh && highlightedTreasureBox != null)
                highlightedTreasureBox.ShowHint();
            return;
        }

        if (highlightedTreasureBox != null)
            highlightedTreasureBox.HideHint();

        highlightedTreasureBox = nearest;

        if (highlightedTreasureBox != null)
            highlightedTreasureBox.ShowHint();
    }

    private void CloseInventoryUIAndRestoreHint()
    {
        InventoryUI.Instance?.HidePanel();
        playerInput?.EnableAllInputs();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        UpdateClosestTreasureBox(true);
    }
}
