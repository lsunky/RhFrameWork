using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RhFrameWork;
public class LoginWindow : UiBase {
    public InputField inputAccount;
    public InputField inputPwd;

    private int tempNum;
    protected override void OnOpenHandle(UiBaseData data)
    {
        UDebug.Log("LoginWindow open ");
        inputAccount.text = "tempCode" + tempNum;
        tempNum++;
    }

    protected override void OnCloseHandle()
    {
        inputAccount.text = string.Empty;
        inputPwd.text = string.Empty;
    }
}
