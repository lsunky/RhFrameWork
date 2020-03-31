//======================================
//	Author: Matrix
//  Create Time：3/10/2017 11:44:04 AM 
//  Function:
//======================================

using System;
using System.Collections.Generic;
using System.Threading;
public class Singleton<T> where T : Singleton<T>, new()
{

    private static T instance = default(T);
    private static object s_objectLock = new object();
    public static T Instance
    {
        get
        {
            if (Singleton<T>.instance == null)
            {
                object obj;
                Monitor.Enter(obj = Singleton<T>.s_objectLock);
                try
                {
                    if (Singleton<T>.instance == null)
                    {
                        Singleton<T>.instance = ((default(T) == null) ? Activator.CreateInstance<T>() : default(T));
                    }
                }
                finally
                {
                    Monitor.Exit(obj);
                }
            }
            return Singleton<T>.instance;
        }
    }

}
