﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.GlobalIllumination;

public class BossAIScript : Entity
{
    [System.Serializable]
    public struct PhaseOneStats
    {
        public float testP1value1;
        public float testP1value2;
        public float testP1value3;
    }
    [System.Serializable]
    public struct PhaseTwoStats
    {
        public float testP2value1;
        public float testP2value2;
        public float testP2value3;
    }

    public PhaseOneStats phaseOneStats;
    public PhaseTwoStats phaseTwoStats;

    [System.Serializable]
    public struct DesiredDistanceToAngleValues
    {
        public float desiredDistanceOffset;
        public float angle;
    }

    //lägga till en speed change variabel, beroende på hur nära man e desiredDistance
    [Tooltip("Elementen MÅSTE vara i ordnade utifrån Desired Distance Offset (från störst till minst)")]
    [SerializeField] public DesiredDistanceToAngleValues[] desiredDistanceToAngleValues;

    [NonSerialized] public List<float> desiredDistanceOffsetValues = new List<float>();
    [NonSerialized] public List<float> desiredDistanceAngleValues = new List<float>();

    public StateMachine<BossAIScript> phaseControllingStateMachine;

    public PreBossFightState preBossFightState = new PreBossFightState();
    public BossPhaseOneState bossPhaseOneState = new BossPhaseOneState();
    public BossPhaseTwoState bossPhaseTwoState = new BossPhaseTwoState();

    [SerializeField] public GameObject murkyWaterPrefab;

    [SerializeField] public LayerMask targetLayers;
    [SerializeField] public LayerMask dashCollisionLayers;

    //galet nog är alla variabler som heter test något, inte planerat att vara permanenta
    [SerializeField] [Range(0, 1)] public float testP2TransitionHP = 0.5f;

    [SerializeField] public HitboxGroup meleeAttackHitboxGroup;
    [SerializeField] public HitboxGroup drainAttackHitboxGroup;

    //lägga till ranomness på attack speed, kan göra det med 2 randoms för att få en normal distribution
    [SerializeField] public float testAttackSpeed = 5f;
    [SerializeField] public float testMinAttackCooldown = 1f;

    [SerializeField] public float testDrainDPS = 5f;
    [SerializeField] public float testDrainRange = 6f;
    [SerializeField] public float testDrainChargeTime = 7f;
    [SerializeField] public float testDrainAttackTime = 8f;

    [SerializeField] public float testMeleeRange = 6f;

    [SerializeField] public float desiredDistanceToPlayer = 5f;

    [SerializeField] [Range(0, 10)] public float testDashChansePerFrame = 0.1f;
    [SerializeField] [Range(0, 100)] public float testDashAttackChanse = 20f;

    [SerializeField] public float testDashSpeed = 20f;
    [SerializeField] public float testDashDistance = 5f;
    [SerializeField] public float testDashLagDurration = 0.5f;
    [SerializeField] public float testDashAcceleration = 2000f;
    [SerializeField] [Range(1, 180)] public float maxAngleDashAttackToPlayer = 90f;

    [SerializeField] public float aggroRange = 10f;
    [SerializeField] public float defaultTurnSpeed = 5f;
    [SerializeField] public float drainActiveTurnSpeed = 0.1f;

    [SerializeField] public float chasingSpeed = 5f;
    [SerializeField] public float chasingAcceleration = 20f;

    [SerializeField] public GameObject placeholoderDranBeam;
    

    [NonSerialized] public Animator bossAnimator;
    [NonSerialized] public float turnSpeed;

    [NonSerialized] public Vector3 movementDirection = new Vector3(0f, 0f, 1f);
    [NonSerialized] public Vector3 dashCheckOffsetVector;
    //[NonSerialized] public float dashCheckAngle;
    [NonSerialized] public Vector3 dashCheckBoxSize;

    [NonSerialized] public bool dashAttack;

    [NonSerialized] public NavMeshAgent agent;
    [NonSerialized] public GameObject player;


    [NonSerialized] public bool animationEnded;
    [NonSerialized] public bool facePlayerBool = true;
    [NonSerialized] public bool drainHitboxActive;
    [NonSerialized] public bool meleeHitboxActive;

