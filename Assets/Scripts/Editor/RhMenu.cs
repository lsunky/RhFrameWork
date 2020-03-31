using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using RhFrameWork;
using System;

public class RhMenu
{
    public enum AssetBundleType
    {
        UI,
        Prefab,
        Icon,
        Config,
    }

    static string GetIconPackingTag(string path)
    {
        string str1 = Path.GetDirectoryName(path);
        string str2 = Path.GetFileName(str1);
        return str2.Split('.')[1];
    }
    
    /// <summary>
    /// 写入这里
    /// </summary>
    static PreAssetBundleConfig[] preAssetBundleConfig = new PreAssetBundleConfig[]
    {
        new PreAssetBundleConfig (AppConst.ResRoot+"UI/",".png.meta|.jpg.meta" ,new AssetBundleFFix { prefix="ui/", suffix="ab" }, BundleBatchType.FloderToOne ),
        new PreAssetBundleConfig (AppConst.ResRoot+"Prefab/",".prefab.meta" ,new AssetBundleFFix { prefix="prefab/", suffix="ab"}, BundleBatchType.None ),
        new PreAssetBundleConfig (AppConst.ResRoot+"Icon/",".png.meta" ,new AssetBundleFFix { prefix="icon/", suffix="ab"}, BundleBatchType.DIY,null, GetIconPackingTag),
        new PreAssetBundleConfig (AppConst.ResRoot+"Config/",".lua.meta" ,new AssetBundleFFix { prefix="config", suffix="ab"}, BundleBatchType.AllToOne ),
    };

    const string BundlesFloderName = "AssetBundles";

    const string kSimulationMode = "RhFrameWork/Res/Simulation Mode";

    [MenuItem(kSimulationMode)]
    public static void ToggleSimulationMode()
    {
        AssetBundleManager.SimulateAssetBundleInEditor = !AssetBundleManager.SimulateAssetBundleInEditor;
    }

    [MenuItem(kSimulationMode, true)]
    public static bool ToggleSimulationModeValidate()
    {
        Menu.SetChecked(kSimulationMode, AssetBundleManager.SimulateAssetBundleInEditor);
        return true;
    }

    const string kResUpdateMode = "RhFrameWork/Res/ResUpdate";

    [MenuItem(kResUpdateMode)]
    public static void ToggleResUpdateMode()
    {
        AssetBundleManager.ResUpdateInEditor = !AssetBundleManager.ResUpdateInEditor;
    }

    [MenuItem(kResUpdateMode, true)]
    public static bool ToggleResUpdateModeValidate()
    {
        Menu.SetChecked(kResUpdateMode, AssetBundleManager.ResUpdateInEditor);
        return true;
    }

    [MenuItem("RhFrameWork/0.写入bundle名", false, 0)]
    public static void AddAssetBundleNames()
    {
        AddAllToAssetBundle();
        Debug.Log("CreateBundle complete");
    }

    [MenuItem("RhFrameWork/1.CreateBundle _F3", false, 0)]
    public static void CreateBundle()
    {
        string outputPath = EditorAssetBundlesPlatformPath;
        Debug.Log("outputPath:" + outputPath);
        AssetBundleManifest mainfest = BuildAssetBundles(outputPath);
        string pLocalFilePath = Application.dataPath + "/" + AppConst.ResRoot + AppConst.FilePath2BundleListName;
        File.Copy(pLocalFilePath, EditorAssetBundlesPlatformPath + "/" + AppConst.FilePath2BundleListName, true);
        DeleteUnUsedAssets(outputPath, mainfest);
        Debug.Log("CreateBundle complete");
    }

    [MenuItem("RhFrameWork/2.GenerateFileList", false, 0)]
    static public void GenerateFileList()
    {
        Debug.Log("开始生成文件列表");
        string outputPath = ResPlatformPath;
        CreateFileList(outputPath);

        string versionConfigPath = AppConst.ResRoot + AppConst.ResVersionConfigFile;
        VersionConfig versionConfig;
        string versionContent;
        if (File.Exists(versionConfigPath))
        {
            versionContent = File.ReadAllText(versionConfigPath);
            versionConfig = JsonUtility.FromJson<VersionConfig>(versionContent);
            File.Delete(versionConfigPath);
        }
        else
        {
            versionConfig = new VersionConfig();
        }
        versionConfig.resVersion++;
        versionConfig.fileListMd5 = Util.Md5file(outputPath + "/" + AppConst.FileListName);
        versionContent = JsonUtility.ToJson(versionConfig);
        File.WriteAllText(versionConfigPath,versionContent);
        Debug.Log("生成文件列表完成");
    }

