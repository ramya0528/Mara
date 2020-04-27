﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicMeleeAI : BaseAIMovementController
{
    [HideInInspector] public Timer _hitStunTimer;
    [HideInInspector] public bool _useHitStun;

    // Start is called before the first frame update
    void Start()
    {
        stateMachine.ChangeState(new BasicMeleeIdleState());
        _meleeEnemy = this;
    }

    protected override void Update()
    {
        base.Update();

        _anim.SetFloat("Blend", _agent.velocity.magnitude);

        if (_health.GetHealth() <= 0)
        {
            KillThis();
        }
    }

    private void KillThis()
    {
        stateMachine.ChangeState(new DeadState());
        _anim.SetTrigger("Dead");
        _agent.SetDestination(transform.position);
    }

    public void Attack()
    {
        _anim.SetTrigger("Attack");
    }

    public override void TakeDamage(Hitbox hitbox)
    {
        stateMachine.ChangeState(new BasicMeleeIdleState());
        EnableHitstun(hitbox.hitstunTime);
        base.TakeDamage(hitbox);
    }

    public void EnableHitstun(float duration)
    {
        _hitStunTimer = new Timer(duration);
        _useHitStun = true;
        _anim.SetTrigger("Hurt");
        _anim.SetBool("InHitstun", true);
    }

    public void DisabelHitStun()
    {
        _useHitStun = false;
        _anim.SetBool("InHitstun", false);
    }

}

public class BasicMeleeIdleState : BaseIdleState
{
    public override void EnterState(BaseAIMovementController owner)
    {
        _chasingState = new BasicMeleeChasingState();
    }
    public override void UpdateState(BaseAIMovementController owner)
    {
        if (owner._meleeEnemy._useHitStun)
        {
            owner._meleeEnemy._hitStunTimer.Time += Time.deltaTime;
            if (owner._meleeEnemy._hitStunTimer.Expired)
            {
                owner._meleeEnemy._hitStunTimer.Reset();
                owner._meleeEnemy.DisabelHitStun();
            }
        }
        else
        {
            base.UpdateState(owner);
        }

    }
}

public class BasicMeleeChasingState : BaseChasingState
{
    public override void EnterState(BaseAIMovementController owner)
    {
        _attackingState = new BasicMeleeAttackingState();
        _returnToIdleState = new BasicMeleeReturnToIdleState();
    }
}

public class BasicMeleeAttackingState : BaseAttackingState
{
    public override void EnterState(BaseAIMovementController owner)
    {
        _chasingState = new BasicMeleeChasingState();
    }

    public override void UpdateState(BaseAIMovementController owner)
    {
        //lägg in attack metod här
        owner._meleeEnemy.Attack();
        base.UpdateState(owner);
    }
}

public class BasicMeleeReturnToIdleState : BaseReturnToIdlePosState
{
    public override void EnterState(BaseAIMovementController owner)
    {
        _chasingState = new BasicMeleeChasingState();
        _idleState = new BasicMeleeIdleState();

    }
}
