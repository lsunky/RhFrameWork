using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UiBase : MonoBehaviour {
    public virtual void Init()
    {
       
    }

    public void Open(UiBaseData data)
    {
        gameObject.SetActive(true);
        OnOpenHandle(data);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        OnCloseHandle();
    }

    protected virtual void OnOpenHandle(UiBaseData data)
    {

    }

    protected virtual void OnCloseHandle()
    {

    }
}
