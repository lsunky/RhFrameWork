using UnityEngine;
#if UNITY_EDITOR	
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using System.IO;

/*
 *  原文档 链接：https://unity3d.com/cn/learn/tutorials/topics/scripting/assetbundles-and-assetbundle-manager?playlist=17117
 	In this demo, we demonstrate:
	1.	Automatic asset bundle dependency resolving & loading.
		It shows how to use the manifest assetbundle like how to get the dependencies etc.
	2.	Automatic unloading of asset bundles (When an asset bundle or a dependency thereof is no longer needed, the asset bundle is unloaded)
	3.	Editor simulation. A bool defines if we load asset bundles from the project or are actually using asset bundles(doesn't work with assetbundle variants for now.)
		With this, you can player in editor mode without actually building the assetBundles.
	4.	Optional setup where to download all asset bundles
	5.	Build pipeline build postprocessor, integration so that building a player builds the asset bundles and puts them into the player data (Default implmenetation for loading assetbundles from disk on any platform)
	6.	Use WWW.LoadFromCacheOrDownload and feed 128 bit hash to it when downloading via web
		You can get the hash from the manifest assetbundle.
	7.	AssetBundle variants. A prioritized list of variants that should be used if the asset bundle with that variant exists, first variant in the list is the most preferred etc.
*/


// Loaded assetBundle contains the references count which can be used to unload dependent assetBundles automatically.
namespace RhFrameWork
{
    public class LoadedAssetBundle
    {
        public AssetBundle m_AssetBundle;
        public int m_ReferencedCount;

        public LoadedAssetBundle(AssetBundle assetBundle)
        {
            m_AssetBundle = assetBundle;
            m_ReferencedCount = 1;
        }
    }

    // Class takes care of loading assetBundle and its dependencies automatically, loading variants automatically.
    public class AssetBundleManager : UnitySingleton<AssetBundleManager>
    {
        public enum LogMode { All, JustErrors };
        public enum LogType { Info, Warning, Error };

        LogMode m_LogMode = LogMode.All;
        string m_BaseDownloadingURL = string.Empty;
        AssetBundleManifest m_AssetBundleManifest = null;
#if UNITY_EDITOR
        static int m_SimulateAssetBundleInEditor = -1;
        const string kSimulateAssetBundles = "SimulateAssetBundles";

        static int m_ResUpdateInEditor = -1;
        const string kResUpdate = "ResUpdate";
#endif

        Dictionary<string, LoadedAssetBundle> m_LoadedAssetBundles = new Dictionary<string, LoadedAssetBundle>();
        Dictionary<string, WWW> m_DownloadingWWWs = new Dictionary<string, WWW>();
        Dictionary<string, string> m_DownloadingErrors = new Dictionary<string, string>();
        List<AssetBundleLoadOperation> m_InProgressOperations = new List<AssetBundleLoadOperation>();
        Dictionary<string, string[]> m_Dependencies = new Dictionary<string, string[]>();

        List<AssetBundleLoadOperation> m_Requesting = new List<AssetBundleLoadOperation>();

        // AssetBundleManifest object which can be used to load the dependecies and check suitable assetBundle variants.
        public AssetBundleManifest AssetBundleManifestObject
        {
            set { m_AssetBundleManifest = value; }
        }

        private void Log(LogType logType, string text)
        {
            return;
            if (logType == LogType.Error)
                Debug.LogError("[AssetBundleManager] " + text);
            else if (m_LogMode == LogMode.All)
                UDebug.Log("[AssetBundleManager] " + text);
        }

#if UNITY_EDITOR
        // Flag to indicate if we want to simulate assetBundles in Editor without building them actually.
        public static bool SimulateAssetBundleInEditor
        {
            get
            {
                if (m_SimulateAssetBundleInEditor == -1)
                    m_SimulateAssetBundleInEditor = EditorPrefs.GetBool(kSimulateAssetBundles, false) ? 1 : 0;

                return m_SimulateAssetBundleInEditor != 0;
            }
            set
            {
                int newValue = value ? 1 : 0;
                if (newValue != m_SimulateAssetBundleInEditor)
                {
                    m_SimulateAssetBundleInEditor = newValue;
                    EditorPrefs.SetBool(kSimulateAssetBundles, value);
                }
            }
        }

