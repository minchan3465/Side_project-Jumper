using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerController : MonoBehaviour {
	[Header("Data")]
	[SerializeField] private PlayerData data;

	[Header("Camera")]
	[SerializeField] private Transform cameraTransform;

	// -- 컴포넌트 -------------------------------------
	public CharacterController CC;
	public PlayerInputHandler Input;

	// -- FSM ------------------------------------------
	public PlayerStateMachine StateMachine { get; private set; }
	public IdleState IdleState { get; private set; }
	public RunState RunState { get; private set; }
	public SprintState SprintState { get; private set; }
	public JumpState JumpState { get; private set; }
	public FallState FallState { get; private set; }
	public WallRunState WallRunState { get; private set; }
	public LandState LandState { get; private set; }

	// -- 물리 상태 (CharacterController는 속도를 직접 관리) --
	public Vector3 Velocity { get; set; }	//현재 속도 (State에서 직접 수정)
	public float VerticalSpeed { 
		get => Velocity.y; 
		set => Velocity = new Vector3(Velocity.x, value, Velocity.z); 
	}

	// -- 감지 결과 -------------------------------------
	public bool IsGrounded { get; private set; }
	public bool IsOnWall { get; private set; }
	public Vector3 WallNormal { get; private set; }
	public bool IsWallOnRight { get; private set; }

	// -- 타이머 (Coyote Time / Jump Buffer) ------------
	public float LastGroundedTime { get; private set; }		//마지막으로 땅에 있던 시간
	public float LastJumpPressTime { get; private set; }    //마지막 점프 입력 시간

	// - 착지 속도 기록 ---------------------------------
	public float LandingSpeed { get; private set; }

	// -- 참조 ------------------------------------------
	public PlayerData Data => data;
	public Transform CameraTransform => cameraTransform;
	public bool CoyoteTimeValid => (Time.time - LastGroundedTime) < data.coyoteTime;
	public bool JumpBufferValid => (Time.time - LastJumpPressTime) < data.jumpBufferTime;

	private bool wasGrounded;
	// --------------------------------------------------
	private void Awake() {
		TryGetComponent(out CC);
		TryGetComponent(out Input);

		StateMachine = new PlayerStateMachine();
		IdleState = new IdleState(this, StateMachine, data);
		RunState = new RunState(this, StateMachine, data);
		SprintState = new SprintState(this, StateMachine, data);
		JumpState = new JumpState(this, StateMachine, data);
		FallState = new FallState(this, StateMachine, data);
		WallRunState = new WallRunState(this, StateMachine, data);
		LandState = new LandState(this, StateMachine, data);
	}

	private void Start() {
		StateMachine.Initialize(IdleState);
	}

	private void Update() {
		// Jump Buffer 타이머 갱식
		if (Input.JumpPressed)
			LastJumpPressTime = Time.time;

		CheckGround();
		CheckWall();
		StateMachine.CurrentState.Update();

		CC.Move(Velocity * Time.deltaTime);
	}

	private void FixedUpdate() {
		StateMachine.CurrentState.FixedUpdate();
	}

	// -- 지면 감지 -------------------------------------
	private void CheckGround() {
		wasGrounded = IsGrounded;
		IsGrounded = CC.isGrounded;

		if (IsGrounded) 
			LastGroundedTime = Time.time;

		//착지 순간 속도 기록
		if (IsGrounded && !wasGrounded) 
			LandingSpeed = Velocity.y;
	}

	// -- 벽 감지 ---------------------------------------
	private void CheckWall() {
		LayerMask wallLayer = LayerMask.GetMask("Wall");
		bool hitRight = Physics.Raycast(transform.position, transform.right, out RaycastHit rightHit, data.wallDetectDistance, wallLayer);
		bool hitLeft = Physics.Raycast(transform.position, -transform.right, out RaycastHit leftHit, data.wallDetectDistance, wallLayer);

		if(hitRight) {
			IsOnWall = true;
			WallNormal = rightHit.normal;
			IsWallOnRight = true;
		} else if(hitLeft) {
			IsOnWall = true;
			WallNormal = leftHit.normal;
			IsWallOnRight = false;
		} else {
			IsOnWall = false;
		}
	}

	// -- 공용 이동 계산 --------------------------------
	/// <summary>
	/// 카메라 방향 기준 수평 이동 속도를 계산해 Velocity.xz에 적용
	/// </summary>
	public void ApplyHorizontalMovement(float targetSpeed, float accel) {
		//카메라 기준 이동 방향
		Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
		Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

		Vector2 moveInput = Input.MoveInput;
		Vector3 moveDir = (camForward * moveInput.y + camRight * moveInput.x).normalized;
		Vector3 targetVel = moveDir * targetSpeed;

		//현재 수평 속도에서 목표 속도로 부드럽게 전환
		Vector3 currentHorizontal = new Vector3(Velocity.x, 0f, Velocity.z);
		Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, targetVel, accel * Time.deltaTime);

		Velocity = new Vector3(newHorizontal.x, Velocity.y, newHorizontal.z);
	}
	/// <summary>
	/// 커스텀 중력 적용 (CharacterController는 중력 미포함)
	/// </summary>
	public void ApplyGravity(float gravityOverride = float.NaN) {
		if(IsGrounded && Velocity.y < 0f) {
			Velocity = new Vector3(Velocity.x, -2f, Velocity.z);    //지면 밀착
			return;
		}

		float g = float.IsNaN(gravityOverride) ? data.gravity : gravityOverride;

		//하강 시 fallMultiplier 적용
		if (Velocity.y < 0f) 
			g *= data.fallMultiplier;

		VerticalSpeed += g * Time.deltaTime;
	}
}
