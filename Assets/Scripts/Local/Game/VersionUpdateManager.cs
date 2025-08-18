using System;
using System.Collections;
using System.Collections.Generic;
using Local;
using UnityEngine;

public class VersionUpdateManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        AssetsDownLoad.CleanLocalCacheBundle();
        StartDownLoad();
    }

    private Coroutine coroutine = null;
    private void StartDownLoad()
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }
        coroutine = StartCoroutine(DownLoad());
    }
    /// <summary>尝试下载次数</summary>
    private int tryDownCount = 0;

    /// <summary>下载资源</summary>
    private IEnumerator DownLoad()
    {
        yield return new WaitForEndOfFrame();//这个地方必须等一帧,否则会产生回调闭包

        if (tryDownCount >= 1)
        {
            AssetsDownLoad.CleanLocalCacheBundle();

            Debug.LogError($"下载次数超过：{tryDownCount}，清空缓存后，重新下载");
        }

        //开始下载资源
        StartCoroutine(AssetsDownLoad.StartDownAsync(DownLoadEnd));
    }


    /// <summary>资源下载结束</summary>
    public void DownLoadEnd(bool success)
    {
        if (success == false)
        {
            Debug.LogError("资源下载失败,弹出UI提示，重新尝试下载......");

            tryDownCount++;

        }
        else
        {
            Debug.Log($"资源下载结束,进入主场景");

            StartCoroutine(App.Instance.EnterMainScene());
        }
    }
}
