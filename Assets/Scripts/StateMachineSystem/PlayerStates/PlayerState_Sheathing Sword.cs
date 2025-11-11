using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerState_SheathingSword", menuName = "StateMachine/Player/SheathingSword")]
public class PlayerState_SheathingSword : PlayerState
{
    // 收剑
    public override void Enter()
    {
        base.Enter();
        playerCtrl.weaponState = PlayerCtrl.WeaponState.Sheathing;
    }
    public override void LogicUpdate()
    {
        if (IsAnimationFinished)
        {
            playerCtrl.weaponState = PlayerCtrl.WeaponState.Sheathed;
            stateMachine.SwitchState(typeof(PlayerState_Idle));
        }
        
        
    }

    public override void PhysicsUpdate()
    {
    }
}
