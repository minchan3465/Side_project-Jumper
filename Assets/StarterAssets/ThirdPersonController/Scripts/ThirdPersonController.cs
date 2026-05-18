using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* 참고: 애니메이션은 애니메이터 null 체크를 사용하여 
 * 캐릭터와 캡슐 컨트롤러 모두에서 호출됩니다.
 */

namespace StarterAssets {
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour {
        [Header("플레이어")]
        [Tooltip("캐릭터의 이동 속도 (m/s)")]
        public float MoveSpeed = 2.0f;

        [Tooltip("캐릭터의 질주 속도 (m/s)")]
        public float SprintSpeed = 5.335f;

        [Tooltip("캐릭터가 이동 방향을 향해 회전하는 속도")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("가속 및 감속 속도")]
        public float SpeedChangeRate = 10.0f;

        public AudioSource AudioFootsteps;
        public AudioSource LandingAudio;
        public AudioSource AudioFoley;
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("플레이어가 점프할 수 있는 높이")]
        public float JumpHeight = 1.2f;

        [Tooltip("캐릭터에 적용되는 중력 값. 엔진 기본값은 -9.81f입니다")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("다시 점프하기 위해 기다려야 하는 시간. 0f로 설정하면 즉시 다시 점프할 수 있습니다")]
        public float JumpTimeout = 0.50f;

        [Tooltip("추락 상태로 진입하기 전까지의 대기 시간. 계단을 내려갈 때 유용합니다")]
        public float FallTimeout = 0.15f;

        [Header("플레이어 지면 체크")]
        [Tooltip("캐릭터가 지면에 닿아 있는지 여부. CharacterController의 기본 grounded 체크와는 별개입니다")]
        public bool Grounded = true;

        [Tooltip("거친 지면에서 유용하게 사용할 수 있는 오프셋 값")]
        public float GroundedOffset = -0.14f;

        [Tooltip("지면 체크 구체의 반지름. CharacterController의 반지름과 일치해야 합니다")]
        public float GroundedRadius = 0.28f;

        [Tooltip("캐릭터가 지면으로 인식할 레이어 마스크")]
        public LayerMask GroundLayers;

        [Header("시네머신(Cinemachine)")]
        [Tooltip("시네머신 가상 카메라가 추적할 목표 오브젝트")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("카메라를 위로 얼마나 움직일 수 있는지 (도 단위)")]
        public float TopClamp = 70.0f;

        [Tooltip("카메라를 아래로 얼마나 움직일 수 있는지 (도 단위)")]
        public float BottomClamp = -30.0f;

        [Tooltip("카메라 각도를 추가로 보정하는 값. 고정된 상태에서 미세 조정 시 유용합니다")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("모든 축에 대해 카메라 위치를 고정할지 여부")]
        public bool LockCameraPosition = false;

        // 시네머신 변수
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // 플레이어 변수
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // 타임아웃 델타 타임 변수
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // 애니메이션 ID 변수
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse {
            get {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }


        private void Awake() {
            // 메인 카메라 참조 가져오기
            if (_mainCamera == null) {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start() {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError( "Starter Assets 패키지의 종속성이 누락되었습니다. Tools/Starter Assets/Reinstall Dependencies를 사용하여 복구하세요.");
#endif

            AssignAnimationIDs();

            // 시작 시 타임아웃 값 초기화
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update() {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
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
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck() {
            // 오프셋을 적용하여 구체의 위치 설정
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // 애니메이터가 있으면 지면 상태 업데이트
            if (_hasAnimator) {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation() {
            // 입력이 있고 카메라 위치가 고정되지 않은 경우
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition) {
                // 마우스 입력인 경우 Time.deltaTime을 곱하지 않음
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // 회전 값이 360도 범위 내에 있도록 제한
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // 시네머신이 이 대상을 추적함
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move() {
            // 질주 입력 여부에 따라 목표 속도 결정
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // 제거, 교체 또는 반복 수정이 쉽도록 설계된 간단한 가감속 로직

            // 참고: Vector2의 == 연산자는 근사치를 사용하므로 부동 소수점 오차에 안전하며 magnitude보다 연산량이 적음
            // 이동 입력이 없으면 목표 속도를 0으로 설정
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // 플레이어의 현재 수평 속도 참조
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // 목표 속도까지 가속 또는 감속
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset) {
                // 선형이 아닌 곡선적인 결과를 생성하여 더 유기적인 속도 변화 제공
                // 참고: Lerp의 T 값은 내부적으로 클램프되므로 속도를 직접 제한할 필요 없음
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // 속도 값을 소수점 셋째 자리에서 반올림
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            } else {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // 입력 방향 정규화
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // 참고: Vector2의 != 연산자는 근사치를 사용함
            // 이동 입력이 있을 때 플레이어를 회전시킴
            if (_input.move != Vector2.zero) {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // 카메라 위치에 상대적인 입력 방향을 향하도록 회전
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // 플레이어 이동 처리
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // 애니메이터 업데이트
            if (_hasAnimator) {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity() {
            if (Grounded) {
                // 추락 타임아웃 타이머 리셋
                _fallTimeoutDelta = FallTimeout;

                // 애니메이터 업데이트
                if (_hasAnimator) {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // 땅에 있을 때 수직 속도가 계속해서 떨어지는 것을 방지
                if (_verticalVelocity < 0.0f) {
                    _verticalVelocity = -2f;
                }

                // 점프 처리
                if (_input.jump && _jumpTimeoutDelta <= 0.0f) {
                    // sqrt(H * -2 * G) = 목표 높이에 도달하기 위해 필요한 속도 계산
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // 애니메이터 업데이트
                    if (_hasAnimator) {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // 점프 타임아웃 감소
                if (_jumpTimeoutDelta >= 0.0f) {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            } else {
                // 점프 타임아웃 타이머 리셋
                _jumpTimeoutDelta = JumpTimeout;

                // 추락 타임아웃 처리
                if (_fallTimeoutDelta >= 0.0f) {
                    _fallTimeoutDelta -= Time.deltaTime;
                } else {
                    // 애니메이터 업데이트
                    if (_hasAnimator) {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // 지면에 있지 않으면 점프 입력 해제
                _input.jump = false;
            }

            // 종단 속도(terminal velocity)보다 낮으면 시간에 따라 중력 적용
            // (델타 타임을 두 번 곱하여 시간에 따라 선형적으로 가속)
            if (_verticalVelocity < _terminalVelocity) {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax) {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected() {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // 선택되었을 때 지면 체크 콜라이더의 위치와 반지름에 맞는 기즈모 구체 그리기
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent) {
            if (animationEvent.animatorClipInfo.weight > 0.5f) {
                if (AudioFootsteps != null)
                    AudioFootsteps.Play();
                if (AudioFoley != null)
                    AudioFoley.Play();
            }
        }

        private void OnLand(AnimationEvent animationEvent) {
            if (animationEvent.animatorClipInfo.weight > 0.5f) {
                if (LandingAudio != null)
                    LandingAudio.Play();
            }
        }
    }
}