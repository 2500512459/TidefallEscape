using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerState_AimIdle", menuName = "StateMachine/Player/AimIdle")]
public class PlayerState_AimIdle : PlayerState
{
    public override void Enter()
    {
        base.Enter();
        
    }

    public override void LogicUpdate()
    {
        if (playerCtrl.isSwimming)
        {
            stateMachine.SwitchState(typeof(PlayerState_Swimming));
            return;
        }
        if (!playerCtrl.isGround)
        {
            stateMachine.SwitchState(typeof(PlayerState_Fall));
            return;
        }

        // 如果鼠标左键点击，进入 PlayerState_AimRecoil
        if (input.Fire)
        {
            stateMachine.SwitchState(typeof(PlayerState_AimRecoil));
            return;
        }

        // 如果鼠标右键松开，进入 PlayerState_IdleArmed
        if (!input.RotatePressed)
        {
            stateMachine.SwitchState(typeof(PlayerState_IdleArmed));
            return;
        }

        // 如果进行了移动，进入 PlayerState_AimWalk
        if (input.Move)
        {
            stateMachine.SwitchState(typeof(PlayerState_AimWalk));
            return;
        }

        playerCtrl.AimTurn();
    }

    public override void PhysicsUpdate()
    {
        
    }
}
