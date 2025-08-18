using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.IO;

namespace Local
{
    /// <summary>
    /// 热更资源下载
    /// </summary>
    public class AssetsDownLoad
    {
        private static Action<bool> downloadEnd = null;

        private static AsyncOperationHandle downloadDependencies;//下载句柄

        private static DownLoadHandleInfoCarrier downLoadPercent = null;

        /// <summary>资源下载进度</summary>
        public static float DownLoadPercent
        {
            get
            {
                if (downLoadPercent == null) return 0;

                if (downLoadPercent.downLoadHandle.IsValid())
                {
                    downLoadPercent.precent = downloadDependencies.GetDownloadStatus().Percent;
                }
                else if (downLoadPercent.downLoadHandle.GetDownloadStatus().IsDone)
                {
                    downLoadPercent.precent = downLoadPercent.downLoadHandle.Status == AsyncOperationStatus.Succeeded ? 1f : 0;
                }
                return downLoadPercent.precent;
            }
        }

        /// <summary>下载资源大小</summary>
        public static float DownLoadSize => downLoadPercent == null ? 0 : downLoadPercent.downLoadSize;


        /// <summary>开始下载热更资源</summary>
        public static IEnumerator StartDownAsync(Action<bool> downloadEnd)
        {

#if UNITY_EDITOR
            //BuildScriptPackedPlayMode-->Use Existing Build
            if (!(UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.ActivePlayModeDataBuilder is
                  UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptPackedPlayMode))
            {
                Debug.Log("curr not Use Existing Build,not hot assets ");
                downloadEnd?.Invoke(true);
                yield break;
            }
#endif

            AssetsDownLoad.downloadEnd = downloadEnd;

            yield return DownAsync();

        }


        /// <summary>
        /// 删除本地缓存中的所有Bundle资源
        /// </summary>
        public static void CleanLocalCacheBundle()
        {
            string cachePath = Caching.currentCacheForWriting.path;

            string catalogPath = Addressables.LibraryPath.Replace("Library/", "").TrimEnd('/');

            //删除文件夹
            Action<string> action = path =>
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        Directory.Delete(path, true);
                    }
                    catch (IOException ex)
                    {
                        Debug.LogError($"文件夹无法删除，详情：{ex.Message}");
                    }
                }
            };

            //删除下载缓存的所有资源
            action?.Invoke(cachePath);
            //删除catalog
            action?.Invoke($"{Application.persistentDataPath}/{catalogPath}");
        }


        /// <summary>下载热更资源</summary>
        private static IEnumerator DownAsync()
        {

            Debug.Log("start down assets");

            //Debug.Log($"assets down path:{AssetsRemoteLoadPath.Path}");

            downLoadPercent = null;

            //默认热更成功
            bool success = true;

            //初始化Addressables
            AsyncOperationHandle<IResourceLocator> handle = Addressables.InitializeAsync(true);
            yield return handle;

            //检查所有可更新的内容目录以获取新版本
            AsyncOperationHandle<List<string>> catalogs = Addressables.CheckForCatalogUpdates(false);
            yield return catalogs;

            if (catalogs.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"CheckForCatalogUpdates Error \n{catalogs.OperationException.ToString()}");
                success = false;
            }


            if (catalogs.Result != null && catalogs.Result.Count > 0)
            {
                //更新指定的[catalogs.Result]目录
                var updateCatalogsHandle = Addressables.UpdateCatalogs(catalogs.Result, false);
                yield return updateCatalogsHandle;

                if (updateCatalogsHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"UpdateCatalogs Error\n {updateCatalogsHandle.OperationException.ToString()}");
                    success = false;
                }

                Addressables.Release(updateCatalogsHandle);
            }



            List<object> requestDownLoadKeys = new List<object>();
            foreach (var item in Addressables.ResourceLocators)
            {
                requestDownLoadKeys.AddRange(item.Keys);
            }

            //读取下载size
            AsyncOperationHandle<long> getDownloadSize = Addressables.GetDownloadSizeAsync(requestDownLoadKeys as IEnumerable);
            yield return getDownloadSize;

            if (getDownloadSize.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"GetDownloadSizeAsync Error\n {getDownloadSize.OperationException.ToString()}");
                success = false;
            }


            Debug.Log($" need down load count: {requestDownLoadKeys.Count}  down load size：{(getDownloadSize.Result / (1024f * 1024f)).ToString("0.000")}MB");

            if (success && getDownloadSize.Result > 0)
            {
                //下载资源
                downloadDependencies = Addressables.DownloadDependenciesAsync(requestDownLoadKeys as IEnumerable, Addressables.MergeMode.Union, false);
                downLoadPercent = new DownLoadHandleInfoCarrier(downloadDependencies, getDownloadSize.Result);
                yield return downloadDependencies;

                success = downloadDependencies.Status == AsyncOperationStatus.Succeeded ? true : false;

                downLoadPercent.precent = downloadDependencies.Status == AsyncOperationStatus.Succeeded ? 1 : 0f;

                Debug.Log("<color=#00ff00>assets down load complete</color>");
                Addressables.Release(downloadDependencies);
            }

            AssetsDownLoad.downloadEnd?.Invoke(success);

            AssetsDownLoad.downloadEnd = null;

            downLoadPercent = null;

            Debug.Log($"down finish -->result:{success}");

            //释放操作句柄
            Addressables.Release(catalogs);
            Addressables.Release(getDownloadSize);
        }


        /// <summary>下载句柄信息</summary>
        private class DownLoadHandleInfoCarrier
        {
            public AsyncOperationHandle downLoadHandle;

            /// <summary>下载句柄信息</summary>
            public float precent = 0;

            /// <summary>需下载资源size</summary>
            public float downLoadSize = 0;

            public DownLoadHandleInfoCarrier(AsyncOperationHandle downLoadHandle, float downLoadSize)
            {
                this.downLoadHandle = downLoadHandle;
                this.precent = 0;
                this.downLoadSize = downLoadSize;
            }
        }

    }
}


