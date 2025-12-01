using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerState_Swimming", menuName = "StateMachine/Player/Swimming")]
public class PlayerState_Swimming : PlayerState
{
    public override void Enter()
    {
        base.Enter();
    }
    public override void LogicUpdate()
    {
        if(!input.Move)
        {
            stateMachine.SwitchState(typeof(PlayerState_Floating));
            return;
        }
        if (playerCtrl.isClimbing)
        {
            stateMachine.SwitchState(typeof(PlayerState_Climb));
            return;
        }
        if (playerCtrl.isClimbOver)
        {
            stateMachine.SwitchState(typeof(PlayerState_ClimbOver));
            return;
        }
        if (playerCtrl.isGround)
        {
            stateMachine.SwitchState(typeof(PlayerState_Idle));
            return;
        }
        if (playerCtrl.isFalling)
        {
            stateMachine.SwitchState(typeof(PlayerState_Fall));
            return;
        }
    }
    public override void PhysicsUpdate()
    {
        playerCtrl.Move();
    }
}