    void Awake()
    {
        phaseControllingStateMachine = new StateMachine<BossAIScript>(this);

        //borde inte göras såhär at the end of the day men måste göra skit med spelaren då och vet inte om jag får det
        player = GlobalState.state.PlayerGameObject;

        testDrainDPS = phaseOneStats.testP1value1;

        bossAnimator = GetComponent<Animator>();
        agent = GetComponentInParent<NavMeshAgent>();

        turnSpeed = defaultTurnSpeed;

        dashCheckOffsetVector = new Vector3(0f, 1f, testDashDistance / 2);
        dashCheckBoxSize = new Vector3(0.75f, 0.75f, testDashDistance / 2);
    }

    void Start()
    {
        for (int i = 0; i < desiredDistanceToAngleValues.Length; i++)
        {
            desiredDistanceOffsetValues.Add(desiredDistanceToAngleValues[i].desiredDistanceOffset);
            desiredDistanceAngleValues.Add(desiredDistanceToAngleValues[i].angle);
        }

        agent.updateRotation = false;
        phaseControllingStateMachine.ChangeState(preBossFightState);
    }

    void Update()
    {
        phaseControllingStateMachine.Update();
    }

    public void FacePlayer()
    {
        Vector3 lookDirection = (player.transform.position - this.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(lookDirection.x, 0, lookDirection.z));
        this.transform.rotation = Quaternion.Slerp(this.transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
    }

    public override void TakeDamage(HitboxValues hitbox, Entity attacker)
    {
        GetComponent<EnemyHealth>().Damage(hitbox.damageValue);
        //spela hurtljud här
    }

    public bool CheckDashPath(Vector3 dashCheckVector)
    {
        float dashCheckAngle = Vector3.SignedAngle(transform.forward, dashCheckVector.normalized, transform.up);

        dashCheckOffsetVector = Quaternion.AngleAxis(dashCheckAngle, Vector3.up) * dashCheckOffsetVector;

        return Physics.OverlapBox(transform.TransformPoint(dashCheckOffsetVector), dashCheckBoxSize, Quaternion.LookRotation(dashCheckVector.normalized), dashCollisionLayers).Length <= 0;
    }

    public void MurkyWaterAbility(int layers)
    {
        Vector3 testVec = Vector3.forward;
        for (int i = 0; i < layers; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                testVec = Quaternion.AngleAxis(46f, Vector3.up) * testVec;
                print(testVec);
                SpawnMurkyWater(testVec * (i + 1));
            }
        }
    }

    //hur fan ska det funka med olika Y nivå? får typ raycasta upp och ner när den spawnas för att hitta vart marken är och sen flytta den dit och rotera den baserat på normalen eller något, låter jobbigt :(
    public void SpawnMurkyWater(Vector3 spawnPositionOffset, float timeToLive = 0f)
    {
        GameObject murkyWater = (GameObject)Instantiate(murkyWaterPrefab, transform.TransformPoint(spawnPositionOffset), Quaternion.identity);
        if (timeToLive > 0.01f)
        {
            murkyWater.GetComponentInChildren<MurkyWaterScript>().timeToLive = timeToLive;

            //print("spawned murky water for " + timeToLive);
        }
        else
        {
            //print("spawned murky water for ever >:) " + timeToLive);
        }
    }

    //hur fan ska det funka med olika Y nivå? får typ raycasta upp och ner när den spawnas för att hitta vart marken är och sen flytta den dit och rotera den baserat på normalen eller något, låter jobbigt :(
    public void SpawnMurkyWater(float timeToLive = 0f)
    {
        GameObject murkyWater = (GameObject)Instantiate(murkyWaterPrefab, transform.position, Quaternion.identity);
        if (timeToLive > 0.1f)
        {
            murkyWater.GetComponentInChildren<MurkyWaterScript>().timeToLive = timeToLive;

            //print("spawned murky water for " + timeToLive);
        }
        else
        {
            //print("spawned murky water for ever >:) " + timeToLive);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        Gizmos.color = Color.blue;

        Vector3 dashCheckOffsetVectorGizmo = new Vector3(0f, 1f, testDashDistance / 2);
        Vector3 dashCheckBoxSizeGizmo = new Vector3(1.5f, 1.5f, testDashDistance);

        float dashCheckAngleGizmo = Vector3.SignedAngle(transform.forward, movementDirection, transform.up);

        dashCheckOffsetVectorGizmo = Quaternion.AngleAxis(dashCheckAngleGizmo, Vector3.up) * dashCheckOffsetVectorGizmo;

        Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(dashCheckOffsetVectorGizmo), Quaternion.LookRotation(movementDirection.normalized), transform.localScale);
        Gizmos.DrawWireCube(Vector3.zero, dashCheckBoxSizeGizmo);
    }

