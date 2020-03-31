
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
namespace RhFrameWork.StateMachine
{
    public abstract class State
    {
        protected StateMachine stateMachine;
       
        public void SetMachine(StateMachine _stateMachine)
        {
            this.stateMachine = _stateMachine;
        }

        public abstract void EnterState(Hashtable hash);
        
        public abstract void ExitState();
    }
}

