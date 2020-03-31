using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using RhFrameWork;
public class PreloadingWindow : MonoBehaviour
{
    public PopUpBox popUpBox;
    public Image image_Bar;
    public Text textDes;

    private Dictionary<GameStates, string> dicDes = new Dictionary<GameStates, string>() {
        { GameStates.CheckAppVersion, "校验版本中..." },
        { GameStates.CopyToPersistent, "解压资源中..." },
        { GameStates.CheckResource, "校验资源中..." },
        { GameStates.DownResource, "下载资源中..." },
    };
    private void Start()
    {
        GameStateManager.Instance.GameStateChangEvent += GameStateChangHandle;
        GameStateManager.Instance.DownErrorEvent += DownErrorHandle;
        GameStateManager.Instance.DownProgressChangeEvent += DownProgressChangeHandle;
        GameStateManager.Instance.ShowPopEvent += ShowPopHandle;
    }

    private void ShowPopHandle(bool isDoubleBtn, string strContent, Action sureAction, Action cancaleAction)
    {
        if (isDoubleBtn)
        {
            popUpBox.Open(strContent,sureAction,cancaleAction);
        }
        else
        {
            popUpBox.Open(strContent, sureAction);
        }
    }

    private void DownProgressChangeHandle(float hasDownSize, float allSize, float degree)
    {
        textDes.text = string.Format("{0}/{1}", hasDownSize, allSize);
        image_Bar.fillAmount = degree;
    }

    private void DownErrorHandle(string path, string errorInfo)
    {
        UDebug.LogError(string.Format("{0}下载错误{1}", path, errorInfo));
        popUpBox.Open("资源下载错误！", GameStateManager.Instance.Quit);
    }

    private void GameStateChangHandle(GameStates state)
    {
        string contentStr ;
        if (dicDes.TryGetValue(state, out contentStr))
        {
            textDes.text = contentStr;
        }
    }

    private void OnDestroy()
    {
        GameStateManager.Instance.GameStateChangEvent -= GameStateChangHandle;
        GameStateManager.Instance.DownErrorEvent -= DownErrorHandle;
        GameStateManager.Instance.DownProgressChangeEvent -= DownProgressChangeHandle;
        GameStateManager.Instance.ShowPopEvent -= ShowPopHandle;
    }


}
