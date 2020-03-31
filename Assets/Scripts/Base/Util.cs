using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace RhFrameWork
{
    public class Util
    {
        /// <summary>
        /// 计算文件的MD5值
        /// </summary>
        public static string Md5file(string file)
        {
            try
            {
                FileStream fs = new FileStream(file, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(fs);
                fs.Close();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("md5file() fail, error:" + ex.Message);
            }
        }

        public static void WriteFile(string filePath, byte[] bytes)
        {
            String strPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(strPath))
            {
                Directory.CreateDirectory(strPath);
            }
            else
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            File.WriteAllBytes(filePath, bytes);
        }
        /// <summary>
        /// 计算文件大小
        /// </summary>
        public static string GetFileSize(string file)
        {
            try
            {
                FileStream fs = new FileStream(file, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(fs);
                fs.Close();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("md5file() fail, error:" + ex.Message);
            }
        }

        /// <summary>
        ///  对比两个列表得到差异列表
        /// </summary>
        /// <param name="localList">本地列表</param>
        /// <param name="serverList">云列表</param>
        /// <returns>对比结果</returns>
        public static FileCheckResult GetChangList(List<string> localList, List<string> serverList)
        {
            Dictionary<string, FileCheckInfo> dicLocal = new Dictionary<string, FileCheckInfo>();
            Dictionary<string, FileCheckInfo> dicServer = new Dictionary<string, FileCheckInfo>();
            FileCheckInfo checkInfo;
            foreach (var item in localList)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    checkInfo = new FileCheckInfo(item);
                    dicLocal[checkInfo.name] = checkInfo;
                    Debug.Log("dicLocal:" + checkInfo.name + "_" + checkInfo.md5);
                }
            }

            foreach (var item in serverList)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    checkInfo = new FileCheckInfo(item);
                    dicServer[checkInfo.name] = checkInfo;
                    Debug.Log("dicServer:" + checkInfo.name +"_"+ checkInfo.md5);
                }
            }

            FileCheckResult result = new FileCheckResult();
            foreach (var item in dicServer)
            {
                if (dicLocal.TryGetValue(item.Key, out checkInfo))
                {
                    if (!checkInfo.Equals(item.Value))
                    {
                        Debug.Log("localFile:" + checkInfo.md5);
                        Debug.Log("serverFile:" + item.Value.md5);
                        result.ChangFile(item.Value);
                        Debug.Log("ChangFile" + item.Key);
                    }
                    dicLocal.Remove(item.Key);
                }
                else
                {
                    Debug.Log("AddFile:" + item.Key);
                    result.AddFile(item.Value);
                }
            }

            foreach (var item in dicLocal)
            {
                result.DeleteFile(item.Value);
            }
            result.Sort();
            return result;
        }

        public static float SumSize(List<FileUpdateVo> files, int endIndex)
        {
            float size = 0f;
            for (int i = 0; i < endIndex; i++)
            {
                size += files[i].Size;
            }
            return size;
        }
    }
}

