using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RhFrameWork;
using UnityEngine;
public class UiMgr:Singleton<UiMgr>
{
    private Transform uiRoot;
    private Dictionary<WindowId, string> dicWindowsName = new Dictionary<WindowId, string>() {
        { WindowId .Login,"LoginWindow"}
    };
    private Dictionary<WindowId, UiBase> dicWindows  = new Dictionary<WindowId, UiBase>();

    public bool Init()
    {
        GameObject tempGo = GameObject.Find("UiRoot");
        if (tempGo == null)
        {
            return false;
        }
        uiRoot = tempGo.transform;
        return true;
    }

    public void Open(WindowId windowId,UiBaseData data)
    {
        UiBase window;
        if (!dicWindows.TryGetValue(windowId, out window))
        {
            string path = string.Format("Prefab/{0}", dicWindowsName[windowId]);
            GameObject prefab = AssetBundleManager.Instance.LoadAsset<GameObject>(path);
            window = GameObject.Instantiate<GameObject>(prefab, uiRoot).GetComponent<UiBase>();
            window.Init();
        }
        window.Open(data);
    }

    
}
