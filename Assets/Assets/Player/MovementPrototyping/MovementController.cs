﻿using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class MovementController : MonoBehaviour {
    #region Parameters
    /* === DEBUG === */
    [Header("Debug")]
    [SerializeField] private bool lockCursor = true;
    [SerializeField] private Transform lockOnTarget;

    /* === STATS === */
    [Header("Character properties")]
    [SerializeField] private float _gravity = -9.82f;
    [SerializeField] private float _jumpHeight = 3f;

    public float maxSpeed = 12f;
    public float acceleration = 8f;
    public float deceleration = 2f;
    public float rotationSpeed;
    public float rotationAngleUntilMove = 30;

    [SerializeField] private float _dashCooldownTime = 0.25f;
    public float dashTime = 0.25f;
    public float dashLag = 0.15f;
    public float dashSpeed = 10.0f;

    /* === HIDDEN REFERENCES === */
    [HideInInspector] public Camera mainCamera;
    [HideInInspector] public CharacterController controller;
    private Animator _cameraAnimator;

    /* === PUBLIC REFERENCES === */
    [Header("References")]
    public Animator playerAnimator;
    [SerializeField] private Transform _groundCheckPosition;
    [SerializeField] private Cinemachine.CinemachineFreeLook _freeLookCam;
    [SerializeField] private Cinemachine.CinemachineFreeLook _lockonCam;
    public LayerMask groundMask;
    public LayerMask enemyMask;

    /* === INFORMATION === */
    [Header("Information")]
    [SerializeField] private float _groundDistance = 0.4f;
    #endregion

    #region Variables
    /* === SCRIPT EXCLUSIVES === */
    [Header("Placeholder lock on")]
    [SerializeField] private Vector3 _lockOnOffset;
    [SerializeField] private float _lockOnRadius;
    [SerializeField] private float _lockOnMaxDistance;
    private Vector3 _lockOnOrigin;
    private Vector3 _lockOnDirection;
    private float _lockOnCurrentHitDistance;
    private RaycastHit _lockOnCastHit;


    private Timer _dashCooldownTimer;
    private bool _doSnapCamera;
    private bool _hasJumped;
    private bool _isGrounded;
    private Vector3 _velocity;
    private PlayerInput _playerInput;
    [HideInInspector] public Vector2 input;
    [HideInInspector] public Transform pointOfInterest;
    [HideInInspector] public StateMachine<MovementController> stateMachine;

    private Vector3 _maxSpeedVec;
    public Vector3 maxSpeedVec {
        get {
            if (_maxSpeedVec == null || _maxSpeedVec == Vector3.zero)
                _maxSpeedVec = Vector3.Normalize(new Vector3(1, 0, 1)) * maxSpeed;
            return _maxSpeedVec;
        }
    }

    private bool _lockedOn;
    [HideInInspector] public bool isLockedOn { get { return _lockedOn; } }
    #endregion

    public void ToggleLockon() {
        if (!pointOfInterest) {
            Debug.LogWarning("Trying to toggle lockon without a point of interest!", this);
        }
        else {
            _lockedOn = !_lockedOn;
            _cameraAnimator.SetBool("lockedOn", _lockedOn);
            if (isLockedOn) {
                _lockonCam.LookAt = pointOfInterest;

                // BUG: overrides current state, resulting in deleted end lag
                stateMachine.ChangeState(new StrafeMovementState());
            }
            else {
                _doSnapCamera = true;
            }
        }
    }

    private void Awake() {
        // Debug
        if (lockCursor)
            Cursor.lockState = CursorLockMode.Locked;

        // Reference handling
        _cameraAnimator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();

        // Declarations
        _playerInput = new PlayerInput();
        stateMachine = new StateMachine<MovementController>(this);
        _dashCooldownTimer = new Timer(_dashCooldownTime);
        _dashCooldownTimer.Time = _dashCooldownTime;
        stateMachine.ChangeState(new IdleMovementState());

        // Input
        _playerInput.PlayerControls.Move.performed += ctx => input = ctx.ReadValue<Vector2>();
        //_playerInput.PlayerControls.Test.performed += _ => ToggleLockon();
        _playerInput.PlayerControls.Jump.performed += ctx => _hasJumped = true;
        _playerInput.PlayerControls.Dash.performed += Dash;
    }
    
    void Update() {
        GroundCheck();

        stateMachine.Update();

        Jump();

        SnapCamera();

        _dashCooldownTimer.Time += Time.deltaTime;

        _hasJumped = false;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Debug.DrawLine(_lockOnOrigin, _lockOnOrigin + _lockOnDirection * _lockOnCurrentHitDistance);
        Gizmos.DrawWireSphere(_lockOnOrigin + _lockOnDirection * _lockOnCurrentHitDistance, _lockOnRadius);
    }

    private void OnEnable() { _playerInput.Enable(); }

    private void OnDisable() { _playerInput.Disable(); }

    [SerializeField] private bool toggleLockon;
    private void OnValidate() {
        pointOfInterest = lockOnTarget;
        if (toggleLockon) {
            toggleLockon = false;
            ToggleLockon();
        }
    }
    /// <summary>
    /// Snaps camera after returning to free look camera
    /// </summary>
    void SnapCamera() {
        if (_doSnapCamera) {
            //var free = new Vector2(_freeLookCam.m_XAxis.Value, _freeLookCam.m_YAxis.Value);
            //var loc = new Vector2(_lockonCam.m_XAxis.Value, _lockonCam.m_YAxis.Value);
            _freeLookCam.m_XAxis.Value = transform.eulerAngles.y;
            _doSnapCamera = false;
        }
    }

    void GroundCheck() {
        _isGrounded = Physics.CheckSphere(_groundCheckPosition.position, _groundDistance, groundMask);
        if (_isGrounded && _velocity.y < 0) {
            _velocity.y = -2f;
        }
        playerAnimator.SetBool("Grounded", _isGrounded);
    }

    void Jump() {
        if (_hasJumped && _isGrounded) {
            _velocity.y = Mathf.Sqrt(_jumpHeight) * -_gravity;
            playerAnimator.SetTrigger("Jump");
        }

        _velocity.y += _gravity * Time.deltaTime; //Gravity formula
        controller.Move(_velocity * Time.deltaTime); // T^2
    }

    /* === PLACEHOLDERS === */
    private void Dash(InputAction.CallbackContext c) { // Placeholder
        if (_dashCooldownTimer.Expired()) {
            _dashCooldownTimer.Reset();
            stateMachine.ChangeState(new DashMovementState());
        }
    }
}

