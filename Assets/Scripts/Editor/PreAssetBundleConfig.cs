using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RhFrameWork
{
    public struct AssetBundleFFix
    {
        public string prefix;
        public string suffix;
    }
    public enum BundleBatchType
    {
        /// <summary>
        /// 零碎打包
        /// </summary>
        None,

        /// <summary>
        /// 所有的子文件全打到根目录这一个包里
        /// </summary>
        AllToOne,

        /// <summary>
        /// 各自文件夹下批量打到一个包里
        /// </summary>
        FloderToOne,

        /// <summary>
        /// 自定义规则
        /// </summary>
        DIY

    }
    public class PreAssetBundleConfig
    {
        public delegate string DiyBundleNameRule(string path);
        public string path;
        public string[] keyword;
        public AssetBundleFFix ffix;
        public BundleBatchType batchType; //批量打包，包名为资源上级目录的名字 
        public SearchOption searchOption = SearchOption.AllDirectories;
        public string pattern;
        public DiyBundleNameRule diyBundleNameRule;

        /// <summary>
        /// keyword支持多个，用|分割
        /// </summary>
        /// <param name="_path"></param>
        /// <param name="_keyword"></param>
        /// <param name="_ffix"></param>
        /// <param name="_batch"></param>
        public PreAssetBundleConfig(string _path, string _keyword, AssetBundleFFix _ffix, BundleBatchType _batch, string _pattern = null, DiyBundleNameRule rule = null)
        {
            this.path = _path;
            this.ffix = _ffix;
            this.batchType = _batch;
            this.keyword = _keyword.Split('|');
            this.pattern = _pattern;
            this.diyBundleNameRule = rule;
        }
    }
}
