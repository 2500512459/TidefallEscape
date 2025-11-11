using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerState_Attack01", menuName = "StateMachine/Player/Attack01")]
public class PlayerState_Attack01 : PlayerState
{
    [SerializeField] float minNextAttackTime = 0.3f; // 允许连击的最早时间
    [SerializeField] float maxNextAttackTime = 0.8f; // 可输入下一段攻击的最大时间
    private float attackStartTime;
    public override void Enter()
    {
        base.Enter();
        attackStartTime = Time.time;
        playerCtrl.isAttacking = false; // 重置输入标志
    }
    public override void LogicUpdate()
    {
        float elapsed = Time.time - attackStartTime;
        // 若在攻击时间内按下下一次攻击，则进入Attack02
        if (playerCtrl.isAttacking && elapsed >= minNextAttackTime && elapsed <= maxNextAttackTime)
        {
            playerCtrl.isAttacking = false;
            stateMachine.SwitchState(typeof(PlayerState_Attack02));
            return;
        }

        // 攻击动画结束或超时回到IdleArmed
        if (IsAnimationFinished)
        {
            stateMachine.SwitchState(typeof(PlayerState_IdleArmed));
            return;
        }
    }
}