    //Animation attack events
    #region Animation attack events
    
    //Generella
    public void EndAnimation()
    {
        animationEnded = true;
    }

    public void DontFacePlayer()
    {
        facePlayerBool = false;
        //print("rotation stopped");
    }
    //P1
    public void FlipDrainHitboxActivation()
    {
        drainHitboxActive = !drainHitboxActive;
    }
    public void FlipMeleeHitboxActivation()
    {
        meleeHitboxActive = !meleeHitboxActive;
    }
    #endregion
}

public class PreBossFightState : State<BossAIScript>
{
    private RaycastHit _hit;

    public override void EnterState(BossAIScript owner)
    {
        owner.bossAnimator.SetBool("idleBool", true);
    }

    public override void ExitState(BossAIScript owner)
    {
        //kanske köra någon funktion som stänger en dörr eller något så man inte kan springa iväg
        owner.bossAnimator.SetBool("idleBool", false);
    }

    public override void UpdateState(BossAIScript owner)
    {
        //aggro detection, börjar boss fighten
        if (owner.aggroRange > Vector3.Distance(owner.player.transform.position, owner.transform.position))
        {
            if (Physics.Raycast(owner.transform.position + new Vector3(0, 1, 0), (owner.player.transform.position - owner.transform.position).normalized, out _hit, owner.aggroRange, owner.targetLayers))
            {
                if (_hit.transform == owner.player.transform)
                {
                    owner.phaseControllingStateMachine.ChangeState(owner.bossPhaseOneState);
                }
            }
        }
    }
}

//////////////////
//PHASE 1 STATES//
//////////////////

//drain (charge, shoot, stay)
//idle/movement/bestämma nästa attack (gå runt lite, vara vänd mot spelaren, bestämma vilket state man ska in i sen, alla states går in i detta state)
//slå attack (om nära -> slå, typ?)
//dash (dasha, fast vart?) (dash attack, random dash(kan bli dåligt), dash om nära, dash om wiff)

//vill nog lägga till något transition state där någon aggro/bossfighten börjar nu annimation spelas


#region Phase 1 States
public class BossPhaseOneState : State<BossAIScript>
{

    public BossAIScript parentScript;

    public StateMachine<BossPhaseOneState> phaseOneStateMashine;


    public Phase1Attack1State phase1Attack1State;

    public PhaseOneCombatState phaseOneCombatState;
    public PhaseOneChargeDrainAttackState phaseOneChargeDrainAttackState;
    public PhaseOneActiveDrainAttackState phaseOneActiveDrainAttackState;
    public PhaseOneMeleeAttackOneState phaseOneMeleeAttackOneState;
    public PhaseOneDashState phaseOneDashState;
    public PhaseOneChaseToAttackState phaseOneChaseToAttackState;
    public override void EnterState(BossAIScript owner)
    {
        //fixa detta i konstruktor kanske?
        phaseOneStateMashine = new StateMachine<BossPhaseOneState>(this);

        phase1Attack1State = new Phase1Attack1State(owner.testDrainDPS, owner.testDrainRange, owner.testDrainChargeTime);

        phaseOneCombatState = new PhaseOneCombatState(owner.testAttackSpeed, owner.testMinAttackCooldown, owner.testMeleeRange, owner.testDrainRange, owner);
        phaseOneDashState = new PhaseOneDashState(owner.testDashSpeed, owner.testDashDistance, owner.testDashLagDurration, owner.testDashAcceleration);
        phaseOneChaseToAttackState = new PhaseOneChaseToAttackState();


        phaseOneChargeDrainAttackState = new PhaseOneChargeDrainAttackState(owner.testDrainChargeTime);
        phaseOneActiveDrainAttackState = new PhaseOneActiveDrainAttackState(owner.testDrainDPS, owner.testDrainRange, owner.testDrainAttackTime);
        phaseOneMeleeAttackOneState = new PhaseOneMeleeAttackOneState(owner.testDrainDPS, owner.testMeleeRange, owner.testDrainAttackTime, owner.testDrainChargeTime);

        parentScript = owner;

        phaseOneStateMashine.ChangeState(phaseOneCombatState);

        //spela cool animation :)
    }