    [MenuItem("RhFrameWork/3.CreateChangList", false, 0)]
    static public void GenerateChangList()
    {
        string outputPath = ResPlatformPath;
        CreateChangList(outputPath + "lastFiles.txt", outputPath + AppConst.FileListName, AppConst.ResRoot +"changList" + GetPlatformName() + ".txt");
        Debug.Log("CreateChangList Complete");
    }

    static public string EditorAssetBundlesPlatformPath
    {
        get
        {
            return ResPlatformPath + AppConst.BundlesFloderName + "/";
        }
    }

    static public string ResPlatformPath
    {
        get
        {
            return AppConst.ResRoot + GetPlatformName() + "/";
        }
    }
    
    static string GetPlatformName()
    {
        return GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
    }
    public static string GetPlatformFolderForAssetBundles(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.Android:
                return "Android";
            case BuildTarget.iOS:
                return "iOS";
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return "Windows";
            case BuildTarget.StandaloneOSXIntel:
            case BuildTarget.StandaloneOSXIntel64:
                return "OSX";
            // Add more build targets for your own.
            // If you add more targets, don't forget to add the same platforms to GetPlatformFolderForAssetBundles(RuntimePlatform) function.
            default:
                return null;
        }
    }

    private static AssetBundleManifest BuildAssetBundles(string outputPath, bool bIsCompress = true)
    {
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        BuildAssetBundleOptions options = bIsCompress ? BuildAssetBundleOptions.DeterministicAssetBundle : BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.UncompressedAssetBundle;
        AssetBundleManifest mainifest = BuildPipeline.BuildAssetBundles(outputPath
            , options
            , EditorUserBuildSettings.activeBuildTarget);
        //刷新编辑器
        AssetDatabase.Refresh();
        return mainifest;
    }

    private static void DeleteUnUsedAssets(string outputPath, AssetBundleManifest mainifest)
    {
        string[] allAssets = mainifest.GetAllAssetBundles();
        string[] all = new string[2 * allAssets.Length + 3];
        for (int i = 0, length = allAssets.Length; i < length; i++)
        {
            all[i * 2] = allAssets[i];
            all[i * 2 + 1] = allAssets[i] + ".manifest"; ;
        }
        
        all[allAssets.Length * 2] = AppConst.BundlesFloderName;
        all[allAssets.Length * 2 + 1] = AppConst.BundlesFloderName + ".manifest";
        all[allAssets.Length * 2 + 2] = AppConst.FilePath2BundleListName;//留着这个文件

        DirectoryInfo d = new DirectoryInfo(outputPath);
        for (int i = 0, length = all.Length; i < length; i++)
        {
            all[i] = all[i].Replace("\\", "/");
            all[i] = d.FullName + all[i].Replace("/", "\\");
        }
        DeleteUnusedAssets(outputPath, all);
    }
    /// <summary>
    /// 删除path目录内所有不在列表里的文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="allAssets"></param>
    private static void DeleteUnusedAssets(string dir, string[] allAssets)
    {
        DirectoryInfo d = new DirectoryInfo(dir);
        FileSystemInfo[] fsinfos = d.GetFileSystemInfos();

        foreach (FileSystemInfo fsinfo in fsinfos)
        {
            if (fsinfo is DirectoryInfo)     //判断是否为文件夹  
            {
                DeleteUnusedAssets(fsinfo.FullName, allAssets);//递归调用  
            }
            else
            {
                int index = Array.IndexOf(allAssets, fsinfo.FullName);
                if (index == -1)
                    File.Delete(fsinfo.FullName);
            }
        }
        if (d.GetFileSystemInfos().Length == 0)
        {
            d.Delete();
        }


    }
    private static void CreateChangList(string lastFilesPath,string curFilesPath,string changListFilesPath)
    {
        string[] lastFiles  = File.ReadAllLines(lastFilesPath);
        string[] curFiles   = File.ReadAllLines(curFilesPath);
        FileCheckResult checkResult =  Util.GetChangList(new List<string>(lastFiles),new List<string>(curFiles));

        if (File.Exists(changListFilesPath))
        {
            File.Delete(changListFilesPath);
        }

        FileStream fs = new FileStream(changListFilesPath, FileMode.Create);
        StreamWriter sw = new StreamWriter(fs);
        FileCheckInfo fileCheckInfo;

        sw.WriteLine("本次热更需要下载量为:   "+ checkResult.downSize);
        if (checkResult.AddList.Count > 0 )
        {
            sw.WriteLine("新增列表:");
        }
        for (int i = 0, length = checkResult.AddList.Count; i < length; i++)
        {
            fileCheckInfo = checkResult.AddList[i];
            sw.WriteLine(fileCheckInfo.name+"|"+ fileCheckInfo.size);
        }

        if (checkResult.ChangeList.Count > 0)
        {
            sw.WriteLine("改变列表:");
        }
        for (int i = 0, length = checkResult.ChangeList.Count; i < length; i++)
        {
            fileCheckInfo = checkResult.ChangeList[i];
            sw.WriteLine(fileCheckInfo.name + "|" + fileCheckInfo.size);
        }

        if (checkResult.DeleteList.Count > 0)
        {
            sw.WriteLine("删除列表:");
        }
        for (int i = 0, length = checkResult.DeleteList.Count; i < length; i++)
        {
            fileCheckInfo = checkResult.DeleteList[i];
            sw.WriteLine(fileCheckInfo.name + "|" + fileCheckInfo.size);
        }
        //清空缓冲区
        sw.Flush();
        //关闭流
        sw.Close();
        fs.Close();

    }

    public static void CreateFileList(string path)
    {
        string outfile = path + "/"+ AppConst.FileListName;
        if (File.Exists(outfile))
        {
            File.Copy(outfile, path + "/lastFiles.txt", true);
            File.Delete(outfile);
        }

        List<KeyValuePair<string, string>> fileMd5List = new List<KeyValuePair<string, string>>();
        DirectorMd5(path, fileMd5List);

        FileStream fs = new FileStream(outfile, FileMode.Create);
        StreamWriter sw = new StreamWriter(fs);
        string handlePath = path.Replace("\\", "/");
        for (int i = 0, length = fileMd5List.Count; i < length; i++)
        {
            string tempPath = fileMd5List[i].Key.Replace("\\", "/");
            if (!tempPath.Contains("lastFiles"))
            {
                int index = tempPath.IndexOf(handlePath);
                tempPath = tempPath.Substring(index + handlePath.Length);
                //开始写入
                sw.WriteLine(tempPath + "|" + fileMd5List[i].Value);
            }
        }
        //清空缓冲区
        sw.Flush();
        //关闭流
        sw.Close();
        fs.Close();
    }

    static void DirectorMd5(string dir, List<KeyValuePair<string, string>> fileMd5List)
    {
        DirectoryInfo d = new DirectoryInfo(dir);
        FileSystemInfo[] fsinfos = d.GetFileSystemInfos();
        foreach (FileSystemInfo fsinfo in fsinfos)
        {
            if (fsinfo is DirectoryInfo)     //判断是否为文件夹  
            {
                DirectorMd5(fsinfo.FullName, fileMd5List);//递归调用  
            }
            else
            {
                FileInfo fileInfo = (FileInfo)fsinfo;
                float size = Mathf.Ceil(fileInfo.Length / 1000f);
                string fullName = fsinfo.FullName;
                if (fullName.EndsWith(".manifest"))
                    continue;
                string md5 = size + "|" + Util.Md5file(fsinfo.FullName);
                fileMd5List.Add(new KeyValuePair<string, string>(fullName, md5));
            }

        }
    }

    static public void AddAllToAssetBundle(string ChoosefilePath = null)
    {
        Debug.Log("start write ab name");
        List<string> preImport = new List<string>();
        List<KeyValuePair<string, string>> listFilePath2Bundle = new List<KeyValuePair<string, string>>();
        foreach (PreAssetBundleConfig assetBundleConfig in preAssetBundleConfig)
        {
            //只打某个文件夹的bundle
            if (ChoosefilePath != null)
            {
                if (assetBundleConfig.path.IndexOf(ChoosefilePath) != 0 &&
                    ChoosefilePath.IndexOf(assetBundleConfig.path) != 0)
                {
                    continue;
                }
            }

            string pathsss = Application.dataPath + "/" + assetBundleConfig.path;
            string removePathStr = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            foreach (var keyword in assetBundleConfig.keyword)
            {
                var files = Directory.GetFiles(pathsss, "*" + keyword, assetBundleConfig.searchOption);
                foreach (var file in files)
                {
                    string name = file;
                    name = name.Replace("\\", "/");
                    string tempFileName = name.Substring(name.LastIndexOf('/') + 1);
                    if (!string.IsNullOrEmpty(assetBundleConfig.pattern) && !System.Text.RegularExpressions.Regex.IsMatch(tempFileName, assetBundleConfig.pattern))
                    {
                        continue;
                    }
                    switch (assetBundleConfig.batchType)
                    {
                        case BundleBatchType.AllToOne:
                            //name = assetBundleConfig.ffix.prefix.Substring(0, assetBundleConfig.ffix.prefix.LastIndexOf("/"));
                            //name = assetBundleConfig.ffix.prefix.Substring(assetBundleConfig.ffix.prefix.LastIndexOf("/") + 1);
                            name = string.Empty;
                            break;
                        case BundleBatchType.FloderToOne:
                            name = name.Substring(0, name.LastIndexOf("/"));
                            name = name.Substring(name.LastIndexOf("/") + 1);
                            break;
                        case BundleBatchType.DIY:
                            string packingTag = assetBundleConfig.diyBundleNameRule(name);
                            if (string.IsNullOrEmpty(packingTag))
                                continue;
                            name = packingTag;
                            break;
                        default:
                            name = name.Substring(name.LastIndexOf('/') + 1);
                            name = name.Replace(keyword, ""); //去掉扩展名(即关键字)
                            break;
                    }
                    string pre = file.Replace(".meta", "");
                    pre = pre.Substring(pre.IndexOf("Assets/"+ AppConst.ResRoot) +7+ AppConst.ResRoot.Length);
                    string bundleName;
                    if (DoSetAssetBundleName(name, assetBundleConfig.ffix.prefix, assetBundleConfig.ffix.suffix, file, out bundleName, true))
                    {
                        preImport.Add(pre);
                    }
                    pre = pre.Replace("\\", "/");
                    pre = pre.Remove(pre.LastIndexOf('.'));
                    listFilePath2Bundle.Add(new KeyValuePair<string, string>(pre, bundleName+"."+ assetBundleConfig.ffix.suffix));
                }
            }
        }

        AssetDatabase.SaveAssets();
        foreach (string path in preImport)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.Default);
        }
        
        string outfile = Application.dataPath + "/"+ AppConst.ResRoot + AppConst.FilePath2BundleListName;
        if (File.Exists(outfile)) File.Delete(outfile);
        FileStream fs = new FileStream(outfile, FileMode.Create);
        StreamWriter sw = new StreamWriter(fs);
        for (int i = 0, length = listFilePath2Bundle.Count; i < length; i++)
        {
            sw.WriteLine(listFilePath2Bundle[i].Key + "|" + listFilePath2Bundle[i].Value);
        }
        //清空缓冲区
        sw.Flush();
        //关闭流
        sw.Close();
        fs.Close();
        Debug.Log("end write ab name"+ outfile);
    }

    static bool DoSetAssetBundleName(string name, string prefix, string suffix, string path, out string bundleName, bool addBundle = true)
    {
        path = path.Replace("\\", "/");

        StreamReader fs = new StreamReader(path);
        List<string> ret = new List<string>();
        string line;

        name = name.Replace(" ", "");
        if (!addBundle)
        {
            prefix = string.Empty;
            suffix = string.Empty;
        }

        bundleName = prefix.ToLower() + name.ToLower();
        string assetBundleName = "  assetBundleName: " + bundleName;
        string assetBundleVariant = "  assetBundleVariant: " + (!string.IsNullOrEmpty(suffix) ? suffix.ToLower() : string.Empty);
        int ifSuccess = 0;

        while ((line = fs.ReadLine()) != null)
        {
            line = line.Replace("\n", "");
            if (line.Equals(assetBundleName))
                ifSuccess++;
            if (line.Equals(assetBundleVariant))
                ifSuccess++;
            if (ifSuccess == 2)
            {
                fs.Close();
                return false;
            }

            if (line.IndexOf("assetBundleName:") != -1)
                ret.Add(assetBundleName);
            else if (line.IndexOf("assetBundleVariant:") != -1)
                ret.Add(assetBundleVariant);
            else
                ret.Add(line);
        }
        fs.Close();

        File.Delete(path);

        StreamWriter writer = new StreamWriter(path + ".tmp");
        foreach (var each in ret)
        {
            writer.WriteLine(each);
        }
        writer.Close();

        File.Copy(path + ".tmp", path);
        File.Delete(path + ".tmp");
        return true;
    }


}
