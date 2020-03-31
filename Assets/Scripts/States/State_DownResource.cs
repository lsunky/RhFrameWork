using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RhFrameWork;
using RhFrameWork.StateMachine;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class State_DownResource : State
{
    public override void EnterState(Hashtable hash)
    {
        GameStateManager.Instance.GameStateChang(GameStates.DownResource);
#if UNITY_EDITOR
        if (!AssetBundleManager.ResUpdateInEditor)
        {
            DownResourceComplete();
            return;
        }
#endif
        FileCheckResult checkResult = (FileCheckResult)hash["checkResult"] ;
        if (checkResult != null)
        {
            VersionConfig serverVersionConfig = (VersionConfig)hash["serverVersionConfig"];
            byte[] fileListBytes = (byte[])hash["fileListBytes"];
            FileUpdateVo versionVo = (FileUpdateVo)hash["versionVo"];
            List<FileUpdateVo> fileVoList = PrepareDownRes(serverVersionConfig, checkResult, fileListBytes, versionVo);
            TaskManager.Instance.StartCoroutine(StartDownRes(serverVersionConfig, fileListBytes, fileVoList, versionVo));
        }
        else
        {
            DownResourceComplete();
        }
    }

    public override void ExitState()
    {
        
    }

    private void DownResourceComplete()
    {
        UDebug.Log("DownResourceComplete");
        SceneManager.LoadScene("Game");
        TaskManager.Instance.StartCoroutine(Enter());
    }

    private IEnumerator Enter()
    {
        while (!UiMgr.Instance.Init())
        {
            yield return new WaitForEndOfFrame();
        }
        this.stateMachine.SetState<State_GameMain>(null);
    }

    private List<FileUpdateVo> PrepareDownRes(VersionConfig serverVersionConfig, FileCheckResult checkResult, byte[] fileListBytes, FileUpdateVo versionVo)
    {
        string url = versionVo.Url;
        string random = versionVo.Random;
        FileUpdateVo fileUpdateVo;
        string persistentMd5 = null;
        foreach (FileCheckInfo item in checkResult.DeleteList)
        {
            fileUpdateVo = new FileUpdateVo(item.name, url, random, item.md5, item.size);
            if (File.Exists(fileUpdateVo.PersistentPath))
            {
                File.Delete(fileUpdateVo.PersistentPath);
            }
        }

        List<FileUpdateVo> fileVoList = new List<FileUpdateVo>();
        for (int i = 0; i < checkResult.DownList.Count; i++)
        {
            FileCheckInfo item = checkResult.DownList[i];
            fileUpdateVo = new FileUpdateVo(item.name, url, random, item.md5, item.size);
            if (File.Exists(fileUpdateVo.PersistentPath))
            {
                persistentMd5 = Util.Md5file(fileUpdateVo.PersistentPath);
            }
            else
            {
                persistentMd5 = string.Empty;
            }

            if (persistentMd5 != fileUpdateVo.Md5)//需要下载
            {
                fileVoList.Add(fileUpdateVo);
            }
        }
        return fileVoList;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serverVersionConfig">如果不为空，则需要写入</param>
    /// <param name="checkResult">如果不为空，则需要删除文件或者下载文件</param>
    /// <param name="fileListBytes">下载之后的文本列表</param>
    /// <param name="url"></param>
    /// <param name="random"></param>
    /// <returns></returns>
    IEnumerator StartDownRes(VersionConfig serverVersionConfig,  byte[] fileListBytes, List<FileUpdateVo> fileVoList,FileUpdateVo versionVo)
    {
        FileUpdateVo fileUpdateVo;
        string url = versionVo.Url;
        string random = versionVo.Random;
        float allSize = Util.SumSize(fileVoList, fileVoList.Count);
        for (int i = 0; i < fileVoList.Count; i++)
        {
            GameStateManager.Instance.DownProgressHandle(GameStates.DownResource, fileVoList, i, allSize);
            fileUpdateVo = fileVoList[i];

            WWW www = new WWW(fileUpdateVo.FileUrl);
            yield return www;
            if (string.IsNullOrEmpty(www.error))
            {
                Util.WriteFile(fileUpdateVo.PersistentPath, www.bytes);
            }
            else
            {
                UDebug.LogError(string.Format("{0}down failed:{1}", fileUpdateVo.FileUrl, www.error));
                yield break;
            }
            www.Dispose();
        }

        if (fileListBytes != null)
        {
            fileUpdateVo = new FileUpdateVo(AppConst.FileListName, url, random, string.Empty, 0);
            Util.WriteFile(fileUpdateVo.PersistentPath, fileListBytes);
        }

        if (serverVersionConfig != null)
        {
            if (File.Exists(versionVo.PersistentPath))
            {
                File.Delete(versionVo.PersistentPath);
                string json = JsonUtility.ToJson(serverVersionConfig);
                File.WriteAllText(versionVo.PersistentPath, json);
            }
        }
        GameStateManager.Instance.DownProgressHandle(GameStates.DownResource, fileVoList, fileVoList.Count, allSize);

        yield return new WaitForEndOfFrame();
        DownResourceComplete();
    }
}
