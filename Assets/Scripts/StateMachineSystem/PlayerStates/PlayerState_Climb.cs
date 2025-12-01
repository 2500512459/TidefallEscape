using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerState_Climb", menuName = "StateMachine/Player/Climb")]
public class PlayerState_Climb : PlayerState
{
    public override void Enter()
    {
        base.Enter();
    }
    public override void LogicUpdate()
    {
        if (playerCtrl.isClimbOver)
        {
            stateMachine.SwitchState(typeof(PlayerState_ClimbOver));
            return;
        }
        if (input.Jump)
        {
            stateMachine.SwitchState(typeof(PlayerState_Fall));
            return;
        }
        if (playerCtrl.isGround)
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
