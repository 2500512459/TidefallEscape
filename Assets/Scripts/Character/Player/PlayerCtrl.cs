using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class PlayerCtrl : MonoBehaviour
{
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
    public bool isClimbing => wallDetector.IsTouchingWall;
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
    public Transform orientation;
    public PlayerGroundDetector groundDetector;
    public PlayerWallDetector wallDetector;
    private PlayerInput playerInput;
    public Rigidbody rb;
    private Player player;
    public Transform interactiveDetection;
    private SphereCollider trigger;
    public MovementState state;
    public enum MovementState { walking, sprinting, climbing, swimming, air }
    public WeaponState weaponState = WeaponState.Sheathed;
    public enum WeaponState { Sheathed, Drawing, Armed, Sheathing }

    // 当前可交互的物体
    private List<IInteractable> interactables = new List<IInteractable>();
    private IInteractable currentInteractTarget;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = PlayerInput.Instance;
        groundDetector = GetComponentInChildren<PlayerGroundDetector>();
        player = GetComponent<Player>();
    }
    private void OnEnable()
    {
        // 启用时添加触发器
        if (trigger == null)
        {
            trigger = interactiveDetection.GetComponent<SphereCollider>();
            if (trigger == null)
                trigger = interactiveDetection.gameObject.AddComponent<SphereCollider>();
        }

        trigger.isTrigger = true;
        trigger.radius = 0.5f;
        trigger.enabled = true;


        PlayerInput.Instance.OnInteractionEvent += HandleInteract;
    }

    private void OnDisable()
    {
        // 禁用时移除或关闭触发器
        if (trigger != null)
            trigger.enabled = false;
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
        if (playerInput.Jump && readyToJump && isGround)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if (Input.GetKeyDown(KeyCode.V) && !PlayerCamera.IsTransitioning)
            PlayerCamera.SwitchCamera();

        // 更新最近目标
        UpdateClosestInteractable(); 
    }

    private void HandleCombatInput()
    {
        if (Input.GetMouseButtonDown(0))
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
        if (Water.Instance == null) return false;
        float waterHeight = Water.Instance.GetWaterHeight(transform.position);
        return transform.position.y < waterHeight + 0.2f;
    }

    private void StateHandler()
    {
        if (isSwimming)
        {
            state = MovementState.swimming;
            moveSpeed = swimmingSpeed;
        }
        else if (isClimbing)
        {
            state = MovementState.climbing;
            moveSpeed = climbSpeed;
        }
        else if (isGround && playerInput.Sprint)
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
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

        rb.velocity = Vector3.Lerp(rb.velocity, newVelocity, Time.fixedDeltaTime * 10f);
        Turn();
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
        Vector3 moveDir = MoveDirection();
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 15f);
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
        if (interactables.Count == 0)
        {
            if (currentInteractTarget != null)
            {
                currentInteractTarget.OnLoseFocus(player);
                currentInteractTarget = null;
            }
            return;
        }

        // 选取最近的可交互对象
        IInteractable closest = interactables
            .OrderBy(i => Vector3.Distance(transform.position, i.Transform.position))
            .FirstOrDefault();

        // 如果目标变化了，更新焦点
        if (closest != currentInteractTarget)
        {
            if (currentInteractTarget != null)
                currentInteractTarget.OnLoseFocus(player);

            currentInteractTarget = closest;
            currentInteractTarget.OnFocus(player);
        }
    }
    // ===================== Trigger 检测 =====================
    private void OnTriggerEnter(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && !interactables.Contains(interactable))
            interactables.Add(interactable);
    }

    private void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && interactables.Contains(interactable))
        {
            interactables.Remove(interactable);
            interactable.OnLoseFocus(player);
            if (currentInteractTarget == interactable)
                currentInteractTarget = null;
        }
    }
}
