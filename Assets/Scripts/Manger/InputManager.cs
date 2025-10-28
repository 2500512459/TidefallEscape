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
    public bool inputRotateState = false;

    [Header("状态标记")]
    public bool isInventoryOpen = false;  // 是否打开背包

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

    // Tab 打开/关闭背包
    public void OnInventory(InputAction.CallbackContext value)
    {
        if (value.performed)
        {
            isInventoryOpen = !isInventoryOpen;
            Debug.Log(isInventoryOpen);
            // 如果你有 UI 管理器，可以在这里控制背包显示
            // UIManager.Instance.ToggleInventoryPanel(isInventoryOpen);
        }
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
