using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoSingleton<PlayerInput>
{
    public PlayerInputAction playerInputAction;

    // ======================
    // 输入状态属性
    // ======================
    public Vector2 axes => playerInputAction.Control.Move.ReadValue<Vector2>();
    public Vector2 inputLook => playerInputAction.Control.Look.ReadValue<Vector2>();

    public bool Move => axes.sqrMagnitude > 0.01f;
    public float AxesX => axes.x;
    public float AxesY => axes.y;

    public bool Jump => playerInputAction.Control.Jump.WasPressedThisFrame();
    public bool StopJump => playerInputAction.Control.Jump.WasReleasedThisFrame();

    public bool Sprint => playerInputAction.Control.BoostMove.IsPressed();
    public bool Fire => playerInputAction.Control.Fire.WasReleasedThisFrame();

    public bool WeaponPressed => playerInputAction.Control.Weapon.WasPressedThisFrame();
    public bool SwitchWeaponPressed => playerInputAction.Control.SwitchWeapon.WasPressedThisFrame();
    public bool RotatePressed => playerInputAction.Control.Rotate.IsPressed();

    // UI 输入
    public bool InventoryPressed => playerInputAction.Control.OpenInventory.WasPressedThisFrame();
    public bool QuestPressed => playerInputAction.Control.Quest.WasPressedThisFrame();
    // 事件输入
    //public bool OpenEvent => playerInputAction.Control.OpenEvent.WasPressedThisFrame();
    //public bool InteractionEvent => playerInputAction.Control.InteractionEvent.WasPressedThisFrame();


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

    protected override void Awake()
    {
        playerInputAction = new PlayerInputAction();

        // 注册事件监听
        playerInputAction.Control.OpenInventory.performed += OnInventoryInput;
        playerInputAction.Control.OpenEvent.performed += OnLootOpen;            // 打开宝箱F
        playerInputAction.Control.InteractionEvent.performed += OnInteraction;
        //playerInputAction.Control.InteractionEvent.performed += OnBoardShip;    // 上船E
        //playerInputAction.Control.InteractionEvent.performed += OnDriveBoat;    // 驾驶船E
        playerInputAction.Control.Weapon.performed += OnIsAttackedInput;
        playerInputAction.Control.Quest.performed += OnQuestInput;
        playerInputAction.Control.SwitchWeapon.performed += OnSwitchWeaponInput;
    }

    void OnDestroy()
    {
        // 移除事件监听器并禁用输入控制
        if (playerInputAction != null)
        {
            try
            {
                playerInputAction.Control.OpenInventory.performed -= OnInventoryInput;
                playerInputAction.Control.OpenEvent.performed -= OnLootOpen;
                playerInputAction.Control.InteractionEvent.performed -= OnInteraction;
                //playerInputAction.Control.InteractionEvent.performed -= OnBoardShip;
                //playerInputAction.Control.InteractionEvent.performed -= OnDriveBoat;
                playerInputAction.Control.Weapon.performed -= OnIsAttackedInput;
                playerInputAction.Control.Quest.performed -= OnQuestInput;
                playerInputAction.Control.SwitchWeapon.performed -= OnSwitchWeaponInput;

                // 禁用输入控制，防止内存泄漏和性能问题
                // 这是 PlayerInputAction 析构函数的要求
                if (playerInputAction.Control.enabled)
                {
                    playerInputAction.Control.Disable();
                }
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

    // ======================
    // 控制启用/禁用
    // ======================
    public void EnableControlInput()
    {
        playerInputAction.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void DisableControlInput()
    {
        playerInputAction.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void DisableMovementAndLook(bool disableQuestInput = false, bool disableInteractionInput = false)
    {
        playerInputAction.Control.Move.Disable();
        playerInputAction.Control.Look.Disable();
        if (disableQuestInput)
        {
            playerInputAction.Control.Quest.Disable();
        }
        if (disableInteractionInput)
        {
            playerInputAction.Control.InteractionEvent.Disable();
        }
    }
    
    public void EnableMovementAndLook(bool enableQuestInput = false, bool enableInteractionInput = false)
    {
        playerInputAction.Control.Move.Enable();
        playerInputAction.Control.Look.Enable();
        if (enableQuestInput)
        {
            playerInputAction.Control.Quest.Enable();
        }
        if (enableInteractionInput)
        {
            playerInputAction.Control.InteractionEvent.Enable();
        }
    }


    /// <summary>
    /// 禁用除指定按键之外的所有输入动作。
    /// </summary>
    /// <param name="allowedActions">允许继续启用的输入动作集合。</param>
    public void DisableAllInputsExcept(params InputAction[] allowedActions)
    {
        if (playerInputAction == null || playerInputAction.asset == null)
        {
            Debug.LogWarning("PlayerInputAction 未初始化，无法禁用输入。");
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

        foreach (var actionMap in playerInputAction.asset.actionMaps)
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
        if (playerInputAction == null || playerInputAction.asset == null)
        {
            Debug.LogWarning("PlayerInputAction 未初始化，无法启用输入。");
            return;
        }

        foreach (var actionMap in playerInputAction.asset.actionMaps)
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
        if (playerInputAction == null || playerInputAction.asset == null)
        {
            Debug.LogWarning("PlayerInputAction 未初始化，无法启用输入。");
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

        foreach (var actionMap in playerInputAction.asset.actionMaps)
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
