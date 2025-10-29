using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InputManager : MonoSingleton<InputManager>
{
#if ENABLE_INPUT_SYSTEM
    public PlayerInput playerInput;
#endif

    [Header("移动与视角输入")]
    public Vector2 inputMove = Vector2.zero;
    public Vector2 inputLook = Vector2.zero;
    public bool isBoosting = false;// 是否正在按下Shift（加速中）
    public bool inputRotateState = false;

    [Header("状态标记")]
    public bool isInventoryOpen = false;  // 背包是否打开
    public bool isLootOpen = false;       // 掉落栏是否打开

    [Header("检测参数")]

    public PlayerShip playerShip;             // 玩家Transform引用

    // UI或系统事件
    public event Action<bool> OpenInventoryEvent;   //打开背包
    public event Action LootPressedEvent;           //打开宝箱
    void Start()
    {

    }

    void Update()
    {
        // 这里可以根据状态锁定鼠标
        if (isInventoryOpen)
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

#if ENABLE_INPUT_SYSTEM
    // =====================
    // 输入事件
    // =====================
    public void OnMove(InputAction.CallbackContext value)
    {
        if (isInventoryOpen) return;  // 打开背包时禁止移动
        MoveInput(value.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext value)
    {
        if (isInventoryOpen) return;  // 打开背包时禁止视角旋转
        LookInput(value.ReadValue<Vector2>());
    }

    public void OnRotate(InputAction.CallbackContext value)
    {
        if (isInventoryOpen) return;
        RotateInput(value.performed);
    }
    // Shift 加速
    public void OnBoost(InputAction.CallbackContext value)
    {
        if (value.phase == InputActionPhase.Started)
            isBoosting = true;
        else if (value.phase == InputActionPhase.Canceled)
            isBoosting = false;
    }
    // Tab 打开/关闭背包
    public void OnInventory(InputAction.CallbackContext value)
    {
        if (value.phase == InputActionPhase.Started)
        {
            isInventoryOpen = !isInventoryOpen;
            OpenInventoryEvent?.Invoke(isInventoryOpen);

            isLootOpen = isInventoryOpen;
        }
    }
    // F 打开/关闭背包和掉落栏
    public void OnLoot(InputAction.CallbackContext value)
    {
        if (value.phase != InputActionPhase.Started) return;

        LootPressedEvent?.Invoke();
    }
#endif

    // =====================
    // 输入处理函数
    // =====================
    public void MoveInput(Vector2 newMoveDirection)
    {
        inputMove = newMoveDirection;
    }

    public void LookInput(Vector2 newLook)
    {
        inputLook = newLook;
    }

    public void RotateInput(bool newRotateState)
    {
        inputRotateState = newRotateState;
    }

}
