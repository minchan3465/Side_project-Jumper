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
		if (player.MoveInput.magnitude > 0.1f) {
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
}

// ==========================================================
// JumpState
// ==========================================================
public class JumpState : PlayerState {
	public JumpState(PlayerController p, PlayerStateMachine sm, PlayerData d) : base(p, sm, d) { }
}

// ==========================================================
// FallState
// ==========================================================
public class FallState : PlayerState {
	public FallState(PlayerController p, PlayerStateMachine sm, PlayerData d) : base(p, sm, d) { }
}

// ==========================================================
// LandState ( 착지 경직 / Jump Buffer 처리 )
// ==========================================================
public class LandState : PlayerState {
	public LandState(PlayerController p, PlayerStateMachine sm, PlayerData d) : base(p, sm, d) { }
}

// ==========================================================
// WallRunState
// ==========================================================
public class WallRunState : PlayerState {
	public WallRunState(PlayerController p, PlayerStateMachine sm, PlayerData d) : base(p, sm, d) { }
}
