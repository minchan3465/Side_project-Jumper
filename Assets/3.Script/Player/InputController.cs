using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour {
	[Header("Input Values")]
	public Vector2 move = Vector2.zero;
	public Vector2 look = Vector2.zero;
	public bool jump;
	public bool sprint;

	[Header("Settings")]
	public bool cursorLocked = true;

	public void OnMove(InputValue value) => move = value.Get<Vector2>();
	public void OnLook(InputValue value) => look = value.Get<Vector2>();
	public void OnJump(InputValue value) => jump = value.isPressed;
	public void OnSprint(InputValue value) => sprint = value.isPressed;

	private void OnApplicationFocus(bool hasFocus) => Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
}