    public override void ExitState(BossAIScript owner)
    {

    }

    public override void UpdateState(BossAIScript owner)
    {
        //kolla om man ska gå över till nästa phase
        if ((owner.GetComponent<EnemyHealth>().GetHealth() / owner.GetComponent<EnemyHealth>().GetMaxHealth()) < owner.testP2TransitionHP)
        {
            owner.phaseControllingStateMachine.ChangeState(owner.bossPhaseTwoState);
        }

        phaseOneStateMashine.Update();
    }
}

//vet inte om allt detta typ egentligen borde göras i parent statet (borde typ det tror jag)
public class PhaseOneCombatState : State<BossPhaseOneState>
{
    private Timer timer;
    private float _attackSpeed;
    private float _baseMinAttackCooldown;
    private float _minAttackCooldown;
    private float _meleeAttackRange;
    private float _drainAttackRange;

    private Vector3 _destination;
    //private Vector3 _direction;
    private Vector3 _dashAttackDirection;
    private float _dashAttackAngle;

    private Vector3 _bossToPlayer;

    private RaycastHit _hit;

    private BossAIScript _ownerParentScript;

    public PhaseOneCombatState(float attackSpeed, float minAttackCooldown, float meleeAttackRange, float drainAttackRange, BossAIScript ownerParentScript)
    {
        _attackSpeed = attackSpeed;
        _baseMinAttackCooldown = minAttackCooldown;
        _meleeAttackRange = meleeAttackRange;
        _drainAttackRange = drainAttackRange;

        _ownerParentScript = ownerParentScript;

        timer = new Timer(_attackSpeed);
    }

    public override void EnterState(BossPhaseOneState owner)
    {
        //Debug.Log("in i PhaseOneCombatState");
        if (timer.Expired)
        {
            timer = new Timer(_attackSpeed);
            _minAttackCooldown = _baseMinAttackCooldown;
        }
        else
        {
            _minAttackCooldown += timer.Time;
        }
        //_ownerParentScript.MurkyWaterAbility(10);
    }

    public override void ExitState(BossPhaseOneState owner)
    {
        //Debug.Log("hej då PhaseOneCombatState");
        _ownerParentScript.agent.SetDestination(_ownerParentScript.transform.position);
    }