public class IdleMovementState : State<MovementController> {
    private float _currentBlend;
    private float _timer;
    private float Timer {
        get { return _timer; }
        set {
            if (value >= 1)
                _timer = 1;
            else
                _timer = value;
        }
    }
    public override void EnterState(MovementController owner) {
        _currentBlend = owner.playerAnimator.GetFloat("Blend");
        owner.playerAnimator.SetFloat("Blend", 0.0f);
    }
    public override void ExitState(MovementController owner) { }
    
    public override void UpdateState(MovementController owner) {
        if (owner.input != Vector2.zero)
            owner.stateMachine.ChangeState(new GeneralMovementState());
        //Timer += Time.deltaTime;
        //owner.playerAnimator.SetFloat("Blend", Mathf.Lerp(_currentBlend, 0.0f, Timer));
    }
}

public class GeneralMovementState : State<MovementController> {
    private bool _isMoving;

    public override void EnterState(MovementController owner) { }
    public override void ExitState(MovementController owner) {
        _isMoving = false;
    }

    public float movingThreshold = 0.09f;
    //private float completeTurnAroundAngleThreshold = 120;
    public override void UpdateState(MovementController owner) {
        if (owner.input == Vector2.zero) { // Changes state to idle if player is not moving
            owner.stateMachine.ChangeState(new IdleMovementState());
        }
        else {
            if (owner.input.magnitude >= movingThreshold) {
                Vector3 baseInputDirection = Camera.main.transform.right * owner.input.normalized.x + Camera.main.transform.forward * owner.input.normalized.y;
                Vector3 resultingDirection = Vector3.RotateTowards(owner.transform.forward, baseInputDirection, owner.rotationSpeed * Time.deltaTime, 0.0f);

                // The angle between baseInputDirection and resultingDirection
                float angle = Vector2.Angle(new Vector2(owner.transform.forward.x, owner.transform.forward.z),
                                            new Vector2(baseInputDirection.x, baseInputDirection.z));

                owner.transform.rotation = Quaternion.LookRotation(resultingDirection);
                owner.transform.eulerAngles = new Vector3(0, owner.transform.eulerAngles.y, 0); // Limits rotation to the Y-axis
                Vector3 move = owner.transform.forward * owner.input.magnitude;                 // Constant forward facing force
                owner.playerAnimator.SetFloat("Blend", owner.input.magnitude);
                if (angle < owner.rotationAngleUntilMove) {
                    _isMoving = true;
                }

                if (_isMoving) {
                    owner.controller.Move(move * owner.maxSpeed * Time.deltaTime);
                }
            }
        }
    }
}