        public static bool ResUpdateInEditor
        {
            get
            {
                if (m_ResUpdateInEditor == -1)
                    m_ResUpdateInEditor = EditorPrefs.GetBool(kResUpdate, false) ? 1 : 0;

                return m_ResUpdateInEditor != 0;
            }
            set
            {
                int newValue = value ? 1 : 0;
                if (newValue != m_ResUpdateInEditor)
                {
                    m_ResUpdateInEditor = newValue;
                    EditorPrefs.SetBool(kResUpdate, value);
                }
            }
        }
#endif

        public string GetPersistentRootPath()
        {
#if UNITY_EDITOR
            if (!ResUpdateInEditor)
                return System.Environment.CurrentDirectory.Replace("\\", "/") + "/ResRoot";
#endif
            return PathHelper.PersistentPath + "/ResRoot";
        }

        void SetSourceAssetBundleDirectory()
        {
            m_BaseDownloadingURL = GetPersistentRootPath() + "/" + PathHelper.PlatformNameRunTime + "/"+AppConst.BundlesFloderName+"/";
        }
        

        // Get loaded AssetBundle, only return vaild object when all the dependencies are downloaded successfully.
        public LoadedAssetBundle GetLoadedAssetBundle(string assetBundleName, out string error)
        {
            if (m_DownloadingErrors.TryGetValue(assetBundleName, out error))
                return null;

            LoadedAssetBundle bundle = null;
            m_LoadedAssetBundles.TryGetValue(assetBundleName, out bundle);
            if (bundle == null)
                return null;

            // No dependencies are recorded, only the bundle itself is required.
            string[] dependencies = null;
            if (!m_Dependencies.TryGetValue(assetBundleName, out dependencies))
                return bundle;

            // Make sure all dependencies are loaded
            foreach (var dependency in dependencies)
            {
                if (m_DownloadingErrors.TryGetValue(assetBundleName, out error))
                    return bundle;

                // Wait all the dependent assetBundles being loaded.
                LoadedAssetBundle dependentBundle;
                m_LoadedAssetBundles.TryGetValue(dependency, out dependentBundle);
                if (dependentBundle == null)
                    return null;
            }

            return bundle;
        }

        /// <summary>
        /// 初始化bundle路径和Manifest
        /// </summary>
        /// <param name="bundleDirectory"></param>
        /// <param name="pCallBack"></param>
        /// <returns></returns>
        public void InitializeAsy(System.Action<AssetBundleManifest> pCallBack)
        {
            this.SetSourceAssetBundleDirectory();
            InitializeManifestAsy("AssetBundles", pCallBack);
        }

        bool hasInitialize = false;
        /// <summary>
        /// 初始化bundle路径和Manifest
        /// </summary>
        /// <param name="bundleDirectory"></param>
        /// <param name="pCallBack"></param>
        /// <returns></returns>
        public void Initialize()
        {
            if (hasInitialize)
                return;
            string[] list = null;
            
            hasInitialize = true;
            this.SetSourceAssetBundleDirectory();

            string filePath2BundleListPath = null;
#if UNITY_EDITOR
            // If we're in Editor simulation mode, we don't need the manifest assetBundle.
            if (SimulateAssetBundleInEditor)
            {
                filePath2BundleListPath = Application.dataPath + "/" + AppConst.ResRoot + AppConst.FilePath2BundleListName;
                list = File.ReadAllLines(filePath2BundleListPath);
                GameAssetStruct.Init(list);
                return;
            }
#endif
            filePath2BundleListPath = GetBundleFullUrl(AppConst.FilePath2BundleListName);
            list = File.ReadAllLines(filePath2BundleListPath);
            GameAssetStruct.Init(list);

            string bundleName = AppConst.BundlesFloderName;
            string path = GetBundleFullUrl(bundleName);
            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            m_AssetBundleManifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            if (bundle != null)
            {
                AddResourceLoaded(bundleName, bundle);
            }
        }
        // Load AssetBundleManifest.
        AssetBundleLoadManifestOperation InitializeManifestAsy(string manifestAssetBundleName, System.Action<AssetBundleManifest> pCallBack)
        {
#if UNITY_EDITOR
            Log(LogType.Info, "Simulation Mode: " + (SimulateAssetBundleInEditor ? "Enabled" : "Disabled"));
#endif


#if UNITY_EDITOR
            // If we're in Editor simulation mode, we don't need the manifest assetBundle.
            if (SimulateAssetBundleInEditor)
            {
                pCallBack(null);
                return null;
            }
#endif

            LoadAssetBundleAsy(manifestAssetBundleName, true);
            var operation = new AssetBundleLoadManifestOperation(manifestAssetBundleName, "AssetBundleManifest", typeof(AssetBundleManifest), ab =>
            {
                pCallBack(ab.GetAsset<AssetBundleManifest>());
            });
            m_InProgressOperations.Add(operation);
            return operation;
        }

