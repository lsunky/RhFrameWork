using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RhFrameWork;
using RhFrameWork.StateMachine;
using System.IO;
public class State_GameMain : State
{
    public override void EnterState(Hashtable hash)
    {
        AssetBundleManager.Instance.Initialize();
        UiMgr.Instance.Open(WindowId.Login, null);
    }

    public override void ExitState()
    {
        
    }
}