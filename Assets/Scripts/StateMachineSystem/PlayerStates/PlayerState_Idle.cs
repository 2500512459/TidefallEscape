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
            return;
        }
        if(input.Jump)
        {
            stateMachine.SwitchState(typeof(PlayerState_Jump));
            return;
        }
        if (input.Move)
        {
            stateMachine.SwitchState(typeof(PlayerState_Run));
            return;
        }
        if (!playerCtrl.isGround && !playerCtrl.isSwimming)
        {
            stateMachine.SwitchState(typeof(PlayerState_Fall));
            return;
        }
        if (playerCtrl.isSwimming)
        {
            stateMachine.SwitchState(typeof(PlayerState_Floating));
            return;
        }
    }

    public override void PhysicsUpdate()
    {

    }
}
