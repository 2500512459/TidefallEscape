using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerState_IdleArmed", menuName = "StateMachine/Player/IdleArmed")]
public class PlayerState_IdleArmed : PlayerState
{
    [SerializeField] public float maxArmedIdleTime = 5f;  // 超过这段时间自动收剑
    private float armedIdleTime = 0f;     // 进入IdleArmed状态的时间

    public override void Enter()
    {
        base.Enter();
        armedIdleTime = Time.time;
    }
    public override void LogicUpdate()
    {
        // 超时自动收剑
        if (Time.time - armedIdleTime > maxArmedIdleTime)
        {
            playerCtrl.weaponState = PlayerCtrl.WeaponState.Sheathing;
            stateMachine.SwitchState(typeof(PlayerState_SheathingSword));
            return;
        }

        // 如果按下攻击
        if (playerCtrl.isAttacking)
        {
            playerCtrl.isAttacking = false; // 消耗这次攻击输入
            stateMachine.SwitchState(typeof(PlayerState_Attack01));
            return;
        }

        if(input.Jump)
        {
            stateMachine.SwitchState(typeof(PlayerState_Jump));
            return;
        }
        if (input.Move)
        {
            stateMachine.SwitchState(typeof(PlayerState_Run));
            return;
        }
        if (!playerCtrl.isGround)
        {
            stateMachine.SwitchState(typeof(PlayerState_Fall));
            return;
        }

    }

    public override void PhysicsUpdate()
    {
    }
}