        // Load AssetBundle and its dependencies.
        protected void LoadAssetBundleAsy(string assetBundleName, bool isLoadingAssetBundleManifest = false)
        {
            Log(LogType.Info, "Loading Asset Bundle " + (isLoadingAssetBundleManifest ? "Manifest: " : ": ") + assetBundleName);

#if UNITY_EDITOR
            // If we're in Editor simulation mode, we don't have to really load the assetBundle and its dependencies.
            if (SimulateAssetBundleInEditor)
                return;
#endif


            // Check if the assetBundle has already been processed.
            bool isAlreadyProcessed = LoadAssetBundleInternal(assetBundleName, isLoadingAssetBundleManifest);

            // Load dependencies.
            if (!isAlreadyProcessed && !isLoadingAssetBundleManifest)
                LoadDependenciesAsy( assetBundleName);
        }


        /// <summary>
        /// 拼成一个url 链接，assetBundleName含后缀
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <returns></returns>
        protected string GetBundleFullUrl( string assetBundleName, bool addJavaPath = false)
        {
            string baseUrl =   m_BaseDownloadingURL;
            baseUrl = addJavaPath ? "file://" + baseUrl : baseUrl;
            return baseUrl + assetBundleName;
        }
        // Where we actuall call WWW to download the assetBundle.
        protected bool LoadAssetBundleInternal( string assetBundleName, bool isLoadingAssetBundleManifest)
        {
            // Already loaded.
            LoadedAssetBundle bundle = null;
            m_LoadedAssetBundles.TryGetValue(assetBundleName, out bundle);
            if (bundle != null)
            {
                bundle.m_ReferencedCount++;
                return true;
            }

            // @TODO: Do we need to consider the referenced count of WWWs?
            // In the demo, we never have duplicate WWWs as we wait LoadAssetAsync()/LoadLevelAsync() to be finished before calling another LoadAssetAsync()/LoadLevelAsync().
            // But in the real case, users can call LoadAssetAsync()/LoadLevelAsync() several times then wait them to be finished which might have duplicate WWWs.
            if (m_DownloadingWWWs.ContainsKey(assetBundleName))
                return true;

            WWW download = null;
            string url = GetBundleFullUrl( assetBundleName, true);
            AssetBundleManifest manifest =    m_AssetBundleManifest;
            // For manifest assetbundle, always download it as we don't have hash for it.
            if (isLoadingAssetBundleManifest)
                download = new WWW(url);
            else
                download = WWW.LoadFromCacheOrDownload(url, manifest.GetAssetBundleHash(assetBundleName), 0);

            m_DownloadingWWWs.Add(assetBundleName, download);
            download.Dispose();
            return false;
        }

        // Where we get all the dependencies and load them all.
        protected void LoadDependenciesAsy(string assetBundleName)
        {
            AssetBundleManifest manifest = m_AssetBundleManifest;
            if (manifest == null)
            {
                Debug.LogError("Please initialize AssetBundleManifest by calling AssetBundleManager.Initialize()");
                return;
            }

            // Get dependecies from the AssetBundleManifest object..
            string[] dependencies = manifest.GetAllDependencies(assetBundleName);
            if (dependencies.Length == 0)
                return;

            // Record and load all dependencies.
            m_Dependencies.Add(assetBundleName, dependencies);
            for (int i = 0; i < dependencies.Length; i++)
                LoadAssetBundleInternal( dependencies[i], false);
        }

