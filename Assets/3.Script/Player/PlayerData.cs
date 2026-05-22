using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName ="Parkour/PlayerData")]
public class PlayerData : ScriptableObject {
	[Header("Movement")]
	public float moveSpeed = 7f;
	public float sprintSpeed = 11f;
	public float accleration = 15f;		//지면 가속도
	public float deceleration = 20f;    //지면 감속도
	public float airAcceleration = 3f;  //공중 가속도 (제한적 제어)
	public float airControl = 2.5f;		// 실제 공중 조작감 (낮을수록 둔함)
	public float groundFriction = 18f;  // 입력 없을 때 감속 (deceleration과 별도)

	[Header("Jump")]
	public float jumpHeight = 2.5f;
	public float gravity = -14f;    //커스텀 중력 (유니티 기본값보다 강하게)
	public float fallMultiplier = 1.4f; //하강 시 중력 배수 (더 빠르게 떨어짐)
	public float jumpBufferTime = 0.15f;    //착지 직전 점프 입력 허용 시간
	public float coyoteTime = 0.12f;	//절벽 끝에서 잠깐 점프 허용

	[Header("Wall Run")]
	public float wallRunSpeed = 8f;
	public float wallRunGravity = -3f;   // 벽 달리기 중 약한 중력
	public float wallRunDuration = 0.8f;    // 최대 벽달리기 시간
	public float wallRunEntryBoost = 3f; // 진입 순간 속도 부스트 (미러스 엣지)
	public float wallRunEntryLiftSpeed = 2f; // 진입 시 위로 살짝 밀어주는 속도
	public float wallRunEntryLiftDuration = 0.2f;   // 진입 후 이 시간동안 중력 무시
	public float wallRunMaxFallEntry = -4f;          // 이보다 빠른 낙하 중엔 벽런 진입 불가
	
	public float wallJumpForceUp = 10f;
	public float wallJumpForceAway = 6f;
	public float wallJumpAirControlDuration = 0.5f;     // 벽점프 후 공중제어 제한 시간
	public float wallJumpAirControlMultiplier = 0.15f;  // 제한 중 가속도 배수

	public float wallDetectDistance = 0.33f;
	public float wallRunMinSpeed = 4f;  //이 속도 미만이면 벽달리기 종료
	public float wallRunAcceleration = 12f; // 벽달리기 진입 가속도 (관성감)
	public float wallStickForce = 15f;  // 벽 밀착 유지력

	[Header("Land")]
	public float hardLandThreshold = -20f;  //이 속도 이하로 착지 시 경직
	public float hardLandDuration = 0.3f;
}
