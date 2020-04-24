﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedEnemyAI : BaseAIMovementController 
{
    [SerializeField] private GameObject _projectile;
    [SerializeField] private Transform _projectileSpawnPos;
    [SerializeField] private float _firerate;

    [HideInInspector] public Timer firerateTimer;

    [HideInInspector] public Timer _hitStunTimer;
    [HideInInspector] public bool _useHitStun;

    [HideInInspector] public Animator _anim;

    protected override void Awake()
    {
        base.Awake();
        firerateTimer = new Timer(_firerate);
        _anim = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        stateMachine.ChangeState(new RangedEnemyIdleState());
        rangedAI = this;
    }

    protected override void Update()
    {
        base.Update();
        firerateTimer.Time += Time.deltaTime;
        _anim.SetFloat("Blend", agent.velocity.magnitude);
    }

    public void Attack()
    {
        _anim.SetTrigger("Attack");
        if (firerateTimer.Expired)
        {
            Instantiate(_projectile, _projectileSpawnPos.position, _projectileSpawnPos.rotation);
            firerateTimer.Reset();
        }
    }

    public override void TakeDamage(Hitbox hitbox)
    {
        stateMachine.ChangeState(new RangedEnemyIdleState());
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

    public void DisableHitStun()
    {
        _useHitStun = false;
        _anim.SetBool("InHitstun", false);
    }
}

public class RangedEnemyIdleState : BaseIdleState
{
    public override void EnterState(BaseAIMovementController owner)
    {
        _chasingState = new RangedEnemyChasingState();
    }

    public override void UpdateState(BaseAIMovementController owner)
    {
        if (owner.rangedAI._useHitStun)
        {
            owner.rangedAI._hitStunTimer.Time += Time.deltaTime;
            if (owner.rangedAI._hitStunTimer.Expired)
            {
                owner.rangedAI._hitStunTimer.Reset();
                owner.rangedAI.DisableHitStun();
            }
        }
        else
        {
            base.UpdateState(owner);
        }
    }
}

public class RangedEnemyChasingState : BaseChasingState
{
    public override void EnterState(BaseAIMovementController owner)
    {
        _attackingState = new RangedEnemyAttackingState();
        _returnToIdleState = new RangedEnemyReturnToIdleState();
    }

    public override void UpdateState(BaseAIMovementController owner)
    {
        base.UpdateState(owner);
    }
}

public class RangedEnemyAttackingState : BaseAttackingState
{
    public override void EnterState(BaseAIMovementController owner)
    {
        _chasingState = new RangedEnemyChasingState();
        owner.rangedAI.firerateTimer.Reset();
    }

    public override void UpdateState(BaseAIMovementController owner)
    {
        owner.rangedAI.Attack();

        base.UpdateState(owner);
    }
}

public class RangedEnemyReturnToIdleState : BaseReturnToIdlePosState
{
    public override void EnterState(BaseAIMovementController owner)
    {
        _chasingState = new RangedEnemyChasingState();
        _idleState = new RangedEnemyIdleState();
    }

    public override void UpdateState(BaseAIMovementController owner)
    {
        base.UpdateState(owner);
    }
}