using System.Collections;
using System.Collections.Generic;
using Local.Core;
using UnityEngine;

public class LoadDllInScene : MonoBehaviour
{
    private LoadDll loadDll = null;


    private IEnumerator Start()
    {
        Debug.Log($"启动热更程序集......");

        // 创建 LoadDll 的新实例，准备加载 DLL
        loadDll = new LoadDll();

        // 启动协程读取 DLL
        // 使用 StartCoroutine 启动协程，加载热更程序集
        yield return StartCoroutine(loadDll.ReadDll());

        yield return new WaitForEndOfFrame();

        //热更加载完毕，删除该对象
        GameObject.Destroy(gameObject);
    }
}
