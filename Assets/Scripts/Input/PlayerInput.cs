using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoSingleton<PlayerInput>
{
    private PlayerInputAction playerInputAction;

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
    public bool Fire => playerInputAction.Control.Fire.IsPressed();

    public bool Weapon1Pressed => playerInputAction.Control.Weapon.WasPressedThisFrame();
    public bool RotatePressed => playerInputAction.Control.Rotate.IsPressed();

    // UI 输入
    public bool InventoryPressed => playerInputAction.Control.OpenInventory.WasPressedThisFrame();

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
    //public event Action OnBoardShipEvent;
    //public event Action OnDriveBoatEvent;
    public event Action OnInteractionEvent;
    public event Action<bool> IsAttackedEvent;

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
    }

    void OnDestroy()
    {
        playerInputAction.Control.OpenInventory.performed -= OnInventoryInput;
        playerInputAction.Control.OpenEvent.performed -= OnLootOpen;
        playerInputAction.Control.InteractionEvent.performed -= OnInteraction;
        //playerInputAction.Control.InteractionEvent.performed -= OnBoardShip;
        //playerInputAction.Control.InteractionEvent.performed -= OnDriveBoat;
        playerInputAction.Control.Weapon.performed -= OnIsAttackedInput;
    }

    void OnInventoryInput(InputAction.CallbackContext context)
    {
        // 防止商店打开时切换背包
        var shopPanel = UIManger.Instance.GetPanel<ShopPanel>();
        if (shopPanel != null && !shopPanel.IsVisible)
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

    public void DisableMovementAndLook()
    {
        playerInputAction.Control.Move.Disable();
        playerInputAction.Control.Look.Disable();
    }
    
    public void EnableMovementAndLook()
    {
        playerInputAction.Control.Move.Enable();
        playerInputAction.Control.Look.Enable();
    }
}
