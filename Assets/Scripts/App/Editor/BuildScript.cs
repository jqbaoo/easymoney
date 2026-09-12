using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EasyMoney.App.EditorTools
{
    /// <summary>
    /// 命令行构建入口。用法见 Tools/build-android.sh：
    /// Unity.exe -quit -batchmode -projectPath &lt;path&gt; -buildTarget Android
    ///           -executeMethod EasyMoney.App.EditorTools.BuildScript.BuildAndroid
    /// </summary>
    public static class BuildScript
    {
        private const string OUTPUT_PATH = "Builds/EasyMoney.apk";

        // 场景路径与 AndroidPlayerSettingsTests.BuildScene_Exists 里的常量保持一致。
        // 那个测试就是为了让「场景被改名」在构建之前就暴露，而不是白等二十分钟。
        private const string SCENE_PATH = "Assets/Scenes/SampleScene.unity";

        [MenuItem("EasyMoney/构建 Android APK")]
        public static void BuildAndroid()
        {
            string sProjectRoot = Directory.GetParent(Application.dataPath).FullName;
            string sOutputPath = Path.Combine(sProjectRoot, OUTPUT_PATH);

            Directory.CreateDirectory(Path.GetDirectoryName(sOutputPath));

            BuildPlayerOptions oOptions = new BuildPlayerOptions
            {
                scenes = new[] { SCENE_PATH },
                locationPathName = sOutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport oReport = BuildPipeline.BuildPlayer(oOptions);
            BuildSummary oSummary = oReport.summary;

            if (oSummary.result == BuildResult.Succeeded)
            {
                // 用 MB 而不是字节：构建日志是给人看的，几亿字节读不出「这包正常吗」
                Debug.Log($"构建成功: {sOutputPath}（{oSummary.totalSize / 1024 / 1024} MB）");
                EditorApplication.Exit(0);
                return;
            }

            // 显式 Exit(1)：批处理模式下若只靠 -quit，构建失败也可能以 0 退出，
            // 脚本就只能凭 APK 文件在不在来判断——那正是我们要避免的静默失败。
            Debug.LogError($"构建失败: {oSummary.result}，错误 {oSummary.totalErrors} 个");
            EditorApplication.Exit(1);
        }
    }
}
