using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GufaCoding.EditorTools
{
    /// <summary>
    /// 一键：确保场景存在 + 构建 WebGL。
    /// 命令行：Unity -batchmode -quit -projectPath &lt;proj&gt;
    ///         -executeMethod GufaCoding.EditorTools.GufaBuildPipeline.BuildWebGL
    /// </summary>
    public static class GufaBuildPipeline
    {
        public const string ScenePath = "Assets/GufaCoding/Scenes/GufaMain.unity";
        public const string OutputDir = "Builds/TapTapWebGL";

        [MenuItem("古法编程/构建 WebGL (TapTap)")]
        public static void BuildWebGL()
        {
            EnsureScene();

            if (Resources.Load<Font>("GufaFont") == null)
            {
                Debug.LogError("[Gufa] 缺少 Resources/GufaFont，WebGL 里中文会全是空白，构建中止。");
                EditorApplication.Exit(1);
                return;
            }

            // 托管裁剪会按不到 Resources.Load 的反射路径，用低裁剪换稳定性
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Low);
            // 托管端不保证给压缩包带 Content-Encoding，靠 fallback 自带解压器最稳（产物 *.unityweb）
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.memorySize = 128;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log(string.Format("[Gufa] 构建结束：{0}，大小 {1:N1} MB，耗时 {2}",
                summary.result, summary.totalSize / 1048576f, summary.totalTime));

            if (summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        /// <summary>场景不存在就按游戏要求生成一份（相机 + GufaGame），并设为构建第一个场景。</summary>
        public static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color32(0x1E, 0x1E, 0x1E, 0xFF);
                cam.orthographic = true;

                new GameObject("GufaGame").AddComponent<GufaGame>();

                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("[Gufa] 已生成场景 " + ScenePath);
            }

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == ScenePath && s.enabled)) return;
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
