using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class App : MonoBehaviour
{
    public static App Instance = null;
    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        Instance = this;
        
    }

    public IEnumerator EnterMainScene()
    {
        Scene sampleScene = SceneManager.GetActiveScene();
        
        //加载Main场景
        var handle = Addressables.LoadSceneAsync("Assets/AddressableResources/Remote/Scenes/Main.unity", LoadSceneMode.Additive);
        yield return handle;
        
        //切换到Main场景
        SceneManager.SetActiveScene(SceneManager.GetSceneByName("Main"));
        
        //卸载sampleScene场景
        var unloadHandle = SceneManager.UnloadSceneAsync(sampleScene);
        yield return unloadHandle;

        //释放内村资源
        Addressables.Release(handle);
        Addressables.Release(unloadHandle);
    }
}
