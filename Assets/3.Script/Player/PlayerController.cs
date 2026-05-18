using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerController : MonoBehaviour {
    [Header("플레이어 이동")]
    public float MoveSpeed = 2.0f;
    public float SprintSpeed = 6.0f;
    public float SpeedChangeRate = 15.0f;       // [수정] 10 → 15, 가속/감속 더 빠릿하게
    public float RotationSmoothTime = 0.06f;    // [수정] 0.12 → 0.06, 방향 전환 즉각 반응

    [Header("점프 / 중력")]
    public float JumpHeight = 1.2f;
    public float JumpTimeout = 0.50f;
    public float Gravity = -15.0f;
    public float FallMultiplier = 2.5f;         // [추가] 낙하 가속 배율 (빠릿한 낙하)

    // [제거] FallTimeout — Coyote Time 없이 즉각 FreeFall 판정
    // public float FallTimeout = 0.15f;

    [Header("점프 버퍼")]
    public float JumpBufferTime = 0.15f;        // [추가] 착지 직전 점프 입력을 기억하는 시간

    [Header("지면 체크")]
    public bool Grounded = true;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;
    public LayerMask GroundLayers;

    [Header("시네머신 카메라")]
    [SerializeField] private Transform _cameraRoot;
    [SerializeField] private float _mouseSensitive = 1.0f;

    // 컴포넌트
    private InputController _input;
    private CharacterController _controller;
    private GameObject _mainCamera;
    private Animator _animator;

    // 카메라
    private float _targetYaw;
    private float _targetPitch;
    private float _threshold = 0.01f;

    // 이동
    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    // 타이머
    private float _jumpTimeoutDelta;
    private float _jumpBufferDelta;             // [추가] 점프 버퍼 타이머

    // 상태
    private bool _wasGrounded;                  // [추가] 이전 프레임 지면 여부 (Stop 판정용)
    private bool _isStopping;                   // [추가] Stop 애니메이션 재생 중 여부

    // 애니메이션 ID
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDStop;                    // [수정] 이제 실제로 사용

    private void Awake() {
        if (_mainCamera == null) {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
        TryGetComponent(out _input);
        TryGetComponent(out _controller);
        TryGetComponent(out _animator);
    }

    private void Start() {
        AssignAnimationIDs();
        _jumpTimeoutDelta = JumpTimeout;
        _jumpBufferDelta = 0f;
    }

    private void Update() {
        GroundedCheck();
        JumpAndGravity();
        Move();
    }

    private void LateUpdate() {
        CameraRotation();
    }

    private void AssignAnimationIDs() {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDStop = Animator.StringToHash("Stop");
    }

    private void GroundedCheck() {
        _wasGrounded = Grounded; // [추가] 이전 프레임 상태 저장

        Vector3 spherePosition = new Vector3(
            transform.position.x,
            transform.position.y - GroundedOffset,
            transform.position.z
        );
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

        if (_animator) {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
    }

    private void CameraRotation() {
        if (_input.look.sqrMagnitude >= _threshold) {
            _targetYaw += _input.look.x * _mouseSensitive;
            _targetPitch -= _input.look.y * _mouseSensitive;
        }

        if (_targetYaw < -360f) _targetYaw += 360f;
        if (_targetYaw > 360f) _targetYaw -= 360f;
        _targetPitch = Mathf.Clamp(_targetPitch, -30f, 70f);

        _cameraRoot.rotation = Quaternion.Euler(_targetPitch, _targetYaw, 0.0f);
    }

    private void Move() {
        float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
        bool isMoving = _input.move != Vector2.zero;

        if (!isMoving) targetSpeed = 0f;

        // --- Stop 판정 ---
        // [추가] 이동 중이다가 입력이 끊기면 Stop 트리거
        bool justStopped = !isMoving && _animationBlend > 0.1f && !_isStopping;
        if (justStopped) {
            _isStopping = true;
            if (_animator) {
                _animator.SetTrigger(_animIDStop);
            }
        }
        // 속도가 거의 0이 되면 Stop 상태 해제
        if (_isStopping && _animationBlend < 0.01f) {
            _isStopping = false;
        }

        // --- 속도 보간 ---
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;
        float speedOffset = 0.1f;

        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset) {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, Time.deltaTime * SpeedChangeRate);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        } else {
            _speed = targetSpeed;
        }

        // 애니메이션 블렌드 (Blend Tree용 Speed 파라미터)
        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        // --- 회전 ---
        Vector3 inputDirection = new Vector3(_input.move.x, 0f, _input.move.y).normalized;
        if (isMoving) {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg
                              + _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, _targetRotation,
                ref _rotationVelocity, RotationSmoothTime
            );
            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }

        // --- 실제 이동 ---
        Vector3 targetDirection = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;
        _controller.Move(
            targetDirection.normalized * (_speed * Time.deltaTime)
            + new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime
        );

        // 애니메이터 업데이트
        if (_animator) {
            _animator.SetFloat(_animIDSpeed, _animationBlend);
        }
    }

    private void JumpAndGravity() {
        // --- 점프 버퍼 갱신 ---
        // [추가] 점프 입력이 들어오면 버퍼 타이머 충전
        if (_input.jump) {
            _jumpBufferDelta = JumpBufferTime;
            _input.jump = false; // 입력은 버퍼에 위임하고 즉시 소비
        }
        if (_jumpBufferDelta > 0f) {
            _jumpBufferDelta -= Time.deltaTime;
        }

        if (Grounded) {
            // --- 착지 처리 ---
            if (_animator) {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            if (_verticalVelocity < 0f) {
                _verticalVelocity = -2f; // 지면에 살짝 눌러줘야 GroundedCheck가 안정적
            }

            // --- 점프 (버퍼 활용) ---
            // [수정] _input.jump 대신 버퍼 타이머로 판정
            if (_jumpBufferDelta > 0f && _jumpTimeoutDelta <= 0f) {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                _jumpBufferDelta = 0f; // 버퍼 소비

                if (_animator) {
                    _animator.SetBool(_animIDJump, true);
                    _animator.ResetTrigger(_animIDStop);
                }
            }

            if (_jumpTimeoutDelta >= 0f) {
                _jumpTimeoutDelta -= Time.deltaTime;
            }

        } else {
            // --- 공중 처리 ---
            _jumpTimeoutDelta = JumpTimeout;

            // [수정] FallTimeout 제거 → 공중이고 하강 중이면 즉시 FreeFall
            if (_animator && _verticalVelocity < 0f) {
                _animator.SetBool(_animIDFreeFall, true);
            }
        }

        // --- 중력 적용 ---
        if (_verticalVelocity < _terminalVelocity) {
            // [추가] Fall Multiplier: 하강 시 중력 추가 배율 적용
            float gravityScale = _verticalVelocity < 0f ? FallMultiplier : 1.0f;
            _verticalVelocity += Gravity * gravityScale * Time.deltaTime;
        }
    }
}