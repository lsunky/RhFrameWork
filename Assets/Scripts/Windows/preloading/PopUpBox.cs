using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

public class PopUpBox : MonoBehaviour {
    [SerializeField]
    private Button btn_Sure;
    [SerializeField]
    public Button btn_Cancle;
    [SerializeField]
    private Button btn_SureCenter;
    [SerializeField]
    private Text txt_Content;

    private Action sureAction;
    private Action cancleAction;
    // Use this for initialization
    void Start () {
        this.btn_Sure.onClick.AddListener(btn_SureClickHandle);
        this.btn_Cancle.onClick.AddListener(btn_CancleClickHandle);
    }

    private void btn_SureClickHandle()
    {
        gameObject.SetActive(false);
        if (sureAction != null)
        {
            sureAction();
        }
    }

    private void btn_CancleClickHandle()
    {
        gameObject.SetActive(false);
        if (cancleAction != null )
        {
            cancleAction();
        }
    }
    
    public void Open(string content, Action sureAction,Action cancleAction)
    {
        btn_SureCenter.gameObject.SetActive(false);
        btn_Cancle.gameObject.SetActive(true);
        btn_Sure.gameObject.SetActive(true);
        gameObject.SetActive(true);
        this.sureAction = sureAction;
        this.cancleAction = cancleAction;
        txt_Content.text = content;
    }

    public void Open(string content, Action sureAction)
    {
        btn_SureCenter.gameObject.SetActive(true);
        btn_Cancle.gameObject.SetActive(false);
        btn_Sure.gameObject.SetActive(false);
        gameObject.SetActive(true);
        this.sureAction = sureAction;
        this.cancleAction = null;
        txt_Content.text = content;
    }
}