    public override void UpdateState(BossPhaseOneState owner)
    {
        _ownerParentScript.FacePlayer();

        timer.Time += Time.deltaTime;

        //kanske borde dela upp detta i olika movement states pga animationer men vet inte om det behövs

        //kolla om man ska attackera
        if (timer.Expired)
        {
            //nära nog för att göra melee attacken
            if (_drainAttackRange > Vector3.Distance(_ownerParentScript.transform.position, _ownerParentScript.player.transform.position))
            {
                //vill den dash attacka?
                if (UnityEngine.Random.Range(0f, 100f) > 100f - _ownerParentScript.testDashAttackChanse)
                {
                    _bossToPlayer = _ownerParentScript.player.transform.position - _ownerParentScript.transform.position;

                    Physics.Raycast(_ownerParentScript.transform.position + new Vector3(0, 1, 0), _bossToPlayer.normalized, out _hit, _ownerParentScript.testDashDistance + _ownerParentScript.testMeleeRange, _ownerParentScript.targetLayers);

                    //är spelaren innom en bra range och innom LOS?
                    if (_hit.transform == _ownerParentScript.player.transform && _bossToPlayer.magnitude < _ownerParentScript.testDashDistance + _ownerParentScript.testMeleeRange / 2 && _bossToPlayer.magnitude > _ownerParentScript.testDashDistance - _ownerParentScript.testMeleeRange / 2)
                    {
                        //Debug.Log("_bossToPlayer " + _bossToPlayer.magnitude);

                        int dashSign = 0;

                        if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
                        {
                            dashSign = 1;
                        }
                        else
                        {
                            dashSign = -1;
                        }

                        _dashAttackAngle = Mathf.Rad2Deg * Mathf.Acos((Mathf.Pow(_bossToPlayer.magnitude, 2) + Mathf.Pow(_ownerParentScript.testDashDistance, 2) - Mathf.Pow(_ownerParentScript.testMeleeRange / 2, 2)) / (2 * _bossToPlayer.magnitude * _ownerParentScript.testDashDistance));

                        _dashAttackDirection = _bossToPlayer;

                        _dashAttackDirection = Quaternion.AngleAxis(_dashAttackAngle * dashSign, Vector3.up) * _dashAttackDirection;

                        Vector3 _playerToDashPos = (_ownerParentScript.transform.position + _dashAttackDirection.normalized * _ownerParentScript.testDashDistance) - _ownerParentScript.player.transform.position;

                        Vector3 _playerToBoss = _ownerParentScript.transform.position - _ownerParentScript.player.transform.position;

                        float _angleDashAttackToPlayer = Vector3.Angle(_playerToBoss, _playerToDashPos);

                        if (_angleDashAttackToPlayer < _ownerParentScript.maxAngleDashAttackToPlayer)
                        {
                            //ändra så det inte är en siffra utan att det beror på deras hittboxes storlek eller en parameter

                            //ranomizar vart bossen kommer dasha, sålänge den inte skulle kunna krocka med spelaren
                            if (_bossToPlayer.magnitude - 0.45f > _ownerParentScript.testDashDistance)
                            {
                                _dashAttackAngle = UnityEngine.Random.Range(0, _dashAttackAngle);
                                _dashAttackDirection = _bossToPlayer;
                                _dashAttackDirection = Quaternion.AngleAxis(_dashAttackAngle * dashSign * -1, Vector3.up) * _dashAttackDirection;
                            }

                            //är det något i vägen för dashen?
                            if (_ownerParentScript.CheckDashPath(_dashAttackDirection))
                            {
                                _ownerParentScript.movementDirection = _dashAttackDirection;
                                _ownerParentScript.dashAttack = true;
                                owner.phaseOneStateMashine.ChangeState(owner.phaseOneDashState);
                            }
                            else
                            {
                                _dashAttackDirection = _bossToPlayer;
                                //"*-1" för att få andra sidan av spelaren
                                _dashAttackDirection = Quaternion.AngleAxis(_dashAttackAngle * dashSign * -1, Vector3.up) * _dashAttackDirection;
                                //är något i vägen om den dashar till andra sidan av spelaren?
                                if (_ownerParentScript.CheckDashPath(_dashAttackDirection))
                                {
                                    _ownerParentScript.movementDirection = _dashAttackDirection;
                                    _ownerParentScript.dashAttack = true;
                                    owner.phaseOneStateMashine.ChangeState(owner.phaseOneDashState);
                                }
                                //saker va i vägen för dashen
                                else
                                {
                                    Debug.Log("kan inte dasha med all denna skit ivägen juuuuuöööööö");

                                    owner.phaseOneStateMashine.ChangeState(owner.phaseOneChaseToAttackState);
                                }
                            }
                        }
                        else
                        {
                            Debug.Log("no want dash tru player");
                            owner.phaseOneStateMashine.ChangeState(owner.phaseOneChaseToAttackState);
                        }
                    }
                    else
                    {
                        Debug.Log("ITS TO FAR AWAY!!!! (or to close)");
                        owner.phaseOneStateMashine.ChangeState(owner.phaseOneChaseToAttackState);
                    }
                }
                //springa och slå
                else
                {
                    Debug.Log("no want dash tyvm");
                    owner.phaseOneStateMashine.ChangeState(owner.phaseOneChaseToAttackState);
                }
            }
            //drain
            else
            {
                owner.phaseOneStateMashine.ChangeState(owner.phaseOneChargeDrainAttackState);
            }
        }
        //kolla om spelaren är nära nog att slå
        else if (timer.Time > _minAttackCooldown && _meleeAttackRange > Vector3.Distance(_ownerParentScript.transform.position, _ownerParentScript.player.transform.position))
        {
            //kanske göra AOE attack här för att tvinga iväg spelaren?
            owner.phaseOneStateMashine.ChangeState(owner.phaseOneMeleeAttackOneState);
        }
        //idle movement
        else
        {
            //flytta till fixed uppdate (kanske)
            Physics.Raycast(_ownerParentScript.transform.position + new Vector3(0, 1, 0), (_ownerParentScript.player.transform.position - _ownerParentScript.transform.position).normalized, out _hit, Mathf.Infinity, _ownerParentScript.targetLayers);

            //om bossen kan se spelaren
            if (_hit.transform == _ownerParentScript.player.transform)
            {
                //gör så att den byter mellan att gå höger och vänster
                int strafeSign = 0;

                if (timer.Ratio > 0.5f)
                {
                    strafeSign = -1;
                }
                else
                {
                    strafeSign = 1;
                }

                _ownerParentScript.movementDirection = _ownerParentScript.transform.right;

                //lägga till någon randomness variabel så movement inte blir lika predictable? (kan fucka animationerna?)
                //kanske slurpa mellan de olika värdena (kan bli jobbigt och vet inte om det behövs)
                float compairValue = Vector3.Distance(_ownerParentScript.transform.position, _ownerParentScript.player.transform.position);

                for (int i = 0; i < _ownerParentScript.desiredDistanceToAngleValues.Length; i++)
                {
                    if (compairValue > _ownerParentScript.desiredDistanceToPlayer + _ownerParentScript.desiredDistanceOffsetValues[i])
                    {
                        _ownerParentScript.movementDirection = Quaternion.AngleAxis(_ownerParentScript.desiredDistanceAngleValues[i] * strafeSign * -1, Vector3.up) * _ownerParentScript.movementDirection;
                        _ownerParentScript.movementDirection *= strafeSign;
                        break;
                    }
                }

                //random dash in combat
                if (UnityEngine.Random.Range(0f, 100f) > 100f - _ownerParentScript.testDashChansePerFrame)
                {
                    if (_ownerParentScript.CheckDashPath(_ownerParentScript.movementDirection))
                    {
                        owner.phaseOneStateMashine.ChangeState(owner.phaseOneDashState);
                    }
                    else
                    {
                        Debug.Log("kunde inte dasha för saker va i vägen");
                    }
                }
                else
                {
                    //ändra 5an till typ destinationAmplifier
                    _destination = _ownerParentScript.transform.position + _ownerParentScript.movementDirection * 5;
                    _ownerParentScript.agent.SetDestination(_destination);
                }
            }
            //om bossen inte kan se spelaren
            else
            {
                _destination = _ownerParentScript.player.transform.position;
                _ownerParentScript.agent.SetDestination(_destination);
            }
        }
    }
}

