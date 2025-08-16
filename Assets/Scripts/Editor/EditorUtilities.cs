using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using tyme.eightchar;
using tyme.enums;
using tyme.solar;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
namespace Editor.Utilities
{
    public class EditorUtilities
    {
        [MenuItem("GameObject/获取子对象路径 &Q")]
        public static void CopyGoFllPath()
        {
            TextEditor textEditor = new TextEditor();
            textEditor.text = $"transform.Find(\"{GetPath(Selection.activeTransform)}\").GetComponent<TMP_Text>();";
            textEditor.SelectAll();
            textEditor.Copy();

            Debug.Log("success");
        }

        private static string GetPath(Transform select)
        {
            if (select == null) return string.Empty;

            if (select.parent == null) return select.name;

            return $"{GetPath(select.parent)}/{select.name}";
        }



        [MenuItem("Tools/清空 PlayerPrefs 数据")]
        public static void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();

            Debug.Log($"PlayerPrefs数据清除成功");
        }



        [MenuItem("Tools/清空 资源缓存资源 数据")]
        public static void ClearCacheData()
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




        //[MenuItem("Tools/加密本地配置文件到StreamingAssets")]
        //public static void EncrypLocalConfigToStreamingAssets()
        //{
        //    // 读取文件路径
        //    string path = "Assets/Scripts/Editor/Config/LocalConfig_TMP.json";

        //    // 检查文件是否存在
        //    if (!File.Exists(path))
        //    {
        //        Debug.LogError("本地加密配置文件不存在: " + path);
        //        return;
        //    }

        //    //解密密钥
        //    string unlockKey = File.ReadAllText("Assets/Scripts/Editor/Config/Unlock.json");

        //    // 加载 JSON 文件
        //    string jsonContent = File.ReadAllText(path);

        //    // 假设 CryptoManager.DecryptStr 进行加密
        //    string encryptedContent = CryptoManager.EncryptStr(unlockKey, jsonContent); // 使用加密方法

        //    // 获取 StreamingAssets 目录路径
        //    string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, "LocalConfig.json");

        //    // 创建目标目录（如果不存在）
        //    string directory = Path.GetDirectoryName(streamingAssetsPath);
        //    if (!Directory.Exists(directory))
        //    {
        //        Directory.CreateDirectory(directory);
        //    }

        //    // 将加密后的内容写入到新的文件中
        //    File.WriteAllText(streamingAssetsPath, encryptedContent, Encoding.UTF8);

        //    // 输出日志
        //    Debug.Log("加密的配置文件已保存到: " + streamingAssetsPath);

        //    AssetDatabase.Refresh();
        //}



        [MenuItem("Tools/切换Panel字体")]
        public static void SwitchPanelFonts()
        {
            // 获取当前选中的 GameObject
            GameObject selectedObject = Selection.activeGameObject;

            // 检查是否有选中的 GameObject
            if (selectedObject == null)
            {
                EditorUtility.DisplayDialog("错误", "没有选中的 GameObject。请选中一个预制体。", "确定");

                return;
            }

            // 确保选中的对象是一个预制体
            if (PrefabUtility.GetPrefabAssetType(selectedObject) != PrefabAssetType.Regular &&
                PrefabUtility.GetPrefabAssetType(selectedObject) != PrefabAssetType.Model)
            {
                EditorUtility.DisplayDialog("错误", "选中的对象不是有效的预制体。", "确定");
                return;
            }

            // 弹出文件选择对话框让用户选择新的字体
            string path = EditorUtility.OpenFilePanel("选择新的 TMP 字体", "Assets/TextMesh Pro/Resources/Fonts & Materials", "asset");
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("错误", "未选择新的字体。", "确定");
                return;
            }

            // 确保路径在项目内
            path = FileUtil.GetProjectRelativePath(path);
            TMP_FontAsset newFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (newFont == null)
            {
                EditorUtility.DisplayDialog("错误", "选择的文件不是有效的 TMP 字体。", "确定");
                return;
            }

