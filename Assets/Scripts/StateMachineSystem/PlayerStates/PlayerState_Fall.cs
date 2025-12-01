using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerState_Fall", menuName = "StateMachine/Player/Fall")]
public class PlayerState_Fall : PlayerState
{
    [SerializeField] AnimationCurve speedCurve;
    public override void Enter()
    {
        base.Enter();
    }

    public override void LogicUpdate()
    {
        if (playerCtrl.isGround)
        {
            stateMachine.SwitchState(typeof(PlayerState_Land));
            return;
        }
        if (playerCtrl.isClimbing)
        {
            stateMachine.SwitchState(typeof(PlayerState_Climb));
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
        playerCtrl.SetVelocityY(speedCurve.Evaluate(StateDuration));
        playerCtrl.Move();
    }
}
