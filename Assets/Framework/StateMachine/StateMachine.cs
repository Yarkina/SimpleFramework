using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine
{
	public StateMachine() { }
	public void AddState(State state)
	{
		if (state == null)
		{
			return;
		}
		if (state_dict_.ContainsKey(state.id))
		{
			LogSystem.Debug("add state failed, state already exist " + state.id);
			return;
		}
		state_dict_.Add(state.id, state);
		state.fsm = this;
	}

	public void RemoveState(string id)
	{
		State s;
		if (state_dict_.TryGetValue(id, out s))
		{
			s.fsm = null;
			state_dict_.Remove(id);
		}
	}

	public void Tick(float dt)
	{
		if (cur_state_ != null)
		{
			cur_state_.Tick(dt);
		}
	}
	public void FixedTick(float dt)
	{
		if (cur_state_ != null)
		{
			cur_state_.FixedTick(dt);
		}
	}

	public void Goto(string id)
	{
		State s;
		if (!state_dict_.TryGetValue(id, out s))
		{
			Debug.LogError("go to state failed! not find state " + id);
			return;
		}
		if (cur_state_ != null && cur_state_.id == s.id)
		{
			return;
		}
		if (cur_state_ != null)
		{
			cur_state_.Exit();
		}
		cur_state_ = s;
		cur_state_.Enter();
	}

	public void Stop()
	{
		if (cur_state_ != null)
		{
			cur_state_.Exit();
			cur_state_ = null;
		}
	}

	private State cur_state_;
	private Dictionary<string, State> state_dict_ = new Dictionary<string, State>();
}