using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RhFrameWork
{
    public class TaskManager: UnitySingleton<TaskManager>
    {
        /// <summary>
        /// 说明:开启协同任务
        /// </summary>
        /// <param name="pFunction"></param>
        /// <returns></returns>
        public Coroutine RunCoroutine(IEnumerator pFunction)
        {
            return StartCoroutine(pFunction);
        }

    }
}
