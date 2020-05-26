﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public class MylingAI : BaseAIMovementController
{
    [Header("References")]
    [SerializeField] public GameObject _healthBar;
    [SerializeField] public HitboxGroup _hitBox;

    private void Start()
    {
        stateMachine.ChangeState(new MylingIdleState());
        mylingAI = this;

        for (int i = 0; i < _healthBar.transform.childCount; i++)
        {
            _healthBar.transform.GetChild(i).gameObject.SetActive(false);
        }
    }

    private new void Update()
    {
        print(stateMachine.currentState);

        base.Update();
    }

    public override void KillThis()
    {
        stateMachine.ChangeState(new DeadState());
        _anim.SetBool("Dead", true);
        _agent.SetDestination(transform.position);
        transform.tag = "Untagged";
    }

    public override void Parried() { /* Cant parry this motherfucker */ }

    public override void TakeDamage(HitboxValues hitbox, Entity attacker)
    {
        base.TakeDamage(hitbox, attacker);

        for (int i = 0; i < _healthBar.transform.childCount; i++)
        {
            // _healthBar.transform.GetChild(i).gameObject.SetActive(true);
        }
    }
}

public class MylingIdleState : BaseIdleState
{
    private float time = 0;
    private float blend;
    public float idleBlendDuration = 0.05f;

    public override void EnterState(BaseAIMovementController owner)
    {
        _chasingState = new MylingChasingState();
        base.EnterState(owner);

        blend = owner._anim.GetFloat("Blend");
    }

    public override void UpdateState(BaseAIMovementController owner)
    {
        owner._anim.SetFloat("Blend", Mathf.Lerp(blend, 0, time / (idleBlendDuration * blend)));
        time += Time.deltaTime;

        base.UpdateState(owner);
    }
}

public class MylingChasingState : BaseChasingState
{
    private float time = 0;
    public float idleBlendDuration = 0.05f;

    public override void EnterState(BaseAIMovementController owner)
    {
        _returnToIdleState = new MylingRetToIdleState();
        _attackingState = new MylingAttackingState();

        base.EnterState(owner);
    }

    public override void UpdateState(BaseAIMovementController owner)
    {
        float magnitude = owner._agent.speed;
        owner._anim.SetFloat("Blend", Mathf.Lerp(0, magnitude, time / (idleBlendDuration * magnitude)));
        time += Time.deltaTime;

        base.UpdateState(owner);
    }
}

public class MylingAttackingState : BaseAttackingState
{
    private float time = 0;
    public float idleBlendDuration = 0.1f;

    public override void EnterState(BaseAIMovementController owner)
    {
        _chasingState = new MylingChasingState();

        float magnitude = owner._agent.speed;
        owner._anim.SetFloat("Blend", Mathf.Lerp(magnitude, 0, time / (idleBlendDuration * magnitude)));

        owner.mylingAI._hitBox.enabled = true;
        owner.GenerateNewAttackTimer();

        base.EnterState(owner);
    }

    public override void UpdateState(BaseAIMovementController owner)
    {
        float magnitude = owner._agent.speed;
        owner._anim.SetFloat("Blend", Mathf.Lerp(magnitude, 0, time / (idleBlendDuration * magnitude)));

        base.UpdateState(owner);
        owner._attackRateTimer += Time.deltaTime;
        time += Time.deltaTime;

        if (owner._attackRateTimer.Expired)
        {
            owner._anim.SetTrigger("Attack");
        }

        if (owner._animationOver)
        {
            owner.KillThis();
        }
    }

    public override void ExitState(BaseAIMovementController owner)
    {
        owner.mylingAI._hitBox.enabled = false;
        owner.GenerateNewAttackTimer();

        base.ExitState(owner);
    }
}

public class MylingRetToIdleState : BaseReturnToIdlePosState
{
    public override void EnterState(BaseAIMovementController owner)
    {
        _chasingState = new MylingChasingState();
        _idleState = new MylingIdleState();

        base.EnterState(owner);
    }

    public override void ExitState(BaseAIMovementController owner)
    {
        for (int i = 0; i < owner.mylingAI._healthBar.transform.childCount; i++)
        {
            owner.mylingAI._healthBar.transform.GetChild(i).gameObject.SetActive(true);
        }

        base.ExitState(owner);
    }
}
