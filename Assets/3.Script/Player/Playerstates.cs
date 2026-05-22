using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ==========================================================
// IdleState
// ==========================================================
public class IdleState : PlayerState {
	public IdleState(PlayerController p, PlayerStateMachine sm, PlayerData d) : base(p, sm, d) { }

	public override void Enter() {
		//수평 속도 감속 시작
	}

	public override void Update() {
		player.ApplyGravity();

		//수평 감속
		player.ApplyHorizontalMovement(0f, data.deceleration);

		//전환 조건
		if (player.Input.MoveInput.magnitude > 0.1f) {
			stateMachine.ChangeState(player.RunState);
			return;
		}

		if(player.JumpBufferValid && player.CoyoteTimeValid) {
			stateMachine.ChangeState(player.JumpState);
			return;
		}

		if(!player.IsGrounded && !player.CoyoteTimeValid) {
			stateMachine.ChangeState(player.FallState);
		}
	}
}

// ==========================================================
// RunState
// ==========================================================
public class RunState : PlayerState {
	public RunState(PlayerController p, PlayerStateMachine sm, PlayerData d) : base(p, sm, d) { }

	public override void Update() {
		player.ApplyGravity();
		player.ApplyHorizontalMovement(data.moveSpeed, data.accleration);

		//전환 조건
		if(player.Input.MoveInput.magnitude < 0.1f) {
			stateMachine.ChangeState(player.IdleState);
			return;
		}

		if (player.Input.SprintHeld && player.Input.MoveInput.magnitude > 0.1f) {
			stateMachine.ChangeState(player.SprintState);
			return;
		}

		if (player.JumpBufferValid && player.CoyoteTimeValid) {
			stateMachine.ChangeState(player.JumpState);
			return;
		}

		if (player.IsOnWall && player.IsNewWall && !player.IsGrounded && player.Input.MoveInput.magnitude > 0.1f) {
			stateMachine.ChangeState(player.WallRunState);
			return;
		}

		if (!player.IsGrounded && !player.CoyoteTimeValid) {
			stateMachine.ChangeState(player.FallState);
		}
	}
}
// ==========================================================
// SprintState
// ==========================================================
public class SprintState : PlayerState {
	public SprintState(PlayerController p, PlayerStateMachine sm, PlayerData d) : base(p, sm, d) { }

	public override void Update() {
		player.ApplyGravity();
		player.ApplyHorizontalMovement(data.sprintSpeed, data.accleration);

		if (!player.Input.SprintHeld) {
			stateMachine.ChangeState(player.Input.MoveInput.magnitude > 0.1f ? player.RunState : player.IdleState);
			return;
		}

		if (player.Input.MoveInput.magnitude < 0.1f) {
			stateMachine.ChangeState(player.IdleState);
			return;
		}

		if (player.JumpBufferValid && player.CoyoteTimeValid) {
			stateMachine.ChangeState(player.JumpState);
			return;
		}

		if (player.IsOnWall && player.IsNewWall && !player.IsGrounded) {
			stateMachine.ChangeState(player.WallRunState);
			return;
		}

		if (!player.IsGrounded && !player.CoyoteTimeValid)
			stateMachine.ChangeState(player.FallState);
	}
}


// ==========================================================
// JumpState
// ==========================================================
	public class JumpState : PlayerState {
	public JumpState(PlayerController p, PlayerStateMachine sm, PlayerData d) : base(p, sm, d) { }

	public override void Enter() {
		// v= sqrt(2 * |gravity| * jumpHeight)
		float jumpSpeed = Mathf.Sqrt(2f * Mathf.Abs(data.gravity) * data.jumpHeight);
		player.VerticalSpeed = jumpSpeed;

		// Jump Buffer 소비
		// LastJumpPressTime을 초기화해 중복 점프 방지
	}

	public override void Update() {
		player.ApplyGravity();
		player.ApplyHorizontalMovement(data.moveSpeed, data.airControl);

		//벽 달리기 진입
		if (player.IsOnWall && player.IsNewWall && !player.IsGrounded) {
			stateMachine.ChangeState(player.WallRunState);
			return;
		}

		//정점 이후 -> Fall
		if (player.Velocity.y <0f) {
			stateMachine.ChangeState(player.FallState);
			return;
		}

		//착지 (빠른 착지 대응)
		if(player.IsGrounded) {
			stateMachine.ChangeState(player.LandState);
		}
	}
}

// ==========================================================
// FallState
// ==========================================================
public class FallState : PlayerState {
	public FallState(PlayerController p, PlayerStateMachine sm, PlayerData d) : base(p, sm, d) { }

	public override void Update() {
		player.ApplyGravity();
		player.ApplyHorizontalMovement(data.moveSpeed, data.airControl);

		//Jump Buffer로 착지 직전 점프 선입력 허용
		// -> LandState에서 Buffer 확인 후 JumpState로 전환

		//벽달리기
		bool fallingTooFast = player.Velocity.y < data.wallRunMaxFallEntry;
		if (player.IsOnWall && player.IsNewWall && player.Input.MoveInput.magnitude > 0.1f && !fallingTooFast) {
			stateMachine.ChangeState(player.WallRunState);
			return;
		}
		//착지
		if (player.IsGrounded) {
			stateMachine.ChangeState(player.LandState);
		}
	}
}

// ==========================================================
// LandState ( 착지 경직 / Jump Buffer 처리 )
// ==========================================================
public class LandState : PlayerState {
	private float landTimer;
	private bool isHardLand;
	private bool skipFrame;   // Enter()에서 바로 전환하면 FSM 불안정 -> 1프레임 대기

