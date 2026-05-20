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

		if(player.IsOnWall && !player.IsGrounded && player.Input.MoveInput.magnitude > 0.1f) {
			stateMachine.ChangeState(player.WallRunState);
			return;
		}

		if(!player.IsGrounded && !player.CoyoteTimeValid) {
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

		if (player.IsOnWall && !player.IsGrounded) {
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
		player.ApplyHorizontalMovement(data.moveSpeed, data.airAcceleration);

		//벽 달리기 진입
		if(player.IsOnWall && !player.IsGrounded) {
			stateMachine.ChangeState(player.WallRunState);
			return;
		}

		//정점 이후 -> Fall
		if(player.Velocity.y <0f) {
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
		player.ApplyHorizontalMovement(data.moveSpeed, data.airAcceleration);

		//Jump Buffer로 착지 직전 점프 선입력 허용
		// -> LandState에서 Buffer 확인 후 JumpState로 전환

		//벽달리기
		if(player.IsOnWall && player.Input.MoveInput.magnitude > 0.1f) {
			stateMachine.ChangeState(player.WallRunState);
			return;
		}

		//착지
		if(player.IsGrounded) {
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

	public LandState(PlayerController p, PlayerStateMachine sm, PlayerData d) : base(p, sm, d) { }

	public override void Enter() {
		isHardLand = player.LandingSpeed < data.hardLandThreshold;
		landTimer = isHardLand ? data.hardLandDuration : 0f;

		//Jump Buffer가 있으면 경직 없이 바로 점프
		if(!isHardLand && player.JumpBufferValid) {
			stateMachine.ChangeState(player.JumpState);
		}
	}

	public override void Update() {
		player.ApplyGravity();
		player.ApplyHorizontalMovement(0f, data.deceleration);

		landTimer -= Time.deltaTime;

		if (landTimer <= 0f) {
			// 경직 종료 후 상태 전환
			if (player.JumpBufferValid)
				stateMachine.ChangeState(player.JumpState);
			else if (player.Input.MoveInput.magnitude > 0.1f)
				stateMachine.ChangeState(player.RunState);
			else
				stateMachine.ChangeState(player.IdleState);
		}
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

		// 수직 속도 리셋 (벽에 붙는 느낌)
		player.VerticalSpeed = 0f;
	}

	public override void Update() {
		wallRunTimer -= Time.deltaTime;

		ApplyWallRunMovement();

		//벽 점프
		if(player.Input.JumpPressed) {
			WallJump();
			return;
		}

		//종료 조건
		bool tooSlow = new Vector3(player.Velocity.x, 0f, player.Velocity.z).magnitude < data.wallRunMinSpeed;
		if (!player.IsOnWall || wallRunTimer <= 0f || tooSlow) {
			stateMachine.ChangeState(player.FallState);
			return;
		}

		if (player.IsGrounded) {
			stateMachine.ChangeState(player.LandState);
		}
	}

	private void ApplyWallRunMovement() {
		//벽 면을 따라 달리는 방향 (WallNormal x Up)
		Vector3 wallForward = Vector3.Cross(player.WallNormal, Vector3.up);

		//플레이어가 바라보는 방향과 일치하기 위해 내적으로 방향 결정
		if (Vector3.Dot(wallForward, player.CameraTransform.forward) < 0f)
			wallForward = -wallForward;

		//수평은 벽 방향 고정 속도, 수직은 약한 중력
		Vector3 targetVel = wallForward * data.wallRunSpeed;
		player.Velocity = new Vector3(targetVel.x, player.Velocity.y, targetVel.z);

		//약간 하강 중력
		player.VerticalSpeed += data.wallRunGravity * Time.deltaTime;

		//벽족으로 살짝 당겨주기 (떨어지지 않도록)
		player.Velocity += -player.WallNormal * 2f;
	}

	private void WallJump() {
		//벽 법선 방향 + 위쪽으로 튕겨나감
		Vector3 jumpDir = (player.WallNormal + Vector3.up).normalized;
		player.Velocity = new Vector3(jumpDir.x * data.wallJumpForceAway, data.wallJumpForceUp, jumpDir.x * data.wallJumpForceAway);

		stateMachine.ChangeState(player.JumpState);
	}

	public override void Exit() {
		//벽달리기 종료 시 약간의 관성 유지
	}
}