        // Unload assetbundle and its dependencies.
        public void UnloadAssetBundle(GameAssetStruct gas)
        {
#if UNITY_EDITOR
            // If we're in Editor simulation mode, we don't have to load the manifest assetBundle.
            if (SimulateAssetBundleInEditor)
                return;
#endif

            //UDebug.Log(m_LoadedAssetBundles.Count + " assetbundle(s) in memory before unloading " + assetBundleName);

            UnloadAssetBundleInternal(gas.BundleFullName);
            UnloadDependencies(gas.BundleFullName);
            //UDebug.Log(m_LoadedAssetBundles.Count + " assetbundle(s) in memory after unloading " + assetBundleName);
        }

        protected void UnloadDependencies(string assetBundleName)
        {
            string[] dependencies = null;
            if (!m_Dependencies.TryGetValue(assetBundleName, out dependencies))
                return;

            // Loop dependencies.
            foreach (var dependency in dependencies)
            {
                UnloadAssetBundleInternal(dependency);
            }

            m_Dependencies.Remove(assetBundleName);
        }

        protected void UnloadAssetBundleInternal(string assetBundleName)
        {
            string error;
            LoadedAssetBundle bundle = GetLoadedAssetBundle(assetBundleName, out error);
            if (bundle == null)
                return;

            if (--bundle.m_ReferencedCount == 0)
            {
                bundle.m_AssetBundle.Unload(false);
                m_LoadedAssetBundles.Remove(assetBundleName);

                Log(LogType.Info, assetBundleName + " has been unloaded successfully");
            }
        }

        void Update()
        {
            // Collect all the finished WWWs.
            var keysToRemove = new List<string>();
            foreach (var keyValue in m_DownloadingWWWs)
            {
                WWW download = keyValue.Value;

                // If downloading fails.
                if (!string.IsNullOrEmpty(download.error))
                {
                    if (!m_DownloadingErrors.ContainsKey(keyValue.Key))
                    {
                        string errorStr = string.Format("Failed downloading bundle {0} from {1}: {2}", keyValue.Key, download.url, download.error);
                        m_DownloadingErrors.Add(keyValue.Key, errorStr);
                        Debug.LogError(errorStr);
                    }

                    keysToRemove.Add(keyValue.Key);
                    continue;
                }

                // If downloading succeeds.
                if (download.isDone)
                {
                    AssetBundle bundle = download.assetBundle;
                    if (bundle == null)
                    {
                        m_DownloadingErrors.Add(keyValue.Key, string.Format("{0} is not a valid asset bundle.", keyValue.Key));
                        keysToRemove.Add(keyValue.Key);
                        continue;
                    }

                    //UDebug.Log("Downloading " + keyValue.Key + " is done at frame " + Time.frameCount);
                    m_LoadedAssetBundles.Add(keyValue.Key, new LoadedAssetBundle(download.assetBundle));
                    keysToRemove.Add(keyValue.Key);
                }
            }

            // Remove the finished WWWs.
            foreach (var key in keysToRemove)
            {
                WWW download = m_DownloadingWWWs[key];
                m_DownloadingWWWs.Remove(key);
                download.Dispose();
            }

            // Update all in progress operations
            for (int i = 0; i < m_InProgressOperations.Count;)
            {
                if (!m_InProgressOperations[i].Update())
                {
                    m_Requesting.Add(m_InProgressOperations[i]);
                    m_InProgressOperations.RemoveAt(i);
                }
                else
                    i++;
            }

            AssetBundleLoadOperation temp;
            // Update all in progress operations
            for (int i = 0; i < m_Requesting.Count;)
            {
                if (m_Requesting[i].IsDone())
                {
                    temp = m_Requesting[i];
                    m_Requesting.RemoveAt(i);
                    temp.OnDone();
                }
                else
                    i++;
            }

        }

