using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerState_Idle", menuName = "StateMachine/Player/Idle")]
public class PlayerState_Idle : PlayerState
{
    public override void Enter()
    {
        base.Enter();

    }


    public override void LogicUpdate()
    {
        if(playerCtrl.weaponState == PlayerCtrl.WeaponState.Drawing)
        {
            stateMachine.SwitchState(typeof(PlayerState_WithdrawingSword));
        }
        if(input.Jump)
        {
            stateMachine.SwitchState(typeof(PlayerState_Jump));
        }
        if (input.Move)
        {
            stateMachine.SwitchState(typeof(PlayerState_Run));
        }
        if (!playerCtrl.isGround && !playerCtrl.isSwimming)
        {
            stateMachine.SwitchState(typeof(PlayerState_Fall));
        }
        if (playerCtrl.isSwimming)
        {
            stateMachine.SwitchState(typeof(PlayerState_Swimming));
        }
    }

    public override void PhysicsUpdate()
    {

    }
}
