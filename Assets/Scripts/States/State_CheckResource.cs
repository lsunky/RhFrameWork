using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RhFrameWork;
using RhFrameWork.StateMachine;
using UnityEngine;
using System.IO;

public class State_CheckResource : State
{
    public override void EnterState(Hashtable hash)
    {
        GameStateManager.Instance.GameStateChang(GameStates.CheckResource);
#if UNITY_EDITOR
        if (!AssetBundleManager.ResUpdateInEditor)
        {
            CheckResourceComplete(null,null,null,null);
            return;
        }
#endif
        VersionConfig serverVersionConfig = (VersionConfig)hash["serverVersionConfig"];
        FileUpdateVo versionVo              = (FileUpdateVo)hash["versionVo"];
        TaskManager.Instance.StartCoroutine(CheckFileListMd5(serverVersionConfig, versionVo));
    }
    
    public override void ExitState()
    {
        
    }

    private void CheckResourceComplete(VersionConfig serverVersionConfig,FileCheckResult checkResult, byte[] fileListBytes, FileUpdateVo versionVo)
    {
        Hashtable hash = new Hashtable();
        hash["checkResult"] = checkResult;
        hash["serverVersionConfig"] = serverVersionConfig;
        hash["fileListBytes"] = fileListBytes;
        hash["versionVo"] = versionVo;
        if (checkResult != null )//&& checkResult.downSize > 1000)//大于某个值了才提示
        {
            //弹窗，确认之后才能继续更新
            GameStateManager.Instance.ShowPop(true, string.Format("有{0}b资源更新，点击确定开始更新！", checkResult.downSize), () =>
            {
                this.stateMachine.SetState<State_DownResource>(hash);
            }, GameStateManager.Instance.Quit);
        }
        else
        {
            this.stateMachine.SetState<State_DownResource>(hash);
        }

    }

    IEnumerator CheckFileListMd5(VersionConfig serverVersionConfig, FileUpdateVo versionVo)
    {
        string content = File.ReadAllText(versionVo.PersistentPath);
        VersionConfig persistentVersionConfig = JsonUtility.FromJson<VersionConfig>(content);
        int persistentVersion = persistentVersionConfig.resVersion;
        int serverVersion = serverVersionConfig.resVersion;
        
        if (serverVersion >= persistentVersion)
        {
            bool versionConfigChange = false;
            FileUpdateVo fileListVo = new FileUpdateVo(AppConst.FileListName, versionVo.Url, versionVo.Random, string.Empty, 100);
            string fileListMd5 = Util.Md5file(fileListVo.PersistentPath);
            FileCheckResult checkResult = null;
            byte[] fileListBytes = null;
            if (!serverVersionConfig.fileListMd5.Equals(fileListMd5))
            {
                versionConfigChange = true;
                WWW www = new WWW(fileListVo.FileUrl);
                yield return www;
                if (string.IsNullOrEmpty(www.error))
                {
                    string[] fileList = www.text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    List<string> serverFileList = new List<string>(fileList);
                    fileList = File.ReadAllLines(fileListVo.PersistentPath);
                    List<string> persistentFileList = new List<string>(fileList);
                    checkResult = Util.GetChangList(persistentFileList, serverFileList);
                    fileListBytes = www.bytes;
                }
                else
                {
                    UDebug.LogError(string.Format("{0}down failed:{1}", fileListVo.FileUrl, www.error));
                    yield break;
                }
                www.Dispose();
                //下载云端list
            }
            else if(serverVersion > persistentVersion)//只是version改变了，实际资源没有变，或者上次资源下载好了，但是version写入没有成功
            {
                versionConfigChange = true;
            }

            VersionConfig toWriteVersionConfig = versionConfigChange ? serverVersionConfig : null;
            CheckResourceComplete(toWriteVersionConfig,  checkResult, fileListBytes,  versionVo);
        }
        else
        {
            if (GameStateManager.Instance.showGameStateLog)
                UDebug.Log("本资源比较超前，不做资源版本更新: " + "persistentVersion" + persistentVersion.ToString() + "    serverVersion:" + serverVersion.ToString());
            CheckResourceComplete(null, null, null, versionVo);
        }
        yield return new WaitForEndOfFrame();

    }

}