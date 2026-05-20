using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName ="Parkour/PlayerData")]
public class PlayerData : ScriptableObject {
	[Header("Movement")]
	public float moveSpeed = 7f;
	public float sprintSpeed = 11f;
	public float accleration = 15f; //지면 가속도
	public float deceleration = 20f;    //지면 감속도
	public float airAcceleration = 5f;  //공중 가속도 (제한적 제어)

	[Header("Jump")]
	public float jumpHeight = 2.5f;
	public float gravity = -20f;    //커스텀 중력 (유니티 기본값보다 강하게)
	public float fallMultiplier = 1.8f; //하강 시 중력 배수 (더 빠르게 떨어짐)
	public float jumpBufferTime = 0.15f;    //착지 직전 점프 입력 허용 시간
	public float coyoteTime = 0.12f;	//절벽 끝에서 잠깐 점프 허용

	[Header("Wall Run")]
	public float wallRunSpeed = 8f;
	public float wallRunGravity = -3f;   // 벽 달리기 중 약한 중력
	public float wallRunDuration = 2f;    // 최대 벽달리기 시간
	public float wallJumpForceUp = 10f;
	public float wallJumpForceAway = 6f;
	public float wallDetectDistance = 0.6f;
	public float wallRunMinSpeed = 3f;  //이 속도 미만이면 벽달리기 종료

	[Header("Land")]
	public float hardLandThreshold = -10f;  //이 속도 이하로 착지 시 경직
	public float hardLandDuration = 0.3f;
}
