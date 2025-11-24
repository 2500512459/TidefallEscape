using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerState_AimWalk", menuName = "StateMachine/Player/AimWalk")]
public class PlayerState_AimWalk : PlayerState
{
    [SerializeField] float slowMoveTime = 0.2f; // 防止动画刚开始就移动
    [SerializeField] float slowMoveSpeedRatio = 0.3f; // 慢速移动时的速度比例
    private float aimStartTime;
    private float originalWalkSpeed;
    private bool isSlowMoving = false;

    public override void Enter()
    {
        base.Enter();
        aimStartTime = Time.time;
        originalWalkSpeed = playerCtrl.walkSpeed;
    }

    public override void Exit()
    {
        base.Exit();
        // 恢复原始速度
        if (isSlowMoving)
        {
            playerCtrl.walkSpeed = originalWalkSpeed;
            isSlowMoving = false;
        }
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

        // 如果鼠标左键点击，进入 PlayerState_AimRecoil
        if (input.Fire)
        {
            stateMachine.SwitchState(typeof(PlayerState_AimRecoil));
            return;
        }

        // 如果鼠标右键松开，进入 PlayerState_IdleArmed
        if (!input.RotatePressed)
        {
            stateMachine.SwitchState(typeof(PlayerState_IdleArmed));
            return;
        }

        // 如果停止移动，回到 PlayerState_AimIdle
        if (!input.Move)
        {
            stateMachine.SwitchState(typeof(PlayerState_AimIdle));
            return;
        }

        // 在slowMoveTime时间内，临时修改walkSpeed实现慢速移动
        if(Time.time - aimStartTime < slowMoveTime)
        {
            if (!isSlowMoving)
            {
                playerCtrl.walkSpeed = originalWalkSpeed * slowMoveSpeedRatio;
                isSlowMoving = true;
            }
        }
        else
        {
            if (isSlowMoving)
            {
                playerCtrl.walkSpeed = originalWalkSpeed;
                isSlowMoving = false;
            }
        }

        playerCtrl.AimTurn();
    }

    public override void PhysicsUpdate()
    {
        playerCtrl.Move();
    }
}
