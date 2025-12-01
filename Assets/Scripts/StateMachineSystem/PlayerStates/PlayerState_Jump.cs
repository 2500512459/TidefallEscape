using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerState_Jump", menuName = "StateMachine/Player/Jump")]
public class PlayerState_Jump : PlayerState
{
    public override void Enter()
    {
        base.Enter();
        playerCtrl.Jump();
    }

    public override void LogicUpdate()
    {
        if (playerCtrl.isFalling)
        {
            stateMachine.SwitchState(typeof(PlayerState_Fall));
            return;
        }
        if (playerCtrl.isClimbing)
        {
            stateMachine.SwitchState(typeof(PlayerState_Climb));
            return;
        }
        if(IsAnimationFinished)
        {
            stateMachine.SwitchState(typeof(PlayerState_Idle));
            return;
        }
    }

    public override void PhysicsUpdate()
    {
        playerCtrl.Move();
    }
}