        /// <summary>
        /// 同步加载某种类型资源
        /// </summary>
        public T LoadAsset<T>( string assetPath ) where T : UnityEngine.Object
        {
            GameAssetStruct gas = new GameAssetStruct(assetPath);
            return LoadAsset<T>(gas);
        }
        public GameObject LoadAssetAndInstantiation( string assetPath )
        {
            GameObject prefab = LoadAsset<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogError("assetbundle with name " + assetPath + " not found");
                return null;
            }
            GameObject instanted = GameObject.Instantiate(prefab);
            return instanted;

        }
        /// <summary>
        /// 同步加载某种类型资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gas"></param>
        /// <returns></returns>
        public T LoadAsset<T>(GameAssetStruct gas) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            if (SimulateAssetBundleInEditor )
            {
                string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(gas.BundleFullName, gas.AssetName);
                if (assetPaths.Length == 0)
                {
                    Log(LogType.Error, "There is no asset with name \"" + gas.AssetName + "\" in " + gas.BundleFullName);
                    return null;
                }

                // @TODO: Now we only get the main object from the first asset. Should consider type also.
                //Object target = AssetDatabase.LoadMainAssetAtPath(assetPaths[0]);
                T t = AssetDatabase.LoadAssetAtPath<T>(assetPaths[0]);
                return t;

                //下面的写法有bug，GetAsset里强制类型转换，不对
                //AssetBundleLoadAssetOperationSimulation operation = new AssetBundleLoadAssetOperationSimulation(target, null);
                //return operation.GetAsset<T>();
            }
            else
#endif
            {
                AssetBundle bundle = LoadOrGetAssetsBundle(gas);
                if (bundle != null)
                {
                    T go = bundle.LoadAsset<T>(gas.AssetName);
                    if (go == null)
                    {
                        Debug.LogError("go == null gas.BundleFullName:" + gas.BundleFullName + "  gas.AssetName:" + gas.AssetName);
                    }
                    return go;
                }
                //CommonData.tempLog += "bundle == null:   ";
                //CommonData.tempLog += gas.AssetName;
                //CommonData.tempLog += gas.BundleFullName;
                return null;
            }
        }

        public T[] LoadAllAssets<T>(GameAssetStruct gas) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            if (SimulateAssetBundleInEditor)
            {
                string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(gas.BundleFullName);
                T[] result = new T[assetPaths.Length];
                if (assetPaths.Length == 0)
                {
                    Log(LogType.Error, "There is no asset with name \"" + gas.AssetName + "\" in " + gas.BundleFullName);
                    return null;
                }
                for (int i = 0, length = assetPaths.Length; i < length; i++)
                {
                    // @TODO: Now we only get the main object from the first asset. Should consider type also.
                    Object target = AssetDatabase.LoadMainAssetAtPath(assetPaths[i]);
                    AssetBundleLoadAssetOperationSimulation operation = new AssetBundleLoadAssetOperationSimulation(target, null);
                    result[i] = operation.GetAsset<T>();
                }
                return result;
            }
            else
