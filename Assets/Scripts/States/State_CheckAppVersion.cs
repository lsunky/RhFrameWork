/// <summary>
/// Author      Liuxun
/// Date        2018/4/17 15:50:56
/// Description:GameStateUnzipStreaming
/// </summary>

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RhFrameWork;
using RhFrameWork.StateMachine;
using System.IO;

/// <summary>
/// GameStateUnzipStreaming
/// </summary>

public class GameStateCheckAppVersion : State
{
    public override void EnterState(Hashtable hash)
    {
        GameStateManager.Instance.GameStateChang(GameStates.CheckAppVersion);
#if UNITY_EDITOR
        if (!AssetBundleManager.ResUpdateInEditor)
        {
            CheckAppVersionComplete(null,null, null);
            return;
        }
#endif
        TaskManager.Instance.StartCoroutine(LoadResVersion(AppConst.ResVersionConfigFile, AppConst.ResourceUrl));
    }

    public override void ExitState()
    {

    }
    

    private void CheckAppVersionComplete(VersionConfig serverVersionConfig, VersionConfig streamVersionConfig, FileUpdateVo versionVo)
    {
        if (GameStateManager.Instance.showGameStateLog)
            UDebug.Log("CheckAppVersionComplete");
        Hashtable hash = new Hashtable();
        hash["serverVersionConfig"] = serverVersionConfig;
        hash["streamVersionConfig"] = streamVersionConfig;
        hash["versionVo"] = versionVo;
        this.stateMachine.SetState<State_CopyToPersistent>(hash);
    }

    IEnumerator LoadResVersion(string versionFile, string _webUrl)
    {
        string random = DateTime.Now.ToString("yyyymmddhhmmss");
        FileUpdateVo versionVo = new FileUpdateVo(versionFile,  _webUrl, random, string.Empty, 10, false);

        string configStr;
        WWW www;
        if (Application.isMobilePlatform)
        {
            www = new WWW(versionVo.StreamingPath);
            yield return www;
            if (!string.IsNullOrEmpty(www.error))
            {
                GameStateManager.Instance.ShowDownError(versionVo.StreamingPath, www.error);
            }
            configStr = www.text;
            www.Dispose();
        }
        else
        {
            configStr = File.ReadAllText(versionVo.StreamingPath);
        }
        VersionConfig streamVersionConfig = JsonUtility.FromJson<VersionConfig>(configStr);

        if (GameStateManager.Instance.showGameStateLog)
            UDebug.Log("开始加载：" + versionVo.FileUrl);
        www = new WWW(versionVo.FileUrl);
        yield return www;

        if (!string.IsNullOrEmpty(www.error))
        {
            GameStateManager.Instance.ShowDownError(versionVo.FileUrl, www.error);
            yield break;
        }
        if (GameStateManager.Instance.showGameStateLog)
            UDebug.Log("加载结束：" + versionVo.FileName + www.text);
        configStr = www.text;
        www.Dispose();

        VersionConfig serverVersionConfig = JsonUtility.FromJson<VersionConfig>(configStr);
        UDebug.enableLog = serverVersionConfig.enableLog;
        //GameStateManager.Instance.showGameStateLog = serverVersionConfig.enableLog;
        Version myVersion = new Version(streamVersionConfig.packageVersion);
        Version cloudVersion = new Version(serverVersionConfig.packageVersion);
        if (cloudVersion > myVersion)
        {
            GameStateManager.Instance.ShowPop(true,"请下载最新版本!",()=> {
                string url = AppConst.PackageUrl + "/MyGame_v" + serverVersionConfig.packageVersion + ".rar";
                //www = new WWW(url);
                Application.OpenURL(url);
                GameStateManager.Instance.Quit();
            }, GameStateManager.Instance.Quit);
        }
        else
        {
            CheckAppVersionComplete(serverVersionConfig, streamVersionConfig, versionVo);
        }
    }

}