public class PhaseOneDashState : State<BossPhaseOneState>
{
    private float _dashSpeed;
    private float _oldSpeed;
    private float _dashDistance;
    private float _dashDurration;
    private float _lagDurration;
    private float _dashAcceleration;
    private float _oldAcceleration;

    private Timer _dashTimer;
    private Timer _lagTimer;

    private Vector3 _dashDirection;
    private Vector3 _dashDestination;


    public PhaseOneDashState(float speed, float distance, float lagDurration, float acceleration)
    {
        _dashSpeed = speed;
        _dashDistance = distance;
        _lagDurration = lagDurration;
        _dashAcceleration = acceleration;
    }


    public override void EnterState(BossPhaseOneState owner)
    {
        _oldSpeed = owner.parentScript.agent.speed;
        _oldAcceleration = owner.parentScript.agent.acceleration;

        _dashDurration = (_dashDistance - owner.parentScript.agent.stoppingDistance) / _dashSpeed;
        //Debug.Log("zoom for, " + _dashDurration + " MPH, " + _dashSpeed);
        //Debug.Log(_dashDistance + " " + owner.bossPhaseOneParentScript.agent.stoppingDistance + " " + _dashSpeed);
        _dashTimer = new Timer(_dashDurration);
        _lagTimer = new Timer(_lagDurration);


        owner.parentScript.agent.speed = _dashSpeed;
        owner.parentScript.agent.acceleration = _dashAcceleration;

        _dashDirection = owner.parentScript.movementDirection.normalized;
        _dashDestination = owner.parentScript.transform.position + _dashDirection * _dashDistance;

        owner.parentScript.agent.SetDestination(_dashDestination);
    }

    public override void ExitState(BossPhaseOneState owner)
    {
        owner.parentScript.agent.speed = _oldSpeed;
        owner.parentScript.agent.acceleration = _oldAcceleration;
        owner.parentScript.agent.SetDestination(owner.parentScript.transform.position);
        //Debug.Log("hej då zoom");
    }

