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

    private bool isInitialized = false;

    void Awake()
    {
        if (isInitialized) return;

        // 禁用组件，防止Update在初始化前运行导致空指针
        this.enabled = false;
    }

    public void Initialize()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("PlayerStateMachine: Animator not found in children!");
            return;
        }

        input = PlayerInput.Instance;
        playerCtrl = GetComponent<PlayerCtrl>();
        stateTable = new Dictionary<Type, IState>(States.Length);
        
        // 初始化状态
        foreach (var state in States)
        {
            state.Init(animator, this, input, playerCtrl);
            stateTable.Add(state.GetType(), state);
        }

        // 进入初始状态
        SwitchOn(stateTable[typeof(PlayerState_Idle)]);
        
        // 启用组件，开始Update循环
        this.enabled = true;
        isInitialized = true;
    }
}
