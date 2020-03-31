
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RhFrameWork
{
    public class GameAssetStruct
    {
        private static Dictionary<string, string> dicFilePath2Bundle;
        public static void Init(string[] contentList)
        {
            dicFilePath2Bundle = new Dictionary<string, string>();
            string[] arr;
            foreach (var item in contentList)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    arr = item.Split('|');
                    dicFilePath2Bundle.Add(arr[0], arr[1]);
                }
                
            }
        }

        private string _assetName;
        public string AssetName
        {
            get
            {
                return _assetName;
            }
        }
        
        public string BundleFullName
        {
            get
            {
                return _bundleFullName;
            }
        }
        private string _bundleFullName;
        
        /// <summary>
        /// 自动打标签的用法
        /// </summary>
        public GameAssetStruct( string assetPath)
        {
            string bundlePath = string.Empty;
            if (dicFilePath2Bundle.TryGetValue(assetPath, out bundlePath))
            {
                _assetName = Path.GetFileName(assetPath);
                _bundleFullName = bundlePath;
            }
            else
                throw new Exception("dicFilePath2Bundle not contain assets,but you try to get it:" + assetPath);
           
        }
    }



  
}
