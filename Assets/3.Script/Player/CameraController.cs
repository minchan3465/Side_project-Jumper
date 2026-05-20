using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

[DefaultExecutionOrder(-5)]
public class CameraController : MonoBehaviour {
	[Header("References")]
	[SerializeField] private Transform cameraTarget;    //Player 하위 빈 오브젝트
	[SerializeField] private CinemachineCamera cmCaemra;

	[Header("Mouse Sensitivity")]
	[SerializeField] private float sensitivityX = 0.15f;
	[SerializeField] private float sensitivityY = 0.15f;

	[Header("Vertical Clamp")]
	[SerializeField] private float verticalMin = -80f;
	[SerializeField] private float verticalMax = 80f;

	[Header("Wall Run Tilt")]
	[SerializeField] private float tiltAngle = 12f;
	[SerializeField] private float tiltSpeed = 8f;

	// -- 내부 상태 ----------------------------------------
	private PlayerInputHandler inputHandler;
	private PlayerController playerController;

	private float pitch = 0f;   //수직 (X축)
	private float yaw = 0f;     //수평 (Y축)
	private float currentTilt = 0f; //벽달리기 기울기 Z(축)

	// -----------------------------------------------------
	private void Awake() {
		TryGetComponent(out inputHandler);
		TryGetComponent(out playerController);

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	/// <summary>
	/// LateUpdate: 물리/이동 처리 이후 카메라 회전 적용
	/// </summary>
	private void LateUpdate() {
		ApplyLook();
		ApplyWallTilt();
	}

	// -- Look 처리 ----------------------------------------
	private void ApplyLook() {
		Vector2 delta = inputHandler.LookInput;

		yaw += delta.x * sensitivityX;
		pitch -= delta.y * sensitivityY;
		pitch = Mathf.Clamp(pitch, verticalMin, verticalMax);

		// 플레이어 루트: 수평 방향만 회전 (이동 방향 결정)
		transform.rotation = Quaternion.Euler(0f, yaw, 0f);

		// CameraTarget: 수직 + 기울기 적용 (카메라만)
		cameraTarget.localRotation = Quaternion.Euler(pitch, 0f, currentTilt);
	}

	private void ApplyWallTilt() {
		float targetTilt = 0f;

		if (playerController.StateMachine.CurrentState == playerController.WallRunState)
			targetTilt = playerController.IsWallOnRight ? tiltAngle : -tiltAngle;

		currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);
	}

	// -- 공개 API ---------------------------------------------
	public void SetSensitivity(float x, float y) { sensitivityX = x; sensitivityY = y; }

	public void SetCursorLock(bool locked) {
		Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
		Cursor.visible = !locked;
	}
}
