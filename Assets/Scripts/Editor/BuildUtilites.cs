using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor.Utilities;
using HybridCLR.Editor;
using HybridCLR.Editor.BuildProcessors;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Installer;
using NUnit.Framework;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.tvOS;


namespace Editor
{
    public class BuildInfo
    {
        public string buildVersion { get; set; }
        public string buildTime { get; set; }
        public string buildPlatform { get; set; }
        public string buildEnv { get; set; }
        public List<FileInfo> fileList { get; set; }
    }

    public class FileInfo
    {
        public string fileName { get; set; }
    }

    public class BuildUtilites
    {
        /*
         * 构建版本时，先从远程获取版本信息，
         * 然后读取信息，并且得到当前需要发布的版本最新的相关信息
         * 1、版本号
         * 2、构建时间
         * 最终目录：android/debug/eight_char.apk
         * 每次上传都要只保留最新的5个包，这样避免占服务器空间
         *
         */
        private static string[] CommandArgs = null;

        /// <summary>
        /// 构建程序包
        /// </summary>
        public static void BuildTargetPlatform()
        {
            CommandArgs = System.Environment.GetCommandLineArgs();

            BuildTarget buildTarget = GetCurrentPlatform(CommandArgs);

            Debug.Log($"BuildTargetPlatform");

            string buildEnv = GetCurrentBuildEnv(CommandArgs);

            string buildVersion = GetCurrentBuildVersion(CommandArgs);

            //输出路径
            string outputPath = GetOutputPath(buildTarget, buildVersion, buildEnv);

            PlayerSettings.bundleVersion = buildVersion;

            Debug.Log($"outputPath:{outputPath}");

            Debug.Log($"buildVersion:{buildVersion}");

            Debug.Log($"buildEnv:{buildEnv}");

            try
            {
              BuildReport report =  BuildPipeline.BuildPlayer(EditorBuildSettings.scenes
                        .Select(s => s.path).ToArray(),
                    outputPath,
                    buildTarget,
                    BuildOptions.None);
              Debug.Log($"构建总时间:{report.summary.totalTime}，构建结果:{report.summary.result}");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [PostProcessBuild(1)] // postprocessOrder 参数控制执行顺序
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            // 构建完成后执行的逻辑
            UnityEngine.Debug.Log($"构建完成！路径：{pathToBuiltProject}");

            if (CommandArgs == null) return;
            string buildEnv = GetCurrentBuildEnv(CommandArgs);

            DateTime buildTime = DateTime.Now;
            string buildDateTime =
                $"{buildTime.Year}年{buildTime.Month}月{buildTime.Day}日 {buildTime.Hour}:{buildTime.Minute}:{buildTime.Second}";

            string buildVersion = PlayerSettings.bundleVersion;

            Debug.Log($"pathToBuiltProject:{pathToBuiltProject}");

            string directoryPath = Path.GetDirectoryName(pathToBuiltProject);


            Debug.Log($"buildVersion:{buildVersion}");

            Debug.Log($"buildEnv:{buildEnv}");
            Debug.Log($"buildDateTime:{buildDateTime}");


            BuildInfo buildInfo = new BuildInfo();
            buildInfo.buildVersion = buildVersion;
            buildInfo.buildTime = buildDateTime;
            buildInfo.buildPlatform = target.ToString();
            buildInfo.buildEnv = buildEnv;
            buildInfo.fileList = new List<FileInfo>();

            // 获取所有文件（含子目录）
            string[] allFiles = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                .ToArray();

            // string buildTimeStr = $"{buildTime.Year}{buildTime.Month}{buildTime.Day}{buildTime.Hour}{buildTime.Minute}{buildTime.Second}";
            //获取每一个文件的md5值
            for (int i = 0; i < allFiles.Length; i++)
            {
                string extension = Path.GetExtension(allFiles[i]);
                if (extension.Contains("apk") && target == BuildTarget.Android)
                {
                    FileInfo fileInfo = new FileInfo();
                    string fileName = Path.GetFileName(allFiles[i]);
                    fileInfo.fileName = fileName;
                    buildInfo.fileList.Add(fileInfo);
                }
            }

            string buildInfoStr = JsonConvert.SerializeObject(buildInfo);

            string buildInfoFile = $"{directoryPath}/buildInfo.json";
            File.WriteAllText(buildInfoFile, buildInfoStr);

            UnityEngine.Debug.Log($"创建版本信息文件完成!!!");
        }

        /// <summary>
        /// 构建热更脚本
        /// </summary>
        public static void GenerateHybridCLRCode()
        {
            // 调用 HybridCLR 生成热更代码（需引用相关命名空间）
            var installer = new HybridCLR.Editor.Installer.InstallerController();
            if (!installer.HasInstalledHybridCLR())
            {
                throw new BuildFailedException(
                    $"You have not initialized HybridCLR, please install it via menu 'HybridCLR/Installer'");
            }
            CommandArgs = System.Environment.GetCommandLineArgs();
            BuildTarget target = GetCurrentPlatform(CommandArgs);
            CompileDllCommand.CompileDll(target, EditorUserBuildSettings.development);

            Il2CppDefGeneratorCommand.GenerateIl2CppDef();

            // 这几个生成依赖HotUpdateDlls
            LinkGeneratorCommand.GenerateLinkXml(target);

            // 生成裁剪后的aot dll
            StripAOTDllCommand.GenerateStripedAOTDlls(target);

            // 桥接函数生成依赖于AOT dll，必须保证已经build过，生成AOT dll
            MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target);
            AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);


