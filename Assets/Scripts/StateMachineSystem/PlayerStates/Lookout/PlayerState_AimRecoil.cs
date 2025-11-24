using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerState_AimRecoil", menuName = "StateMachine/Player/AimRecoil")]
public class PlayerState_AimRecoil : PlayerState
{
    private ThirdPersonShooterController tpsController => stateMachine.GetComponent<ThirdPersonShooterController>();
    public override void Enter()
    {
        base.Enter();
        tpsController.SetHandAimRig(0f);
    }
    public override void Exit()
    {
        base.Exit();
        tpsController.SetHandAimRig(1f);
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

        // 动画播放完成后，根据鼠标右键和移动状态决定进入哪个状态
        if (IsAnimationFinished)
        {
            // 如果鼠标右键还按下
            if (input.RotatePressed)
            {
                if (input.Move)
                {
                    stateMachine.SwitchState(typeof(PlayerState_AimWalk));
                }
                else
                {
                    stateMachine.SwitchState(typeof(PlayerState_AimIdle));
                }
            }
            // 如果鼠标右键没按下
            else
            {
                if (input.Move)
                {
                    stateMachine.SwitchState(typeof(PlayerState_Run));
                }
                else
                {
                    stateMachine.SwitchState(typeof(PlayerState_IdleArmed));
                }
            }
            return;
        }

        playerCtrl.AimTurn();
    }

    public override void PhysicsUpdate()
    {
        
    }
}
