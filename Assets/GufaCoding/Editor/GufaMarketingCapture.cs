using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GufaCoding.EditorTools
{
    /// <summary>
    /// 审核用截图：把游戏各界面按 1080x1920 渲染成 PNG。
    ///
    /// 踩过的坑，逐条说明：
    /// 1) 游戏是纯代码搭 UI（ScreenSpaceOverlay），Overlay 画布相机渲染不到，
    ///    所以把画布临时切成 ScreenSpaceCamera + RenderTexture 再截。
    /// 2) 编辑器（非播放模式）下 AddComponent 不触发 Awake，画布不会建，要手动调。
    /// 3) 切屏的 Clear() 用 Object.Destroy（延迟到帧末），编辑器没有帧循环，旧界面不会消失，
    ///    会和上一屏叠成花图 —— 每次切屏后精确清掉上一屏的残留节点。
    /// 4) 新版 UI 用 UITween 做弹入（起始 alpha=0 / scale 偏小），没有帧循环就不会播完，
    ///    截出来是隐形的 —— 截图前把 CanvasGroup 透明度和缩放强制归位。
    ///
    /// 必须带图形设备运行（不能加 -nographics，否则渲染是黑的）。
    /// </summary>
    public static class GufaMarketingCapture
    {
        private const int W = 1080;
        private const int H = 1920;
        private const string OutDir = "Marketing/Screenshots";

        private static RenderTexture _rt;
        private static Camera _cam;

        [MenuItem("古法编程/导出审核截图")]
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutDir);

            _rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var camGo = new GameObject("CaptureCamera", typeof(Camera));
            _cam = camGo.GetComponent<Camera>();
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Color.black;
            _cam.targetTexture = _rt;

            try
            {
                Capture("01-title");
                Capture("02-levels", new Step("ShowLevelSelect"));
                Capture("03-demand", new Step("ShowDemand", 0));
                Capture("04-stage", new Step("ShowDemand", 0), new Step("StartLevel"));
                Capture("05-shop", new Step("ShowShop"));
                Debug.Log("[Gufa] 截图完成：" + Path.GetFullPath(OutDir));
            }
            catch (Exception e)
            {
                Debug.LogError("[Gufa] 截图失败：" + e);
                EditorApplication.Exit(1);
                return;
            }
            finally
            {
                _cam.targetTexture = null;
                _rt.Release();
                UnityEngine.Object.DestroyImmediate(_cam.gameObject);
            }

            EditorApplication.Exit(0);
        }

        private readonly struct Step
        {
            public readonly string Method;
            public readonly object Arg;
            public Step(string method, object arg = null) { Method = method; Arg = arg; }
        }

        private static void Capture(string fileName, params Step[] steps)
        {
            var host = new GameObject("CaptureHost-" + fileName);
            var game = host.AddComponent<GufaGame>();
            Invoke(game, "Awake", null);   // 手动跑 Awake

            Transform canvasNode = host.transform.Find("GufaCanvas");
            Canvas canvas = canvasNode == null ? null : canvasNode.GetComponent<Canvas>();
            if (canvas == null) throw new InvalidOperationException("没找到 GufaCanvas");

            foreach (Step step in steps)
            {
                var stale = new List<GameObject>();
                foreach (string layerName in new[] { "Screen", "Popups" })
                {
                    Transform layer = canvas.transform.Find(layerName);
                    if (layer == null) continue;
                    for (int i = 0; i < layer.childCount; i++) stale.Add(layer.GetChild(i).gameObject);
                }

                Invoke(game, step.Method, step.Arg);

                foreach (GameObject go in stale)
                {
                    if (go != null) UnityEngine.Object.DestroyImmediate(go);
                }
            }

            ForceVisible(canvas.transform);

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _cam;
            canvas.planeDistance = 1f;

            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(W, H);
            scaler.matchWidthOrHeight = 0f;

            Shot(fileName);
            UnityEngine.Object.DestroyImmediate(host);
        }

        /// <summary>
        /// 把 Tween 的起始态（alpha=0、缩放偏小）拉到可见的终态。
        /// 只动 CanvasGroup 和缩放：Graphics 的颜色里有大量低透明度装饰层（行底纹、遮罩），
        /// 统一刷成不透明会把它们变成实心色块。
        /// </summary>
        private static void ForceVisible(Transform root)
        {
            foreach (CanvasGroup group in root.GetComponentsInChildren<CanvasGroup>(true))
            {
                // Toast 是常驻的空提示条（alpha=0），强制点亮会变成一条黑胶囊压在按钮上
                if (group.gameObject.name == "Toast") continue;
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t != root && t.localScale.x < 0.999f) t.localScale = Vector3.one;
            }
        }

        private static void Invoke(object target, string method, object arg)
        {
            MethodInfo m = target.GetType().GetMethod(method,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (m == null) throw new InvalidOperationException("找不到方法 " + method);
            m.Invoke(target, m.GetParameters().Length == 0 ? null : new[] { arg });
        }

        private static void Shot(string name)
        {
            Canvas.ForceUpdateCanvases();
            _cam.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _rt;

            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;

            string path = Path.Combine(OutDir, name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log("[Gufa] 已截图 " + path);
        }
    }
}