            EditorUtilities.CopyDll();
            Debug.Log("HybridCLR 热更代码生成完成");
        }

        /// <summary>
        /// 构建热更资源
        /// </summary>
        public static void BuildAddressables()
        {
             CommandArgs = System.Environment.GetCommandLineArgs();
             Debug.Log("BuildAddressables");
             BuildTarget buildTarget = GetCurrentPlatform(CommandArgs);
             Debug.Log($"buildTarget:{buildTarget}");
             string buildTargetPlatform = "Windows";
             switch (buildTarget)
             {
                 case BuildTarget.Android:
                     buildTargetPlatform= "Android";
                     break;
                 case BuildTarget.StandaloneWindows:
                     buildTargetPlatform= "Windows";
                     break;
             }
            
            string assetsMode = GetCurrentAssetsMode(CommandArgs);
            Debug.Log($"assetsMode:{assetsMode}");
            
            if (string.IsNullOrEmpty(assetsMode))
            {
                Debug.Log("buildAssetMode  参数缺少");
                return;
            }
            switch (assetsMode)
            {
                 case "All_Asset":
                     try
                     {
                         EditorUtilities.ClearCacheData();
                         AddressableAssetSettings.BuildPlayerContent();
                     } catch (Exception e) {
                         Debug.LogError($"异常中断：{e.Message}");
                     }
                     Debug.Log("Addressables 构建全量资源包完成");
                     break;
                 case "Increment_Asset":
                     try
                     {
                         var settings = AddressableAssetSettingsDefaultObject.Settings;
                         var statePath = $"{Application.dataPath}/AddressableAssetsData/{buildTargetPlatform}/addressables_content_state.bin"; 
                         ContentUpdateScript.BuildContentUpdate(settings, statePath);
                     }
                     catch (Exception e)
                     {
                         Debug.LogError(e);
                     }
                     Debug.Log("Addressables 构建增量资源包完成");
                     break;
            }
        }

        /// <summary>
        /// 获取当前平台
        /// </summary>
        /// <returns></returns>
        private static BuildTarget GetCurrentPlatform(string[] args)
        {
            string targetPlatform = GetArgValue(args, "-buildTarget");
            BuildTarget buildTarget = BuildTarget.NoTarget;
            if (!string.IsNullOrEmpty(targetPlatform))
                buildTarget = (BuildTarget)System.Enum.Parse(typeof(BuildTarget), targetPlatform);
            else
                buildTarget = EditorUserBuildSettings.activeBuildTarget;

            return buildTarget;
        }

        /// <summary>
        /// 获取文件的md5值
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string CalculateFileMD5(string filePath)
        {
            try
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);
                    return System.BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"MD5计算失败: {e.Message}");
                return null;
            } 
        }
        private static string GetCurrentAssetsMode(string[] args)
        {
            string targetBuildEnv = GetArgValue(args, "-buildAssetMode");
            return targetBuildEnv;
        }
        private static string GetCurrentAssetsEnv(string[] args)
        {
            string targetBuildEnv = GetArgValue(args, "-buildAssetEnv");
            return targetBuildEnv;
        }

        /// <summary>
        /// 获取当前构建环境
        /// </summary>
        /// <returns></returns>
        private static string GetCurrentBuildEnv(string[] args)
        {
            string targetBuildEnv = GetArgValue(args, "-buildEnv");
            return targetBuildEnv;
        }

        /// <summary>
        /// 获取当前版本号
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static string GetCurrentBuildVersion(string[] args)
        {
            string targetBuildEnv = GetArgValue(args, "-buildVersion");
            return targetBuildEnv;
        }

        /// <summary>
        /// 从命令行参数中提取值
        /// </summary>
        /// <param name="args"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        private static string GetArgValue(string[] args, string paramName)
        {
            for (int i = 0; i < args.Length; i++)
                if (args[i] == paramName && i + 1 < args.Length)
                    return args[i + 1];
            return null;
        }

        /// <summary>
        /// 获取输出目录
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <returns></returns>
        static string GetOutputPath(BuildTarget buildTarget, string buildVersion, string buildEnv)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                    return $"Builds/{buildTarget}/{buildEnv}/{buildVersion}/eight_char.exe";
                case BuildTarget.Android:
                    return $"Builds/{buildTarget}/{buildEnv}/{buildVersion}/eight_char.apk";
                case BuildTarget.iOS:
                    return $"Builds/{buildTarget}/{buildEnv}/{buildVersion}/eight_char";
                case BuildTarget.OpenHarmony:
                    return $"Builds/{buildTarget}/{buildEnv}/{buildVersion}/eight_char";
            }

            return $"Builds/{buildTarget}/{buildEnv}/{buildVersion}/eight_char.exe";
        }

        static string[] SplitVersion(string version)
        {
            return version.Split('.');
        }

        /// <summary>
        /// 安全删除文件夹
        /// </summary>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        public static bool SafeDeleteSubFolder(string targetPath)
        {
            try
            {
                // 处理跨平台路径格式
                targetPath = targetPath.Replace('/', Path.DirectorySeparatorChar);

                if (Directory.Exists(targetPath))
                {
                    // 先删除所有子文件
                    foreach (string file in Directory.GetFiles(targetPath, "*", SearchOption.AllDirectories))
                    {
                        File.SetAttributes(file, FileAttributes.Normal); // 解除只读属性
                        File.Delete(file);
                    }

                    // 再删除空目录
                    Directory.Delete(targetPath, true);

                    return true;
                }

                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"删除失败：{e.Message}");
                return false;
            }
        }
    }
}