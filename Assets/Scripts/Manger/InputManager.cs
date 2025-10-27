using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif


public class InputManager : MonoSingleton<InputManager>
{
    //input
#if ENABLE_INPUT_SYSTEM
    public PlayerInput playerInput;
#endif
    public Vector2 inputMove = Vector2.zero;
    public Vector2 inputLook = Vector2.zero;
    public bool inputRotateState = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame 
    void Update()
    {

    }


    //INPUT MESSAGES
#if ENABLE_INPUT_SYSTEM
    public void OnMove(InputAction.CallbackContext value)
    {
        MoveInput(value.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext value)
    {
        LookInput(value.ReadValue<Vector2>());
    }


    public void OnRotate(InputAction.CallbackContext value)
    {
        RotateInput(value.performed);
    }


#endif


    public void MoveInput(Vector2 newMoveDirection)
    {
        inputMove = newMoveDirection;
    }

    public void LookInput(Vector2 newLook)
    {
        //if (!IsWeaponState())
            inputLook = newLook;
    }

    public void RotateInput(bool newRotateState)
    {
        inputRotateState = newRotateState;
    }
}

