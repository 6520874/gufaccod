using System;
using System.Collections;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace GufaCoding
{
    /// <summary>
    /// TapTap 小游戏 / H5 的广告底层桥接。
    ///
    /// 微信构建走 WeChatWASM（见 Ads.cs 的 GUFA_WECHAT 分支）；TapTap 容器注入的是
    /// window.tap，走 tap.createRewardedVideoAd / createInterstitialAd。
    /// 回调靠轮询而不是 SendMessage：WebGL 模板可换皮，jslib 里拿不到稳定的 gameInstance。
    /// </summary>
    internal static class GufaTapAds
    {
        public const int StateIdle = 0;
        public const int StateLoading = 1;
        public const int StateShowing = 2;
        public const int StateFinished = 3;   // 激励视频看完 / 插屏关闭
        public const int StateCanceled = 4;   // 中途关闭
        public const int StateError = 5;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int GufaTapAdsIsSupported();
        [DllImport("__Internal")] private static extern void GufaTapAdsInit(string spaceId);
        [DllImport("__Internal")] private static extern void GufaTapAdsShowRewarded();
        [DllImport("__Internal")] private static extern void GufaTapAdsShowInterstitial();
        [DllImport("__Internal")] private static extern int GufaTapAdsPoll();
        [DllImport("__Internal")] private static extern int GufaTapAdsErrorCode();
        [DllImport("__Internal")] private static extern void GufaTapAdsReset();
        [DllImport("__Internal")] private static extern void GufaTapAdsDestroy();
#endif

        private static bool supportValue;

        /// <summary>是否运行在注入了 tap 广告 SDK 的 TapTap 容器里。</summary>
        public static bool IsSupported
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (supportValue) return true;
                supportValue = GufaTapAdsIsSupported() != 0;
                return supportValue;
#else
                return false;
#endif
            }
        }

        public static void Init(string spaceId)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!IsSupported || string.IsNullOrEmpty(spaceId)) return;
            GufaTapAdsInit(spaceId);
#endif
        }

        public static void ShowRewarded()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsSupported) GufaTapAdsShowRewarded();
#endif
        }

        public static void ShowInterstitial()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsSupported) GufaTapAdsShowInterstitial();
#endif
        }

        public static int Poll()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return IsSupported ? GufaTapAdsPoll() : StateError;
#else
            return StateError;
#endif
        }

        public static int ErrorCode
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return IsSupported ? GufaTapAdsErrorCode() : 0;
#else
                return 0;
#endif
            }
        }

        public static void Reset()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsSupported) GufaTapAdsReset();
#endif
        }

        public static void Destroy()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsSupported) GufaTapAdsDestroy();
#endif
        }
    }

    /// <summary>
    /// TapTap 广告实现。拿不到广告位或不在 TapTap 容器里时，由 Ads.Provider 回退到模拟广告。
    /// </summary>
    public class TapTapAdProvider : IAdProvider
    {
        private static TapTapRunner runner;

        /// <summary>可真播广告的前提：在 TapTap 容器内，且配置了广告位 ID。</summary>
        public static bool IsAvailable =>
            GufaTapAds.IsSupported && !string.IsNullOrWhiteSpace(AdConfig.TapTapSpaceId);

        public TapTapAdProvider()
        {
            GufaTapAds.Init(AdConfig.TapTapSpaceId);
        }

        public void ShowRewarded(string placement, Action<bool> onDone)
        {
            EnsureRunner().PlayRewarded(onDone);
        }

        public void ShowInterstitial(string placement, Action onClosed)
        {
            EnsureRunner().PlayInterstitial(onClosed);
        }

        private static TapTapRunner EnsureRunner()
        {
            if (runner != null) return runner;
            var go = new GameObject("TapTapAdRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<TapTapRunner>();
            return runner;
        }
    }

    /// <summary>轮询广告状态机的协程宿主。</summary>
    internal class TapTapRunner : MonoBehaviour
    {
        private const float RewardLoadingTimeout = 20f;
        private const float InterstitialTimeout = 8f;

        public void PlayRewarded(Action<bool> onDone)
        {
            StartCoroutine(RewardedRoutine(onDone));
        }

        public void PlayInterstitial(Action onClosed)
        {
            StartCoroutine(InterstitialRoutine(onClosed));
        }

        private IEnumerator RewardedRoutine(Action<bool> onDone)
        {
            GufaTapAds.Reset();
            GufaTapAds.ShowRewarded();

            // 只在「还没开播」阶段计时：首播要下载素材；一旦进入播放就不再计时，
            // 否则视频还在放就被判超时，看完也拿不到奖励。
            float deadline = Time.realtimeSinceStartup + RewardLoadingTimeout;
            bool showing = false;

            while (true)
            {
                int state = GufaTapAds.Poll();
                if (state == GufaTapAds.StateFinished) { onDone(true); yield break; }
                if (state == GufaTapAds.StateCanceled) { onDone(false); yield break; }
                if (state == GufaTapAds.StateError)
                {
                    Debug.LogWarning("[Ads] TapTap 激励视频失败，错误码 " + GufaTapAds.ErrorCode);
                    onDone(false);
                    yield break;
                }
                if (state == GufaTapAds.StateShowing) showing = true;
                if (!showing && Time.realtimeSinceStartup > deadline)
                {
                    Debug.LogWarning("[Ads] TapTap 激励视频加载超时");
                    onDone(false);
                    yield break;
                }
                yield return new WaitForSecondsRealtime(0.2f);
            }
        }

        private IEnumerator InterstitialRoutine(Action onClosed)
        {
            GufaTapAds.Reset();
            GufaTapAds.ShowInterstitial();

            float deadline = Time.realtimeSinceStartup + InterstitialTimeout;
            while (Time.realtimeSinceStartup < deadline)
            {
                int state = GufaTapAds.Poll();
                // 插屏只要不是加载中就该继续流程
                if (state == GufaTapAds.StateFinished || state == GufaTapAds.StateCanceled ||
                    state == GufaTapAds.StateError)
                {
                    break;
                }
                yield return new WaitForSecondsRealtime(0.2f);
            }

            if (onClosed != null) onClosed();
        }
    }
}
