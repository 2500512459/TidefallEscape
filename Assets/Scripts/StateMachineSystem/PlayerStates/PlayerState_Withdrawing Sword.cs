using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerState_WithdrawingSword", menuName = "StateMachine/Player/WithdrawingSword")]
public class PlayerState_WithdrawingSword : PlayerState
{
    // 拔剑
    public override void Enter()
    {
        base.Enter();
        playerCtrl.weaponState = PlayerCtrl.WeaponState.Drawing;
    }
    public override void LogicUpdate()
    {
        if (IsAnimationFinished)
        {
            playerCtrl.weaponState = PlayerCtrl.WeaponState.Armed;
            if(input.Move)
            {
                stateMachine.SwitchState(typeof(PlayerState_Run));
            }
            else
            {
                stateMachine.SwitchState(typeof(PlayerState_IdleArmed));
            }
        }
    }

    public override void PhysicsUpdate()
    {
    }
}