public class StrafeMovementState : State<MovementController> {
    public override void EnterState(MovementController owner) { }
    public override void ExitState(MovementController owner) { }

    public override void UpdateState(MovementController owner) {
        if (owner.isLockedOn) {
            //PointOfInterestIsMousePos(owner);

            owner.transform.LookAt(owner.pointOfInterest);
            owner.transform.eulerAngles = new Vector3(0, owner.transform.eulerAngles.y, 0);

            float x = owner.input.x;
            float z = owner.input.y;

            Vector3 move = owner.transform.forward * z;
            move += owner.transform.right * x;

            owner.controller.Move(move * owner.maxSpeed * Time.deltaTime);
        }
        else {
            owner.stateMachine.ChangeState(new GeneralMovementState());
        }
    }

    // Placeholder meant for debug
    void PointOfInterestIsMousePos(MovementController owner) {
        Vector2 mousePos = Input.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, owner.groundMask)) {
            //owner.pointOfInterest = hit.transform;
            owner.transform.LookAt(hit.point);
            owner.transform.eulerAngles = new Vector3(0, owner.transform.eulerAngles.y, 0);
        }
    }
}

public class DashMovementState : State<MovementController> {
    private Timer _timer;
    private Timer _lagTimer;
    private Vector3 _dashDirection;

    public override void ExitState(MovementController owner) {
        owner.playerAnimator.SetBool("Dash", false);
    }

    public override void EnterState(MovementController owner) {
        _timer = new Timer(owner.dashTime);
        _lagTimer = new Timer(owner.dashLag);

        owner.playerAnimator.SetBool("Dash", true);

        _dashDirection += Camera.main.transform.right * owner.input.x;
        _dashDirection += Camera.main.transform.forward * owner.input.y;
        if (_dashDirection == Vector3.zero)
            _dashDirection = Camera.main.transform.forward;
        _dashDirection.y = 0;

        if (!owner.isLockedOn) {
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(_dashDirection.x, 0, _dashDirection.z));
            owner.transform.rotation = lookRotation;
        }
    }
    public override void UpdateState(MovementController owner) {
        if (_timer.Expired()) {
            _lagTimer.Time += Time.deltaTime;
            if (_lagTimer.Expired()) {
                if (owner.isLockedOn)
                    owner.stateMachine.ChangeState(new StrafeMovementState());
                else
                    owner.stateMachine.ChangeState(new IdleMovementState());
            }
        }
        else {
            _timer.Time += Time.deltaTime;
            owner.controller.Move(_dashDirection * owner.dashSpeed * Time.deltaTime);
        }
    }
}