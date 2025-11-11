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
        if (playerCtrl.isClimbing)
        {
            stateMachine.SwitchState(typeof(PlayerState_Climb));
        }
        if (playerCtrl.isGround)
        {
            stateMachine.SwitchState(typeof(PlayerState_Idle));
        }
        if (playerCtrl.isFalling)
        {
            stateMachine.SwitchState(typeof(PlayerState_Fall));
        }
    }
    public override void PhysicsUpdate()
    {
        playerCtrl.Move();
    }
}
