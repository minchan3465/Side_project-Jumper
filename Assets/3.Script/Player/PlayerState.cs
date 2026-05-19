using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerState {
	protected PlayerController player;
	protected PlayerStateMachine stateMachine;
	protected PlayerData data;

	public PlayerState(PlayerController player, PlayerStateMachine stateMachine, PlayerData data) {
		this.player = player;
		this.stateMachine = stateMachine;
		this.data = data;
	}

	public virtual void Enter() { }
	public virtual void Update() { }
	public virtual void FixedUpdate() { }
	public virtual void Exit() { }

}

