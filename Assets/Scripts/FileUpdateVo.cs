using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace RhFrameWork
{
    public class FileUpdateVo
    {
        private string fileUrl;
        private string persistentPath;
        private string streamingPath;
        private string md5;
        private float size;
        private string fileName;
        private string random;
        private string url;

        private static string GetRelativePath(string filePath,bool usePlatform)
        {
            if (usePlatform)
            {
                return (AppConst.ResRoot + PathHelper.PlatformNameRunTime + "/" + filePath).Trim();
            }
            else
            {
                return (AppConst.ResRoot + filePath).Trim();
            }
        }

        public FileUpdateVo(string localPath, string url, string random, string md5, float size,bool usePlatform = true)
        {
            this.random = random;
            this.url = url;
            string relativePath = GetRelativePath( localPath, usePlatform);
            this.fileUrl = url + "/" + relativePath + "?v=" + random;
            this.persistentPath = PathHelper.PersistentPath + relativePath;
            this.streamingPath = PathHelper.StreamingPath() + relativePath;

            this.md5 = md5;
            this.size = size;
            this.fileName = Path.GetFileName(localPath);
        }

        public string FileUrl
        {
            get
            {
                return fileUrl;
            }
        }

        public string Md5
        {
            get
            {
                return md5;
            }
        }
        
        public float Size
        {
            get
            {
                return size;
            }
        }

        /// <summary>
        /// StreamingAssets 路径
        /// </summary>
        public string StreamingPath
        {
            get
            {
                return streamingPath;
            }
        }

        public string PersistentPath
        {
            get
            {
                return persistentPath;
            }
        }

        public string FileName
        {
            get
            {
                return fileName;
            }
        }

        public string Random
        {
            get
            {
                return random;
            }
        }

        public string Url
        {
            get
            {
                return url;
            }
        }
    }
}

