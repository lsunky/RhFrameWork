
using System;
using System.Collections.Generic;
using System.Collections;
namespace RhFrameWork.StateMachine
{
    public class StateMachine
    {
        public delegate void StateChangedDel(State state);
        public event StateChangedDel StateChangedEvent;

        private readonly Dictionary<Type, State> states = new Dictionary<Type, State>();
        private State currentState;

        public StateMachine(State initialState,Hashtable hash)
        {
            initialState.SetMachine(this);
            states.Add(initialState.GetType(), initialState);
            currentState = initialState;
            initialState.EnterState(hash);
        }
        
        public void SetState<T>(Hashtable hash) where T : State
        {
            var newState = typeof(T);
            if (currentState.GetType() == newState)
                return;

            if (currentState != null)
                currentState.ExitState();

            currentState = GetState(newState);
            currentState.EnterState(hash);

            if (StateChangedEvent != null)
                StateChangedEvent(currentState);
        }

        private State GetState(Type newState)
        {
            if (states.ContainsKey(newState))
                return states[newState];

            var state = Activator.CreateInstance(newState) as State;
            if (state != null)
            {
                state.SetMachine(this);
                states.Add(newState, state);
                return state;
            }
            return null;
        }
    }
}

