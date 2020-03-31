using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RhFrameWork
{
    public class PathHelper
    {
        static bool DebugMode = true;

        /// <summary>
        /// 运行时Application,非EditorUserBuildSettings
        /// </summary>
        public static string PlatformNameRunTime
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                        return "Android";
                    case RuntimePlatform.WindowsEditor:
                        return "Android";
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.OSXEditor:
                        return "iOS";
                    case RuntimePlatform.WindowsPlayer:
                        return "Windows";
                    case RuntimePlatform.OSXPlayer:
                        return "OSX";
                    // Add more build platform for your own.
                    // If you add more platforms, don't forget to add the same targets to GetPlatformFolderForAssetBundles(BuildTarget) function.
                    default:
                        return "Android";
                }
            }
        }

        /// <summary>
        /// 取得数据存放目录
        /// </summary>
        public static string PersistentPath
        {
            get
            {
                if (Application.isMobilePlatform)
                {
                    return Application.persistentDataPath + "/" ;
                }
                if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    return Application.streamingAssetsPath + "/";
                }
                if (DebugMode && Application.isEditor)
                {
                    return Application.persistentDataPath + "/";//Application.streamingAssetsPath + "/";
                }
                if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    int i = Application.dataPath.LastIndexOf('/');
                    return Application.dataPath.Substring(0, i + 1) ;
                }
                return "c:/" ;
            }
        }
        
        /// <summary>
        /// 应用程序内容路径
        /// </summary>
        public static string StreamingPath()
        {
            string path = string.Empty;
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    path = "jar:file://" + Application.dataPath + "!/assets/";
                    break;
                case RuntimePlatform.IPhonePlayer:
                    path = "file://" + Application.dataPath + "/Raw/";
                    break;
                default:
                    path = Application.dataPath + "/StreamingAssets/";
                    break;
            }
            return path;
        }

    }
}