	public LandState(PlayerController p, PlayerStateMachine sm, PlayerData d) : base(p, sm, d) { }

	public override void Enter() {
		isHardLand = player.LandingSpeed < data.hardLandThreshold;
		landTimer = isHardLand ? data.hardLandDuration : 0f;
		skipFrame = true;  // 첫 Update에서 처리
	}

	public override void Update() {
		player.ApplyGravity();

		//일반 착지 (감속 없음)
		if (!isHardLand) {
			if (skipFrame) { skipFrame = false; return; }
			TransitionFromLand();
			return;
		}

		//하드 착지 (경직 적용 및 감속)
		player.ApplyHorizontalMovement(0f, data.groundFriction);
		landTimer -= Time.deltaTime;
		if (landTimer <= 0f)
			TransitionFromLand();
	}

	private void TransitionFromLand() {
		if (player.JumpBufferValid)
			stateMachine.ChangeState(player.JumpState);
		else if (player.Input.MoveInput.magnitude > 0.1f)
			stateMachine.ChangeState(player.Input.SprintHeld ? player.SprintState : player.RunState);
		else
			stateMachine.ChangeState(player.IdleState);
	}
}

// ==========================================================
// WallRunState
// ==========================================================
public class WallRunState : PlayerState {
	private float wallRunTimer;

	public WallRunState(PlayerController p, PlayerStateMachine sm, PlayerData d) : base(p, sm, d) { }

	public override void Enter() {
		wallRunTimer = data.wallRunDuration;

		// 현재 벽을 등록 → 같은 벽 재진입 차단
		player.RegisterLastWall();

		// 수직 속도를 0으로 즉시 끊지 않고, 위로 살짝 밀어줌 (미러스 엣지: 진입 시 상승감)
		player.VerticalSpeed = Mathf.Max(player.VerticalSpeed, 0f) + data.wallRunEntryLiftSpeed;

		// 진입 순간 수평 속도 부스트 (벽 방향 기준)
		Vector3 wallForward = Vector3.Cross(player.WallNormal, Vector3.up);
		if (Vector3.Dot(wallForward, player.CameraTransform.forward) < 0f)
			wallForward = -wallForward;

		Vector3 currentH = new Vector3(player.Velocity.x, 0f, player.Velocity.z);
		Vector3 boosted = currentH + wallForward * data.wallRunEntryBoost;

		// 최대속도 캡 (wallRunSpeed + boost만큼)
		if (boosted.magnitude > data.wallRunSpeed + data.wallRunEntryBoost)
			boosted = boosted.normalized * (data.wallRunSpeed + data.wallRunEntryBoost);

		player.Velocity = new Vector3(boosted.x, player.VerticalSpeed, boosted.z);
	}

	public override void Update() {
		wallRunTimer -= Time.deltaTime;

		//종료 조건
		bool tooSlow = new Vector3(player.Velocity.x, 0f, player.Velocity.z).magnitude < data.wallRunMinSpeed;
		if (!player.IsOnWall || wallRunTimer <= 0f || tooSlow) {
			stateMachine.ChangeState(player.FallState);
			return;
		}
 
		if (player.IsGrounded) {
			stateMachine.ChangeState(player.LandState);
			return;
		}
 
		// 벽 점프
		if(player.Input.JumpPressed) {
			WallJump();
			return;
		}
 
		ApplyWallRunMovement();
	}

	private void ApplyWallRunMovement() {
		//벽 면을 따라 달리는 방향 (WallNormal x Up)
		Vector3 wallForward = Vector3.Cross(player.WallNormal, Vector3.up);

		//플레이어가 바라보는 방향과 일치하기 위해 내적으로 방향 결정
		if (Vector3.Dot(wallForward, player.CameraTransform.forward) < 0f)
			wallForward = -wallForward;

		//수평은 벽 방향 고정 속도, 수직은 약한 중력
		Vector3 currentHorizontal = new Vector3(player.Velocity.x, 0f, player.Velocity.z);
		Vector3 targetHorizontal = wallForward * data.wallRunSpeed;
		float t = 1f - Mathf.Exp(-data.wallRunAcceleration * Time.deltaTime);
		Vector3 newHorizontal = Vector3.Lerp(currentHorizontal, targetHorizontal, t);

		player.Velocity = new Vector3(newHorizontal.x, player.Velocity.y, newHorizontal.z);

		//약간 하강 중력
		player.VerticalSpeed += data.wallRunGravity * Time.deltaTime;

		// 벽 밀착: Velocity가 아닌 CC.Move에 추가 이동으로 처리
		// (Velocity 오염 방지 - 별도 stickyForce로 CC.Move 호출_
		// 여기서는 WallNormal 반대 방향으로 작은 힘을 수직속도와 무관하게 추가
		player.Velocity += -player.WallNormal * data.wallStickForce * Time.deltaTime;
	}

	private void WallJump() {
		//쿨다운 시작 -> 벽 재진입 방지
		player.StartWallJumpCooldown();

		//벽 법선 방향 + 위쪽으로 튕겨나감
		Vector3 jumpDir = (player.WallNormal + Vector3.up).normalized;
		player.Velocity = new Vector3(jumpDir.x * data.wallJumpForceAway, data.wallJumpForceUp, jumpDir.z * data.wallJumpForceAway);

		stateMachine.ChangeState(player.JumpState);
	}

	public override void Exit() {
		//벽달리기 종료 시 약간의 관성 유지
	}
}