#endif
            {
                AssetBundle bundle = LoadOrGetAssetsBundle(gas);
                if (bundle != null)
                {
                    return bundle.LoadAllAssets<T>();
                }
                return null;
            }
        }

        private AssetBundle LoadOrGetAssetsBundle(GameAssetStruct gas)
        {
            if (string.IsNullOrEmpty(gas.AssetName))
            {
                Debug.LogError("gas.BundleFullName:" + gas.BundleFullName + "    gas.AssetName:" + gas.AssetName);
                return null;
            }
            AssetBundle bundle;
            LoadedAssetBundle loadedBundle = null;
            if (m_LoadedAssetBundles.TryGetValue(gas.BundleFullName, out loadedBundle))
            {
                bundle = loadedBundle.m_AssetBundle;
            }
            else
            {
                bundle = LoadAssetsBundle(gas.BundleFullName);
            }

            return bundle;
        }

        private AssetBundle LoadAssetsBundle(string bundleFullName)
        {
            string path;
            AssetBundleManifest bundleManifest =  m_AssetBundleManifest;
            string[] dependsFile = bundleManifest.GetAllDependencies(bundleFullName);
            if (dependsFile.Length > 0)
            {
                for (int i = 0; i < dependsFile.Length; i++)
                {
                    path = GetBundleFullUrl(dependsFile[i]);

                    if (!m_LoadedAssetBundles.ContainsKey(dependsFile[i]))
                    {
                        AddResourceLoaded(dependsFile[i], AssetBundle.LoadFromFile(path));
                    }
                    else
                    {
                        m_LoadedAssetBundles[dependsFile[i]].m_ReferencedCount++;
                    }
                }
            }

            path = GetBundleFullUrl(bundleFullName);
            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            if (bundle != null)
            {
                AddResourceLoaded(bundleFullName, bundle);
            }
            return bundle;
        }


        void AddResourceLoaded(string strBundleName, AssetBundle bundle)
        {
            if (string.IsNullOrEmpty(strBundleName) || bundle == null)
            {
                Debug.LogError("AddResourceLoaded Failed BundleName:" + strBundleName);
                return;
            }

            if (m_LoadedAssetBundles.ContainsKey(strBundleName))
                return;

            m_LoadedAssetBundles.Add(strBundleName, new LoadedAssetBundle(bundle));
        }

        public AssetBundleLoadAssetOperation LoadAssetAsync<T>(GameAssetStruct gas, System.Action<T> pCallBack) where T : UnityEngine.Object
        {
            Log(LogType.Info, "Loading " + gas.AssetName + " from " + gas.BundleFullName + " bundle");

            AssetBundleLoadAssetOperation operation = null;
#if UNITY_EDITOR
            if (SimulateAssetBundleInEditor )
            {
                string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(gas.BundleFullName, gas.AssetName);
                if (assetPaths.Length == 0)
                {
                    Log(LogType.Error, "There is no asset with name \"" + gas.AssetName + "\" in " + gas.BundleFullName);
                    return null;
                }

                // @TODO: Now we only get the main object from the first asset. Should consider type also.
                //Object target = AssetDatabase.LoadMainAssetAtPath(assetPaths[0]);
                T target = AssetDatabase.LoadAssetAtPath<T>(assetPaths[0]);
                operation = new AssetBundleLoadAssetOperationSimulation(target, null);
                pCallBack(operation.GetAsset<T>());
            }
            else
#endif
            {
                LoadAssetBundleAsy(gas.BundleFullName);
                operation = new AssetBundleLoadAssetOperationFull(gas.BundleFullName, gas.AssetName, typeof(T), a => {
                    pCallBack(a.GetAsset<T>());
                });

                m_InProgressOperations.Add(operation);
            }

            return operation;
        }

        // Load asset from the given assetBundle.
        public AssetBundleLoadAssetOperation LoadAssetAsync<T>(string assetPath, System.Action<T> pCallBack) where T : UnityEngine.Object
        {
            GameAssetStruct gas = new GameAssetStruct(assetPath);
            return LoadAssetAsync(gas, pCallBack);
        }

        public AssetBundleLoadOperation LoadLevelAsync(string assetPath, bool isAdditive)
        {
            return LoadLevelAsync(new GameAssetStruct(assetPath), isAdditive);
        }
        // Load level from the given assetBundle.
        public AssetBundleLoadOperation LoadLevelAsync(GameAssetStruct gas, bool isAdditive)
        {
            Log(LogType.Info, "Loading " + gas.AssetName + " from " + gas.BundleFullName + " bundle");

            AssetBundleLoadOperation operation = null;
#if UNITY_EDITOR
            if (SimulateAssetBundleInEditor )
            {
                operation = new AssetBundleLoadLevelSimulationOperation(gas.BundleFullName, gas.AssetName, isAdditive, null);
            }
            else
#endif
            {
                LoadAssetBundleAsy( gas.BundleFullName);
                operation = new AssetBundleLoadLevelOperation(gas.BundleFullName, gas.AssetName, isAdditive, null);

                m_InProgressOperations.Add(operation);
            }

            return operation;
        }


    } // End of AssetBundleManager.
}

