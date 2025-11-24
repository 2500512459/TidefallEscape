using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerState_DrawArrow", menuName = "StateMachine/Player/DrawArrow")]
public class PlayerState_DrawArrow : PlayerState
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

        // 如果鼠标右键松开，进入 PlayerState_IdleArmed
        if (!input.RotatePressed)
        {
            stateMachine.SwitchState(typeof(PlayerState_IdleArmed));
            return;
        }
        // 动画播放完成后进入 PlayerState_AimIdle
        if (IsAnimationFinished)
        {
            stateMachine.SwitchState(typeof(PlayerState_AimIdle));
            return;
        }

        playerCtrl.AimTurn();
    }

    public override void PhysicsUpdate()
    {
    }
}
