//======================================
//	Author: Matrix
//  Create Time：3/10/2017 12:21:13 PM 
//  Function:
//======================================

using UnityEngine;
using System.Collections.Generic;
public class UnitySingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static object _lock = new object();

    public static T Instance
    {
        get
        {
            lock (_lock)
            {
                if (instance == null)
                {
                    if (instance == null)
                    {
                        GameObject singleton = new GameObject();
                        instance = singleton.AddComponent<T>();
                        singleton.name = "(singleton)" + typeof(T).ToString();

                        DontDestroyOnLoad(singleton);
                    }
                }
                return instance;
            }
        }
    }
   
}