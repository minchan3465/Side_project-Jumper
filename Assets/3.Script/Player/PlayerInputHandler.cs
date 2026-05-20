using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// New Input System 입력을 받아 나머지 시스템이 읽을 수 있도록 캐싱합니다.
/// PlayerController가 이 컴포넌트를 참조합니다.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour {
	// -- 읽기 전용 프로퍼티 ---------------------------------
	public Vector2 MoveInput { get; private set; }
	public Vector2 LookInput { get; private set; }
	public bool JumpPressed { get; private set; }	//이번 프레임만 true
	public bool JumpHeld { get; private set; }
	public bool SprintHeld { get; private set; }

	// -- Input Action 콜백 (Action Map : Player) ------------

	//Move
	public void OnMove(InputAction.CallbackContext ctx) 
		=> MoveInput = ctx.ReadValue<Vector2>();

	//Look (Mouse Delta)
	public void OnLook(InputAction.CallbackContext ctx)
		=> LookInput = ctx.ReadValue<Vector2>();

	//Jump
	public void OnJump(InputAction.CallbackContext ctx) {
		JumpHeld = ctx.performed;

		//pressed 순간만 JumpPressed = true -> LateUpdate에서 초기화
		if (ctx.started)
			JumpPressed = true;
	}

	//Sprint
	public void OnSprint(InputAction.CallbackContext ctx)
		=> SprintHeld = ctx.performed;

	// -- 1프레임 입력 초기화 ---------------------------------
	private void LateUpdate() {
		JumpPressed = false;
		LookInput = Vector2.zero;	//Mouse Delta는 매 프레임 초기화 필요
	}
}