            // 加载预制体
            string prefabPath = AssetDatabase.GetAssetPath(selectedObject);
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);

            // 查找并替换 TMP_Text 组件的字体
            TMP_Text[] texts = prefab.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in texts)
            {
                text.font = newFont;
            }

            // 保存更改
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefab);

            // 提示操作成功
            Debug.Log($"字体已更新并且预制体已保存");
        }



        [MenuItem("Tools/拷贝热更Dll文件")]
        public static void CopyDll()
        {
            // 相对路径设置
            string SourceRelativePath = "HybridCLRData/HotUpdateDlls/{0}/RemoveAssembly.dll";
            string TargetRelativePath = "AddressableResources/Remote/Dll/RemoveAssembly.dll.bytes";


            // 获取当前平台
            string platformFolder = EditorUserBuildSettings.activeBuildTarget.ToString();


            // 获取项目的根路径
            string projectRootPath = Application.dataPath;


            // 拼接源文件和目标文件的完整路径
            string sourcePath = Path.Combine(projectRootPath, "..", string.Format(SourceRelativePath, platformFolder));
            string targetPath = Path.Combine(projectRootPath, TargetRelativePath);


            // 检查源文件是否存在
            if (!File.Exists(sourcePath))
            {
                Debug.LogError($"源文件不存在：{sourcePath}");
                return;
            }

            try
            {
                // 确保目标目录存在
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

                // 拷贝文件
                File.Copy(sourcePath, targetPath, true);
                Debug.Log($"文件拷贝成功！源文件：{sourcePath} 目标文件：{targetPath}");

                AssetDatabase.Refresh();
            }
            catch (IOException ex)
            {
                Debug.LogError($"文件拷贝失败：{ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"意外错误：{ex.Message}");
            }
        }



        [MenuItem("Tools/童限不同流派测试")]
        public static void ChildLimitTest()
        {
            Debug.Log("临时测试");




            //出生举例时间：公历：1994:10:17:00:25:29
            //设置起运流派：计算出童限时间
            //DefaultChildLimitProvider:    童限时间：7年3月25日20时3分------>起大运的时间：公历2022年2月8日，阴历2001腊月廿九20点03
            //China95ChildLimitProvider：   童限时间：7年3月25日0时3分------> 起大运的时间：公历2022年2月8日，阴历2001腊月廿九00点03
            //LunarSect1ChildLimitProvider：童限时间：7年3月20日0时3分------> 起大运的时间：公历2022年2月3日，阴历2001腊月廿四00点03
            //LunarSect2ChildLimitProvider：童限时间：7年3月25日20时3分------>起大运的时间：公历2022年2月8日，阴历2001腊月廿九20点03
            //                                                                                      |                    |
            //                                                                                      |                    |
            //                                                                                     壬午                 壬午


            //实际测试结果
            SolarTime birthTime = SolarTime.FromYmdHms(1994, 10, 17, 00, 25, 29);
            ChildLimit childLimit = ChildLimit.FromSolarTime(birthTime, Gender.Man);



            //获取童限小运集合
            HashSet<Fortune> fortunes = new HashSet<Fortune>(10);

            Fortune fortune = childLimit.StartFortune;

            while (true)
            {
                fortune = fortune.Next(-1);

                fortunes.Add(fortune);

                if (fortune.SixtyCycleYear.SixtyCycle.GetName() == childLimit.StartSixtyCycleYear.SixtyCycle.GetName()) break;
            }

            foreach (var item in fortunes)
            {
                Debug.Log($" {item.SixtyCycle.GetName()}  {item.SixtyCycleYear.SixtyCycle.GetName()}");
            }
        }





        [MenuItem("Tools/设置五行位置")]
        public static void CalculateWuXingPoint()
        {
            Debug.Log("设置五行位置......");

            // 获取当前选中的GameObject
            GameObject selectedObject = Selection.activeGameObject;

            if (selectedObject == null)
            {
                Debug.LogError("没有选中的GameObject");
                return;
            }

            // 获取选中对象的所有子物体的RectTransform，但不包括自己
            RectTransform[] images = selectedObject.GetComponentsInChildren<RectTransform>();

            // 过滤掉自己，只保留子物体
            images = System.Array.FindAll(images, rt => rt.gameObject != selectedObject);

            if (images.Length < 5)
            {
                Debug.LogError("子物体数量少于5个");
                return;
            }

            float radius = 185;
            int numImages = images.Length;

            // 计算并设置每个图像的位置，让第一个元素从 (x = 0, y = 正数) 开始
            for (int i = 0; i < numImages; i++)
            {
                // 计算角度，初始角度为90度，让第一个元素从顶部开始
                float angle = 2 * Mathf.PI / numImages * i + Mathf.PI / 2;

                // 计算x和y坐标
                float x = radius * Mathf.Cos(angle);
                float y = radius * Mathf.Sin(angle);

                // 设置子物体的anchoredPosition
                images[i].anchoredPosition = new Vector2(x, y);

                Debug.Log($"{images[i].gameObject.name} 设置位置: ({x}, {y})");
            }
        }



    }





}





