using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState : ScriptableObject, IState
{
    [SerializeField] string stateName;
    [SerializeField, Range(0f, 1f)] float transitionDuration = 0.1f;    // ¹ý¶ÉÊ±¼ä

    float stateStartTime;
    int stateHash;
    protected Animator animator;
    protected PlayerStateMachine stateMachine;
    protected PlayerInput input;
    protected PlayerCtrl playerCtrl;
    protected float currentSpeed;
    protected bool IsAnimationFinished => StateDuration >= animator.GetCurrentAnimatorStateInfo(0).length;
    protected float StateDuration => Time.time - stateStartTime;
    void OnEnable()
    {
        stateHash = Animator.StringToHash(stateName);
    }
    public void Init(Animator animator, PlayerStateMachine stateMachine, PlayerInput input, PlayerCtrl playerCtrl)
    {
        this.animator = animator;
        this.stateMachine = stateMachine;
        this.input = input;
        this.playerCtrl = playerCtrl;
    }

    public virtual void Enter()
    {
        animator.CrossFade(stateHash, transitionDuration);
        stateStartTime = Time.time;
    }

    public virtual void Exit()
    {
    }

    public virtual void LogicUpdate()
    {
    }

    public virtual void PhysicsUpdate()
    {
    }
}
