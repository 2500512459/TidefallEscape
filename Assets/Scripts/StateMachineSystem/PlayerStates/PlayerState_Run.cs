using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerState_Run", menuName = "StateMachine/Player/Run")]
public class PlayerState_Run : PlayerState
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
        if(playerCtrl.weaponState == PlayerCtrl.WeaponState.Sheathing)
        {
            stateMachine.SwitchState(typeof(PlayerState_SheathingSword));
        }
        if (input.Jump)
        {
            stateMachine.SwitchState(typeof(PlayerState_Jump));
        }

        if (!input.Move && playerCtrl.weaponState == PlayerCtrl.WeaponState.Sheathed)
        {
            stateMachine.SwitchState(typeof(PlayerState_Idle));
        }
        else if (!input.Move && playerCtrl.weaponState == PlayerCtrl.WeaponState.Armed)
        {
            stateMachine.SwitchState(typeof(PlayerState_IdleArmed));
        }
        
        if (!playerCtrl.isGround)
        {
            stateMachine.SwitchState(typeof(PlayerState_Fall));
        }
        if (playerCtrl.isClimbing)
        {
            if (playerCtrl.weaponState == PlayerCtrl.WeaponState.Armed)
                stateMachine.SwitchState(typeof(PlayerState_SheathingSword));
            else
                stateMachine.SwitchState(typeof(PlayerState_Climb));
        }
        
    }

    public override void PhysicsUpdate()
    {
        playerCtrl.Move();
    }
}
