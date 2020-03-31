using RhFrameWork;
using RhFrameWork.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lanch : MonoBehaviour {

	// Use this for initialization
	void Start () {
        Screen.orientation = ScreenOrientation.AutoRotation;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;

        GameStateManager.Instance.CreateStateMachine();
        //string fileUrl = @"http://172.21.175.89:80/GameRes//ResRoot/Android/AssetBundles/prefab/loginwindow.ab";
        //TaskManager.Instance.StartCoroutine((MyTest(fileUrl)));
    }

    IEnumerator MyTest(string FileUrl)
    {
        float maxNum = 100;
        for (int i = 0; i < maxNum; i++)
        {
            int randomNum = Random.Range(1, 100000);
            FileUrl = FileUrl + "?v=" + randomNum;
            WWW www = new WWW(FileUrl);
            yield return www;
            if (string.IsNullOrEmpty(www.error))
            {
                UDebug.Log("WriteFile:  loginwindow.ab ");
                string tempFilePath = PathHelper.PersistentPath + "loginwindow"+i+".ab";
                Util.WriteFile(tempFilePath, www.bytes);
            }
            else
            {
                UDebug.LogError(string.Format("{0}down failed:{1}", FileUrl, www.error));
                yield break;
            }
            GameStateManager.Instance.DownProgressChangeEvent(i+1, maxNum, (float)i/maxNum);
            yield return new WaitForSeconds(0.2f);
        }
       
    }
	// Update is called once per frame
	void Update () {
		
	}
}
