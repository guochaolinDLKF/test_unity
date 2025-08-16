using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HybridCLR;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace Remote.Game
{
    public class LoadDll
    {
        private static List<string> AOTMetaAssemblyFiles { get; } = new()
        {
            "mscorlib.dll.bytes", 
            "System.dll.bytes", 
            "System.Core.dll.bytes",
            "tyme.dll.bytes",
        };


        private static Dictionary<string, byte[]> s_assetDatas = new Dictionary<string, byte[]>();

        private const string DLLExtension = ".dll";
        private const string BytesExtension = ".bytes";
        private const string RemoteHotDLLName = "RemoveAssembly";

        public IEnumerator ReadDll()
        {
#if UNITY_EDITOR
            // Editor环境下，HotUpdate.dll.bytes已经被自动加载，不需要加载，重复加载反而会出问题。
            Assembly hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "RemoveAssembly");
# else
            var assets = new List<string> { RemoteHotDLLName + DLLExtension + BytesExtension }.Concat(AOTMetaAssemblyFiles);

            foreach (var asset in assets)
            {
                string path = "";
                //dll的二进制
                byte[] dll = new byte[0];
                path = $"Assets/AddressableResources/Remote/Dll/{asset}";
                yield return StartGetResource(path, bytes => { s_assetDatas[asset] = bytes; });
            }

            LoadMetadataForAOTAssemblies();
            // 非Editor模式下，加载程序集
            Assembly hotUpdateAss = Assembly.Load(ReadBytesFromAssetData($"{RemoteHotDLLName}{DLLExtension}{BytesExtension}"));
#endif
            Type type = hotUpdateAss.GetType("Remote.Game.Runtime");
            type.GetMethod("Init").Invoke(null, null);
            yield break;
        }




        private IEnumerator StartGetResource(string path, Action<byte[]> callback)
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(path);
            yield return handle;

            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Failed)
            {
                Debug.LogError($"加载该路径{path}Dll文件失败......");
                yield break;
            }

            callback?.Invoke(handle.Result.bytes);
            Addressables.Release(handle);
        }


        private byte[] ReadBytesFromAssetData(string dllName)
        {
            if (s_assetDatas.ContainsKey(dllName))
            {
                return s_assetDatas[dllName];
            }
            return null;
        }




        private void LoadMetadataForAOTAssemblies()
        {
            /// 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
            /// 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
            HomologousImageMode mode = HomologousImageMode.SuperSet;
            foreach (var aotDllName in AOTMetaAssemblyFiles)
            {
                byte[] dllBytes = ReadBytesFromAssetData(aotDllName);
                // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
                LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
                Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode} ret:{err}");
            }
        }
    }
}