    public override void UpdateState(BossPhaseOneState owner)
    {
        _dashTimer.Time += Time.deltaTime;

        if (_dashTimer.Expired)
        {
            _lagTimer.Time += Time.deltaTime;

            if (owner.parentScript.dashAttack)
            {
                owner.parentScript.dashAttack = false;
                owner.phaseOneStateMashine.ChangeState(owner.phaseOneMeleeAttackOneState);
            }
            else if (_lagTimer.Expired)
            {
                owner.phaseOneStateMashine.ChangeState(owner.phaseOneCombatState);
            }
        }
    }
}

public class PhaseOneMeleeAttackOneState : State<BossPhaseOneState>
{
    private float _damage;
    private float _range;
    private float _totalDurration;
    private float _chargeTime;


    public PhaseOneMeleeAttackOneState(float damage, float range, float attackTime, float chargeTime)
    {
        _damage = damage;
        _range = range;
        _chargeTime = chargeTime;
        _totalDurration = chargeTime + attackTime;
    }


    public override void EnterState(BossPhaseOneState owner)
    {
        owner.parentScript.bossAnimator.SetTrigger("melee1Trigger");
        owner.parentScript.meleeAttackHitboxGroup.enabled = true;
        //Debug.Log("nu ska jag fan göra PhaseOneMeleeAttackState >:(, med dessa stats:  damage " + _damage + " range " + _range + " totalDurration " + _totalDurration + " chargeTime " + _chargeTime);
    }

    public override void ExitState(BossPhaseOneState owner)
    {
        //Debug.Log("hej då PhaseOneMeleeAttackState");
        owner.parentScript.meleeAttackHitboxGroup.enabled = false;
        owner.parentScript.animationEnded = false;
        owner.parentScript.facePlayerBool = true;
    }

    public override void UpdateState(BossPhaseOneState owner)
    {
        if (owner.parentScript.animationEnded)
        {
            owner.phaseOneStateMashine.ChangeState(owner.phaseOneCombatState);
        }
        else
        {
            if (owner.parentScript.facePlayerBool)
            {
                //spela attackljud här
                owner.parentScript.FacePlayer();
            }
        }
    }
}

public class PhaseOneChaseToAttackState : State<BossPhaseOneState>
{
    private Vector3 _playerPos;
    private float _distanceToPlayer;
    private float _oldSpeed;
    private float _oldAcceleration;


    public override void EnterState(BossPhaseOneState owner)
    {
        _oldSpeed = owner.parentScript.agent.speed;
        _oldAcceleration = owner.parentScript.agent.acceleration;

        owner.parentScript.agent.speed = owner.parentScript.chasingSpeed;
        owner.parentScript.agent.acceleration = owner.parentScript.chasingAcceleration;
        owner.parentScript.bossAnimator.SetTrigger("runningTrigger");
    }

    //ändra speed och acceleration i detta state

    public override void ExitState(BossPhaseOneState owner)
    {
        owner.parentScript.agent.speed = _oldSpeed;
        owner.parentScript.agent.acceleration = _oldAcceleration;

        owner.parentScript.agent.SetDestination(owner.parentScript.transform.position);
    }

    public override void UpdateState(BossPhaseOneState owner)
    {
        owner.parentScript.FacePlayer();

        _playerPos = owner.parentScript.player.transform.position;
        _distanceToPlayer = Vector3.Distance(owner.parentScript.transform.position, _playerPos);

        if (_distanceToPlayer > owner.parentScript.testDrainRange)
        {
            owner.phaseOneStateMashine.ChangeState(owner.phaseOneChargeDrainAttackState);
        }
        else if (_distanceToPlayer < owner.parentScript.testMeleeRange)
        {
            owner.phaseOneStateMashine.ChangeState(owner.phaseOneMeleeAttackOneState);
        }
        else
        {
            owner.parentScript.agent.SetDestination(_playerPos);
        }
    }
}

public class PhaseOneChargeDrainAttackState : State<BossPhaseOneState>
{
    private float _chargeTime;
    private Timer _timer;

    public PhaseOneChargeDrainAttackState(float chargeTime)
    {
        _chargeTime = chargeTime;
    }

