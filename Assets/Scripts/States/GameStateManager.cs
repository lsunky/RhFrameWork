using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RhFrameWork;
using RhFrameWork.StateMachine;
using System;

public class GameStateManager : Singleton<GameStateManager>
{
    public bool showGameStateLog = true;
    public delegate void GameStateChangDel(GameStates state);
    public GameStateChangDel GameStateChangEvent;

    public delegate void DownErrorDel(string path, string errorInfo);
    public DownErrorDel DownErrorEvent;

    public delegate void DownProgressChangeDel(float hasDownSize, float allSize, float degree);
    public DownProgressChangeDel DownProgressChangeEvent;

    public delegate void ShowPopDel(bool isDoubleBtn, string strContent, Action sureAction, Action cancaleAction);
    public ShowPopDel ShowPopEvent;
    public void CreateStateMachine()
    {
        StateMachine stateMachine = new StateMachine(new GameStateCheckAppVersion(), null);
    }
    public void ShowDownError(string path, string errorInfo)
    {
        if (DownErrorEvent != null)
            DownErrorEvent(path, errorInfo);
    }

    public void GameStateChang(GameStates state)
    {
        if (GameStateManager.Instance.showGameStateLog)
            UDebug.Log("GameState:   " + state);
        if (GameStateChangEvent != null)
            GameStateChangEvent(state);
    }

    public void DownProgressHandle(GameStates state, List<FileUpdateVo> currentDownList, int currentDownIndex, float allSize)
    {
        float hasDownSize = Util.SumSize(currentDownList, currentDownIndex);
        float degree = 0;
        if (allSize != 0)
            degree = hasDownSize / allSize;
        if (DownProgressChangeEvent != null)
            DownProgressChangeEvent(hasDownSize, allSize, degree);
    }

    public void ShowPop(bool isDoubleBtn, string strContent, Action sureAction, Action cancaleAction)
    {
        if (ShowPopEvent != null)
        {
            ShowPopEvent(isDoubleBtn, strContent, sureAction, cancaleAction);
        }

    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}
