using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoSingleton<PlayerInput>
{
    [Header("Input System")]
    [Tooltip("Input Actions 资源")]
    [SerializeField] private InputActionAsset inputActionAsset;

    // 对应 Control Action Map 里的各个 Action
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction boostMoveAction;
    private InputAction fireAction;
    private InputAction weaponAction;
    private InputAction switchWeaponAction;
    private InputAction rotateAction;
    private InputAction settingAction;
    private InputAction mapAction;

    // UI / 交互
    private InputAction openInventoryAction;
    private InputAction questAction;
    private InputAction openEventAction;
    private InputAction interactionEventAction;

    // ======================
    // 输入状态属性
    // ======================
    public Vector2 axes => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 inputLook => lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;

    public bool Move => axes.sqrMagnitude > 0.01f;
    public float AxesX => axes.x;
    public float AxesY => axes.y;

    public bool Jump => jumpAction != null && jumpAction.WasPressedThisFrame();
    public bool StopJump => jumpAction != null && jumpAction.WasReleasedThisFrame();

    public bool Sprint => boostMoveAction != null && boostMoveAction.IsPressed();
    public bool Fire => fireAction != null && fireAction.WasReleasedThisFrame();

    public bool WeaponPressed => weaponAction != null && weaponAction.WasPressedThisFrame();
    public bool SwitchWeaponPressed => switchWeaponAction != null && switchWeaponAction.WasPressedThisFrame();
    public bool RotatePressed => rotateAction != null && rotateAction.IsPressed();

    // UI 输入
    public bool InventoryPressed => openInventoryAction != null && openInventoryAction.WasPressedThisFrame();
    public bool QuestPressed => questAction != null && questAction.WasPressedThisFrame();
    // 事件输入
    //public bool OpenEvent => playerInputAction.Control.OpenEvent.WasPressedThisFrame();
    //public bool InteractionEvent => playerInputAction.Control.InteractionEvent.WasPressedThisFrame();

    // 对外暴露用于禁用/启用的关键 Action（用于其它系统调用）
    public InputAction FireInput => fireAction;
    public InputAction OpenInventoryInput => openInventoryAction;
    public InputAction OpenEventInput => openEventAction;
    public InputAction InteractionEventInput => interactionEventAction;
    public InputAction SettingInput => settingAction;
    public InputAction MapInput => mapAction;

    // ======================
    // 状态标志
    // ======================
    public bool isInventoryOpen;
    public bool isLootOpen;
    public bool isAttacked;

    // ======================
    // 事件
    // ======================
    public event Action<bool> OpenInventoryEvent;
    public event Action LootPressedEvent;
    public event Action QuestPressedEvent;
    //public event Action OnBoardShipEvent;
    //public event Action OnDriveBoatEvent;
    public event Action OnInteractionEvent;
    public event Action<bool> IsAttackedEvent;
    public event Action OnSwitchWeaponEvent;
    public event Action EscPressedEvent;
    public event Action MapPressedEvent;

    protected override void Awake()
    {
        base.Awake();

        if (inputActionAsset == null)
        {
            Debug.LogError("PlayerInput: 未在 Inspector 中指定 InputActionAsset，输入功能将无法工作。");
            return;
        }

        // 获取 Control Action Map（名字需要与你的 Input Actions 中的 Map 名一致）
        var controlMap = inputActionAsset.FindActionMap("Control", true);

        // 根据 Action 名称获取各个 InputAction（名称需与 Input Actions 资源里一致）
        moveAction = controlMap.FindAction("Move", true);
        lookAction = controlMap.FindAction("Look", true);
        jumpAction = controlMap.FindAction("Jump", true);
        boostMoveAction = controlMap.FindAction("BoostMove", true);
        fireAction = controlMap.FindAction("Fire", true);
        weaponAction = controlMap.FindAction("Weapon", true);
        switchWeaponAction = controlMap.FindAction("SwitchWeapon", true);
        rotateAction = controlMap.FindAction("Rotate", true);
        settingAction = controlMap.FindAction("Setting", true);
        // 地图（M 键，对应 Control Action Map 中名为 \"Map\" 的 Action）
        mapAction = controlMap.FindAction("Map", true);

        openInventoryAction = controlMap.FindAction("OpenInventory", true);
        questAction = controlMap.FindAction("Quest", true);
        openEventAction = controlMap.FindAction("OpenEvent", true);
        interactionEventAction = controlMap.FindAction("InteractionEvent", true);

        // 注册事件监听
        if (openInventoryAction != null)
            openInventoryAction.performed += OnInventoryInput;
        if (openEventAction != null)
            openEventAction.performed += OnLootOpen;            // 打开宝箱 F
        if (interactionEventAction != null)
            interactionEventAction.performed += OnInteraction;  // 交互 E
        //interactionEventAction.performed += OnBoardShip;    // 上船E
        //interactionEventAction.performed += OnDriveBoat;    // 驾驶船E
        if (weaponAction != null)
            weaponAction.performed += OnIsAttackedInput;
        if (questAction != null)
            questAction.performed += OnQuestInput;
        if (switchWeaponAction != null)
            switchWeaponAction.performed += OnSwitchWeaponInput;
        if (settingAction != null)
            settingAction.performed += OnEscInput;
        if (mapAction != null)
            mapAction.performed += OnMapInput;
    }

    void OnDestroy()
    {
        // 移除事件监听器并禁用输入控制
        if (inputActionAsset != null)
        {
            try
            {
                if (openInventoryAction != null)
                    openInventoryAction.performed -= OnInventoryInput;
                if (openEventAction != null)
                    openEventAction.performed -= OnLootOpen;
                if (interactionEventAction != null)
                    interactionEventAction.performed -= OnInteraction;
                //if (interactionEventAction != null)
                //    interactionEventAction.performed -= OnBoardShip;
                //if (interactionEventAction != null)
                //    interactionEventAction.performed -= OnDriveBoat;
                if (weaponAction != null)
                    weaponAction.performed -= OnIsAttackedInput;
                if (questAction != null)
                    questAction.performed -= OnQuestInput;
                if (switchWeaponAction != null)
                    switchWeaponAction.performed -= OnSwitchWeaponInput;
                if (settingAction != null)
                    settingAction.performed -= OnEscInput;
                if (mapAction != null)
                    mapAction.performed -= OnMapInput;

                // 禁用输入控制，防止内存泄漏和性能问题
                inputActionAsset.Disable();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"清理 PlayerInput 时发生错误: {e.Message}");
            }
        }
    }

    void OnInventoryInput(InputAction.CallbackContext context)
    {
        // 防止商店打开时切换背包
        var shopPanel = ShopUI.Instance;
        Debug.Log("shopPanel: " + shopPanel.IsVisible);
        if (shopPanel == null || !shopPanel.IsVisible)
        {
            isInventoryOpen = !isInventoryOpen;
            isLootOpen = isInventoryOpen;
            OpenInventoryEvent?.Invoke(isInventoryOpen);
        }
    }

    // F键触发的事件
    void OnLootOpen(InputAction.CallbackContext context)
    {
        LootPressedEvent?.Invoke();
    }
    // E键触发的事件
    void OnInteraction(InputAction.CallbackContext context)
    {
        OnInteractionEvent?.Invoke();
    }
    void OnQuestInput(InputAction.CallbackContext context)
    {
        QuestPressedEvent?.Invoke();
    }
    // void OnBoardShip(InputAction.CallbackContext context)
    // {
    //     OnBoardShipEvent?.Invoke();
    // }
    // void OnDriveBoat(InputAction.CallbackContext context)
    // {
    //     OnDriveBoatEvent?.Invoke();
    // }
    void OnIsAttackedInput(InputAction.CallbackContext context)
    {
        isAttacked = !isAttacked;
        IsAttackedEvent?.Invoke(isAttacked);
        if (isAttacked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void OnSwitchWeaponInput(InputAction.CallbackContext context)
    {
        OnSwitchWeaponEvent?.Invoke();
    }

    void OnEscInput(InputAction.CallbackContext context)
    {
        EscPressedEvent?.Invoke();
    }

    void OnMapInput(InputAction.CallbackContext context)
    {
        MapPressedEvent?.Invoke();
    }

    // ======================
    // 控制启用/禁用
    // ======================
    public void EnableControlInput()
    {
        if (inputActionAsset == null)
        {
            Debug.LogWarning("PlayerInput: InputActionAsset 未初始化，无法启用输入。");
            return;
        }

        inputActionAsset.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void DisableControlInput()
    {
        if (inputActionAsset == null)
        {
            Debug.LogWarning("PlayerInput: InputActionAsset 未初始化，无法禁用输入。");
            return;
        }

        inputActionAsset.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void DisableMovementAndLook(bool disableQuestInput = false, bool disableInteractionInput = false)
    {
        moveAction?.Disable();
        lookAction?.Disable();
        if (disableQuestInput)
        {
            questAction?.Disable();
        }
        if (disableInteractionInput)
        {
            interactionEventAction?.Disable();
        }
    }
    
    public void EnableMovementAndLook(bool enableQuestInput = false, bool enableInteractionInput = false)
    {
        moveAction?.Enable();
        lookAction?.Enable();
        if (enableQuestInput)
        {
            questAction?.Enable();
        }
        if (enableInteractionInput)
        {
            interactionEventAction?.Enable();
        }
    }


    /// <summary>
    /// 禁用除指定按键之外的所有输入动作。
    /// </summary>
    /// <param name="allowedActions">允许继续启用的输入动作集合。</param>
    public void DisableAllInputsExcept(params InputAction[] allowedActions)
    {
        if (inputActionAsset == null)
        {
            Debug.LogWarning("PlayerInput: InputActionAsset 未初始化，无法禁用输入。");
            return;
        }

        var allowedSet = new HashSet<InputAction>();
        if (allowedActions != null)
        {
            foreach (var action in allowedActions)
            {
                if (action != null)
                {
                    allowedSet.Add(action);
                }
            }
        }

        foreach (var actionMap in inputActionAsset.actionMaps)
        {
            foreach (var action in actionMap.actions)
            {
                if (allowedSet.Contains(action))
                {
                    if (!action.enabled)
                    {
                        action.Enable();
                    }
                }
                else
                {
                    if (action.enabled)
                    {
                        action.Disable();
                    }
                }
            }
        }
    }
    /// <summary>
    /// 恢复所有输入动作为启用状态。
    /// </summary>
    public void EnableAllInputs()
    {
        if (inputActionAsset == null)
        {
            Debug.LogWarning("PlayerInput: InputActionAsset 未初始化，无法启用输入。");
            return;
        }

        foreach (var actionMap in inputActionAsset.actionMaps)
        {
            foreach (var action in actionMap.actions)
            {
                if (!action.enabled)
                {
                    action.Enable();
                }
            }
        }
    }

    /// <summary>
    /// 启用所有输入动作，除指定输入外维持禁用状态。
    /// </summary>
    /// <param name="excludedActions">需要保持禁用的输入动作。</param>
    public void EnableAllInputsExcept(params InputAction[] excludedActions)
    {
        if (inputActionAsset == null)
        {
            Debug.LogWarning("PlayerInput: InputActionAsset 未初始化，无法启用输入。");
            return;
        }

        var excludedSet = new HashSet<InputAction>();
        if (excludedActions != null)
        {
            foreach (var action in excludedActions)
            {
                if (action != null)
                {
                    excludedSet.Add(action);
                }
            }
        }

        foreach (var actionMap in inputActionAsset.actionMaps)
        {
            foreach (var action in actionMap.actions)
            {
                if (excludedSet.Contains(action))
                {
                    if (action.enabled)
                    {
                        action.Disable();
                    }
                }
                else
                {
                    if (!action.enabled)
                    {
                        action.Enable();
                    }
                }
            }
        }
    }
}
