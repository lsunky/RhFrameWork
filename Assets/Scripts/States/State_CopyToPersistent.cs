using RhFrameWork.StateMachine;
using System.Collections;
using System.Collections.Generic;
using RhFrameWork;
using UnityEngine;
using System.IO;
using System;

/// <summary>
/// 解压缩
/// </summary>
public class State_CopyToPersistent : State
{
    VersionConfig serverVersionConfig;
    FileUpdateVo versionVo;
    public override void EnterState(Hashtable hash)
    {
        GameStateManager.Instance.GameStateChang(GameStates.CopyToPersistent);
#if UNITY_EDITOR
        if (!AssetBundleManager.ResUpdateInEditor)
        {
            UnzipStreamingComplete(null);
            return;
        }
#endif
        this.serverVersionConfig = (VersionConfig)hash["serverVersionConfig"];
        versionVo = (FileUpdateVo)hash["versionVo"];
        VersionConfig streamVersionConfig = (VersionConfig)hash["streamVersionConfig"];
        TaskManager.Instance.StartCoroutine(LoadResVersion(versionVo, streamVersionConfig));
    }

    public override void ExitState()
    {
        
    }

    private void UnzipStreamingComplete(VersionConfig persistentAssetsConfig )
    {
        if (GameStateManager.Instance.showGameStateLog)
            UDebug.Log("UnzipStreamingComplete");
        Hashtable hash = new Hashtable();
        hash["serverVersionConfig"] = serverVersionConfig;
        hash["versionVo"] = versionVo;
        this.stateMachine.SetState<State_CheckResource>(hash);
    }

    IEnumerator LoadResVersion(FileUpdateVo vo, VersionConfig streamVersionConfig)
    {
        bool isExists = File.Exists(vo.PersistentPath);
        bool needCopy = !isExists;
        VersionConfig persistentAssetsConfig = null;
        if (isExists)
        {
            string persistentStr = File.ReadAllText(vo.PersistentPath);
            if (GameStateManager.Instance.showGameStateLog)
                UDebug.Log("persistentStr " + persistentStr);
            persistentAssetsConfig = JsonUtility.FromJson<VersionConfig>(persistentStr);
            if (persistentAssetsConfig.packageVersion != streamVersionConfig.packageVersion)
                needCopy = true;
        }

        if (needCopy)
        {
            string resRoot = PathHelper.PersistentPath + "/" + AppConst.ResRoot;

            if (Directory.Exists(resRoot))
                Directory.Delete(resRoot, true);
        }

        if (!needCopy)
        {
            if (GameStateManager.Instance.showGameStateLog)
                UDebug.Log("不需要解压文件");
            UnzipStreamingComplete(persistentAssetsConfig);
            yield break; ;
        }
        else
        {
            TaskManager.Instance.StartCoroutine(OnExtractResource(vo.Url, vo.Random, streamVersionConfig));
        }
    }

    /// <summary>
    /// 解包逻辑，装完包第一次运行时候做
    /// 现在用的比较笨的方法，真实项目可以用多线程
    /// </summary>
    /// <returns></returns>
    IEnumerator OnExtractResource(string url,string random, VersionConfig streamVersionConfig)
    {
        string message = "正在解包文件:>files.txt";
        string tempStr;
        if (GameStateManager.Instance.showGameStateLog)
            UDebug.Log(message);

        List<FileUpdateVo> fileVoList = new List<FileUpdateVo>();
        //files.txt
        FileUpdateVo fileUpdateVo = new FileUpdateVo(AppConst.FileListName, url, random, string.Empty,100 );
        if (Application.isMobilePlatform) //读取files.txt
        {
            WWW www = new WWW(fileUpdateVo.StreamingPath);
            yield return www;
            tempStr = www.text;
            www.Dispose();
        }
        else
        {
            tempStr = File.ReadAllText(fileUpdateVo.StreamingPath);
        }
        string[] fileList = tempStr.Split(new[] { Environment.NewLine },StringSplitOptions.None);
        string[] strArr;
        foreach (var file in fileList)
        {
            string fileStr = file.Trim();
            if (!string.IsNullOrEmpty(fileStr))
            {
                strArr = fileStr.Split('|');
                fileUpdateVo = new FileUpdateVo( strArr[0], url, random, strArr[2], float.Parse(strArr[1]));
                fileVoList.Add(fileUpdateVo);
            }
        }
        fileUpdateVo = new FileUpdateVo(AppConst.FileListName, url, random, string.Empty, 100);
        fileVoList.Add(fileUpdateVo);

        fileUpdateVo = new FileUpdateVo(AppConst.ResVersionConfigFile, url, random, string.Empty, 100,false);
        fileVoList.Add(fileUpdateVo);

        float allSize = Util.SumSize(fileVoList, fileVoList.Count);
        
        for (int i = 0; i < fileVoList.Count; i++)
        {
            GameStateManager.Instance.DownProgressHandle(GameStates.CopyToPersistent,fileVoList, i, allSize);
            fileUpdateVo = fileVoList[i];
            String strPath = Path.GetDirectoryName(fileUpdateVo.PersistentPath);
            if (!Directory.Exists(strPath))
            {
                Directory.CreateDirectory(strPath);
            }

            if (Application.isMobilePlatform)
            {
                WWW www = new WWW(fileUpdateVo.StreamingPath);
                yield return www;
                if (www.isDone)
                {
                    File.WriteAllBytes(fileUpdateVo.PersistentPath, www.bytes);
                    www.Dispose();
                }
                if (GameStateManager.Instance.showGameStateLog)
                    UDebug.Log(fileUpdateVo.FileName + "  文件写入成功");
            }
            else
            {
                File.Copy(fileUpdateVo.StreamingPath, fileUpdateVo.PersistentPath, true);
            }
        }
        
        GameStateManager.Instance.DownProgressHandle(GameStates.GameMain,fileVoList, fileVoList.Count, allSize);


        message = "解包完成!!!";
        if (GameStateManager.Instance.showGameStateLog)
            UDebug.Log(message);

        yield return new WaitForEndOfFrame();
        message = string.Empty;

        UnzipStreamingComplete(streamVersionConfig);
    }

}
