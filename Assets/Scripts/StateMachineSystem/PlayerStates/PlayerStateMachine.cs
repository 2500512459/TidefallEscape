using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class PlayerStateMachine : StateMachine
{
    private Animator animator;
    private PlayerInput input;
    private PlayerCtrl playerCtrl;
    [SerializeField] private PlayerState[] States;
    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        input = PlayerInput.Instance;
        playerCtrl = GetComponent<PlayerCtrl>();
        stateTable = new Dictionary<Type, IState>(States.Length);
        // 初始化状态
        foreach (var state in States)
        {
            state.Init(animator, this, input, playerCtrl);
            stateTable.Add(state.GetType(), state);
        }
    }

    void Start()
    {
        SwitchOn(stateTable[typeof(PlayerState_Idle)]);
    }
}
