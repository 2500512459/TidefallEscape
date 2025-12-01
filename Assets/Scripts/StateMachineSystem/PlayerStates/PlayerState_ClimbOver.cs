using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerState_ClimbOver", menuName = "StateMachine/Player/ClimbOver")]
public class PlayerState_ClimbOver : PlayerState
{
    public override void Enter()
    {
        base.Enter();
        playerCtrl.rb.velocity = Vector3.zero;
        playerCtrl.ClimbOver();
    }
    public override void LogicUpdate()
    {
        if (IsAnimationFinished)
        {
            stateMachine.SwitchState(typeof(PlayerState_Idle));
            return;
        }
    }
}