    public override void EnterState(BossPhaseOneState owner)
    {
        owner.parentScript.bossAnimator.SetTrigger("drainStartTrigger");
        //Debug.Log("nu ska jag fan göra PhaseOneChargeDrainAttackState >:(, med dessa stats: chargeTime " + _chargeTime);
        _timer = new Timer(_chargeTime);
    }

    public override void ExitState(BossPhaseOneState owner)
    {
        //Debug.Log("hej då PhaseOneChargeDrainAttackState");
    }

    public override void UpdateState(BossPhaseOneState owner)
    {
        _timer.Time += Time.deltaTime;

        if (_timer.Expired)
        {
            owner.phaseOneStateMashine.ChangeState(owner.phaseOneActiveDrainAttackState);
        }
        else if (owner.parentScript.facePlayerBool)
        {
            owner.parentScript.FacePlayer();
        }
    }
}

//dela upp detta state så man kan hålla animationen
public class PhaseOneActiveDrainAttackState : State<BossPhaseOneState>
{
    private float _damagePerSecond;
    private float _range;
    private float _durration;
    private Timer _timer;

    public PhaseOneActiveDrainAttackState(float damagePerSecond, float range, float durration)
    {
        _damagePerSecond = damagePerSecond;
        _range = range;
        _durration = durration;
    }

    public override void EnterState(BossPhaseOneState owner)
    {
        //ändra så den sätts i drain loopen
        owner.parentScript.placeholoderDranBeam.SetActive(true);

        owner.parentScript.bossAnimator.SetBool("drainActiveBool", true);
        owner.parentScript.drainAttackHitboxGroup.enabled = true;
        owner.parentScript.turnSpeed = owner.parentScript.drainActiveTurnSpeed;
        //Debug.Log("nu ska jag fan göra PhaseOneActiveDrainAttackState >:(, med dessa stats:  damage " + _damagePerSecond + " range " + _range + " durration " + _durration);
        _timer = new Timer(_durration);
    }

    public override void ExitState(BossPhaseOneState owner)
    {
        //ändra så den sätts i drain loopen
        owner.parentScript.placeholoderDranBeam.SetActive(false);

        //Debug.Log("hej då PhaseOneActiveDrainAttackState");
        owner.parentScript.turnSpeed = owner.parentScript.defaultTurnSpeed;
        owner.parentScript.drainAttackHitboxGroup.enabled = false;
        owner.parentScript.animationEnded = false;
        owner.parentScript.facePlayerBool = true;
    }

    public override void UpdateState(BossPhaseOneState owner)
    {
        _timer.Time += Time.deltaTime;

        if (owner.parentScript.facePlayerBool)
        {
            owner.parentScript.FacePlayer();
        }
        if (_timer.Expired)
        {
            owner.parentScript.bossAnimator.SetBool("drainActiveBool", false);
            owner.phaseOneStateMashine.ChangeState(owner.phaseOneCombatState);
        }
    }
}

public class Phase1Attack1State : State<BossPhaseOneState>
{
    private float _damage;
    private float _range;
    private float _durration;
    private Timer _timer;


    public Phase1Attack1State(float damage, float range, float durration)
    {
        _damage = damage;
        _range = range;
        _durration = durration;
    }


    public override void EnterState(BossPhaseOneState owner)
    {
        Debug.Log("nu ska jag fan göra Phase1Attack1 >:(, med dessa stats:  damage " + _damage + " range " + _range + " durration " + _durration);
        _timer = new Timer(_durration);
    }

    public override void ExitState(BossPhaseOneState owner)
    {
        Debug.Log("hej då Phase1Attack1");
    }

    public override void UpdateState(BossPhaseOneState owner)
    {
        //kör cool animation som bestämmer när attacken är över istället för durration

        _timer.Time += Time.deltaTime;

        if (_timer.Expired)
        {
            owner.phaseOneStateMashine.ChangeState(owner.phaseOneCombatState);
        }
    }
}

#endregion

//////////////////
//PHASE 2 STATES//
//////////////////

#region Phase 2 States
public class BossPhaseTwoState : State<BossAIScript>
{

    public override void EnterState(BossAIScript owner)
    {

    }

    public override void ExitState(BossAIScript owner)
    {

    }

    public override void UpdateState(BossAIScript owner)
    {
        Debug.Log("nu chillar vi i Phase 2 :)");
    }
}
#endregion
