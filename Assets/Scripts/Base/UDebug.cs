using System;
using System.Collections.Generic;
using UnityEngine;
namespace RhFrameWork
{
    public class UDebug
    {
        public static string logStr = string.Empty;

        private static bool _enableLog = true;
        public static bool enableLog
        {
            set
            {
                //_enableLog = value;
            }
        }
     
        public static void Log(string content)
        {
            if(_enableLog)
                Debug.Log(string.Format("<color=#c3ff55>{0}</color>", content));
        }

        public static void LogWarn(string content)
        {
            if (_enableLog)
                Debug.Log(string.Format("<color=#ff9933>{0}</color>", content));
        }

        public static void LogError(string content)
        {
            if (_enableLog)
                Debug.LogError(string.Format("<color=#ff0000>{0}</color>", content));
        }

        public static void LogErrorList(List<object> contentList)
        {
            string content = string.Empty;
            foreach(object o in contentList)
            {
                content += o.ToString();
            }
            Debug.LogError(string.Format("<color=#ff0000>{0}</color>", content));
        }
    }
}
