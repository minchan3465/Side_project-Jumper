using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerController : MonoBehaviour {
	[Header("플레이어")]
	public float MoveSpeed = 2.0f;          //이동 속도
	public float SprintSpeed = 6.0f;        //질주 속도
	public float SpeedChangeRate = 10.0f;   //가속,감속
	public float RotationSmoothTime = 0.12f;//플레이어가 이동 방향을 향해 회전하는 속도.

	public float JumpHeight = 1.2f;         //점프 높이
	public float JumpTimeout = 0.50f;       //점프 쿨타임.
	public float Gravity = -15.0f;          //중력 값.


	public float FallTimeout = 0.15f;       //추락 상태로 진입하기 전까지의 대기 시간. 계단 내려갈 때 유용. (애니메이션 이야기인듯?)

	public bool Grounded = true;            //플레이어 지면 체크
	public float GroundedOffset = -0.14f;   //거친 지면에서 유용하게 사용할 수 있는 오프셋 값. (뭐라는건지 모르겠당)
	public float GroundedRadius = 0.28f;    //지면 체크 구체 반지름. CharacterController의 반지름과 일치해야한다네요.

	public LayerMask GroundLayers;          //플레이어가 지면으로 인식할 레이어 마스크

	[Header("시네머신 카메라")]
	[SerializeField] private Transform _cameraRoot;
	[SerializeField] private float _mouseSensitive = 1.0f;


	private InputController _input;
	private CharacterController _controller;
	private GameObject _mainCamera;
	private Animator _animator;

	private float _targetYaw;
	private float _targetPitch;
	private float _threshold = 0.01f;

	private float _speed;
	private float _animationBlend;
	private float _targetRotation = 0.0f;
	private float _verticalVelocity;
	private float _rotationVelocity;
	private float _terminalVelocity = 53.0f;    //종단 속도

	private float _jumpTimeoutDelta;
	private float _fallTimeoutDelta;

	private float sprintThreshhold = 4f;

	//애니메이션 ID변수
	private int _animIDSpeed;
	private int _animIDStop;
	private int _animIDJump;
	private int _animIDGrounded;

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
		_fallTimeoutDelta = FallTimeout;
	}

	private void Update() {
		JumpAndGravity();
		GroundedCheck();
		Move();
	}
	private void LateUpdate() {
		CameraRotation();
	}

	private void AssignAnimationIDs() {
		_animIDSpeed = Animator.StringToHash("Speed");
		_animIDStop = Animator.StringToHash("Stop");
		_animIDJump = Animator.StringToHash("Jump");
		_animIDGrounded = Animator.StringToHash("Grounded");
	}
	private void GroundedCheck() {
		Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
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
		if (_input.move == Vector2.zero) targetSpeed = 0f;

		float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
		float speedOffset = 0.1f;

		if (_input.move == Vector2.zero) {
			// 질주 중이었고, 아직 정지 애니메이션 상태가 아닐 때만 1회 실행
			if (currentHorizontalSpeed > sprintThreshhold && !_animator.GetCurrentAnimatorStateInfo(0).IsName("RunToStop")) {
				_animator.SetTrigger(_animIDStop);
			}
		} else {
			// 다시 움직이기 시작하면 쌓여있을지 모를 Stop 트리거를 즉시 제거 (엉거주춤 방지)
			_animator.ResetTrigger(_animIDStop);
		}

		// 멈출 때(targetSpeed == 0)는 감속도를 낮춰서 미끄러지게 하고, 
		// 이동할 때는 원래의 가속도를 사용함
		float lerpSpeed = (_input.move == Vector2.zero) ? SpeedChangeRate * 0.5f : SpeedChangeRate;

		_animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
		if (_animationBlend < 0.01f) _animationBlend = 0f;

		if (currentHorizontalSpeed < targetSpeed - speedOffset ||
			currentHorizontalSpeed > targetSpeed + speedOffset) {
			// 선형적이지 않고 곡선적인 결과를 생성하여 더 유기적인 속도 변화를 제공함
			// 참고: Lerp의 T는 클램프(전달된 범위 내로 제한)되므로 속도를 별도로 제한할 필요 없음
			_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * 1f, Time.deltaTime * lerpSpeed);
			_speed = Mathf.Round(_speed * 1000f) / 1000f; // 속도 값을 소수점 셋째 자리까지 반올림
		} else {
			_speed = targetSpeed;
		}

		//애니메이션 블랜드 관련.


		//입력 방향 정규화
		Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
		if (_input.move != Vector2.zero) {
			_targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
			float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

			transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
		}

		Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

		_controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

		//애니메이션 업데이트
		if (_animator) {
			_animator.SetFloat(_animIDSpeed, _animationBlend);
		}
	}
	private void JumpAndGravity() {
		if (Grounded) {
			_fallTimeoutDelta = FallTimeout;    //추락 타임아웃 타이머 초기화.

			//애니메이터 업데이트 (초기화.)
			if (_animator) {
				_animator.SetBool(_animIDGrounded, true);
				_animator.SetBool(_animIDJump, false);
				// _animator.SetBool(_animIDFreeFall, false); // 필요 시
			}

			if (_verticalVelocity < 0.0f) {
				_verticalVelocity = -2f;
			}

			if (_input.jump && _jumpTimeoutDelta <= 0.0f) {
				_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
				//에니메이터 업데이트 (점프)
				if (_animator) {
					_animator.SetBool(_animIDJump, true); // Jump 시작
					_animator.ResetTrigger(_animIDStop);  // 멈춤 트리거 리셋
				}
			}

			if (_jumpTimeoutDelta >= 0.0f) {
				_jumpTimeoutDelta -= Time.deltaTime;
			}
		} else {
			_jumpTimeoutDelta = JumpTimeout;

			if (_fallTimeoutDelta >= 0.0f) {
				_fallTimeoutDelta -= Time.deltaTime;
			} else {
				//애니메이터 업데이트 (추락 애니메이션)
				if (_animator) {
					_animator.SetBool(_animIDGrounded, false);
				}
			}

			_input.jump = false;    //지면에 있지 않는 상태이므로 점프 입력 해제.
		}

		// 종단 속도(terminal velocity)보다 낮으면 시간에 따라 중력 적용
		// (델타 타임을 두 번 곱하여 시간에 따라 선형적으로 가속)
		if (_verticalVelocity < _terminalVelocity) {
			_verticalVelocity += Gravity * Time.deltaTime;
		}
	}
}
