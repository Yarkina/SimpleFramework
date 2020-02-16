using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State
{
	public State(string id)
	{
		id_ = id;
		is_active_ = false;
	}
	public StateMachine fsm { set { fsm_ = value; } get { return fsm_; } }
	public string id { get { return id_; } }
	public bool is_active { get { return is_active_; } }

	public void Enter()
	{
		if (is_active_)
		{
			return;
		}
		// Debug.Log("enter state " + id);
		if(!is_init_)
		{
			is_init_=true;
			OnInit();
		}
		is_active_ = true;
		OnEnter();
	}

	public void Exit()
	{
		if (!is_active_)
		{
			return;
		}
		// Debug.Log("exit state " + id);
		is_active_ = false;
		OnExit();
	}

	public virtual void OnEnter() { }
	public virtual void Tick(float dt) { }
	public virtual void FixedTick(float dt) { }
	public virtual void OnExit() { }
	public virtual void OnInit() { }

	protected string id_;
	protected StateMachine fsm_;
	protected bool is_active_ = false;
	protected bool is_init_ = false;

}