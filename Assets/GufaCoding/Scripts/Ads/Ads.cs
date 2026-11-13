using System;
using UnityEngine;
using UnityEngine.UI;

namespace GufaCoding
{
    /// <summary>广告位 ID。在微信公众平台「流量主」里创建后填这里。</summary>
    public static class AdConfig
    {
        public const string RewardedUnitId = "adunit-xxxxxxxxxxxxxxxx";
        public const string InterstitialUnitId = "adunit-yyyyyyyyyyyyyyyy";

        /// <summary>
        /// TapTap 小游戏/H5 的广告位 ID（激励视频/插屏/Banner 共用同一个 space id）。
        /// 由 TapTap 开发者后台「广告变现」开通后生成；留空则回退到模拟广告。
        /// </summary>
        public const string TapTapSpaceId = "";
    }

    public interface IAdProvider
    {
        /// <summary>激励视频。onDone(true) 表示看完了，可以发奖励。</summary>
        void ShowRewarded(string placement, Action<bool> onDone);

        /// <summary>插屏。无论成功失败都会回调 onClosed，游戏流程继续。</summary>
        void ShowInterstitial(string placement, Action onClosed);
    }

    /// <summary>
    /// 广告统一入口。编辑器和未接 SDK 时用模拟广告；
    /// 在 Player Settings 的 Scripting Define Symbols 里加上 GUFA_WECHAT，就切到微信广告。
    /// </summary>
    public static class Ads
    {
        static IAdProvider _provider;

        public static IAdProvider Provider
        {
            get
            {
                if (_provider == null)
                {
#if GUFA_WECHAT && !UNITY_EDITOR
                    _provider = new WeChatAdProvider(AdConfig.RewardedUnitId, AdConfig.InterstitialUnitId);
#else
                    // TapTap 容器里且配了广告位就播真广告，否则用模拟广告保证流程可走完
                    _provider = TapTapAdProvider.IsAvailable
                        ? (IAdProvider)new TapTapAdProvider()
                        : new MockAdProvider();
#endif
                }
                return _provider;
            }
            set { _provider = value; }
        }

        public static void ShowRewarded(string placement, Action<bool> onDone)
        {
            Debug.Log("[Ads] 激励视频 → " + placement);
            Provider.ShowRewarded(placement, ok =>
            {
                Debug.Log("[Ads] 激励视频结束 " + placement + " 看完=" + ok);
                if (onDone != null) onDone(ok);
            });
        }

        public static void ShowInterstitial(string placement, Action onClosed)
        {
            Debug.Log("[Ads] 插屏 → " + placement);
            Provider.ShowInterstitial(placement, () => { if (onClosed != null) onClosed(); });
        }
    }

    // ———————————————————— 模拟广告（编辑器里测流程用） ————————————————————

    public class MockAdProvider : IAdProvider
    {
        public void ShowRewarded(string placement, Action<bool> onDone)
        {
            MockAdOverlay.Show("【模拟激励视频】" + placement, 3f, true, onDone);
        }

        public void ShowInterstitial(string placement, Action onClosed)
        {
            MockAdOverlay.Show("【模拟插屏广告】" + placement, 1.5f, false, _ => { if (onClosed != null) onClosed(); });
        }
    }

    public class MockAdOverlay : MonoBehaviour
    {
        Action<bool> _done;
        float _left;
        bool _rewarded;
        Text _count;
        Button _claim;

        public static void Show(string title, float seconds, bool rewarded, Action<bool> done)
        {
            var go = new GameObject("MockAd", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<MockAdOverlay>().Build(title, seconds, rewarded, done);
        }

        void Build(string title, float seconds, bool rewarded, Action<bool> done)
        {
            _done = done;
            _left = seconds;
            _rewarded = rewarded;

            var bg = UIKit.Panel(transform, new Color(0, 0, 0, 0.94f), "Bg");
            bg.rectTransform.Stretch();

            var t = UIKit.Label(bg.transform, title + "\n\n（真机这里会播放广告）", 44, Color.white);
            Center(t.rectTransform, 0, 260, 900, 260);

            _count = UIKit.Label(bg.transform, "", 64, UIKit.Hex("#DCDCAA"));
            Center(_count.rectTransform, 0, 60, 900, 120);

            if (rewarded)
            {
                _claim = UIKit.Btn(bg.transform, "看完了，领取奖励", UIKit.Hex("#0E639C"), Color.white, 40, () => Close(true));
                Center(_claim.GetComponent<RectTransform>(), 0, -120, 760, 130);
                _claim.interactable = false;
            }

            var close = UIKit.Btn(bg.transform, rewarded ? "中途关闭（拿不到奖励）" : "关闭", UIKit.Hex("#3C3C3C"), Color.white, 36,
                () => Close(false));
            Center(close.GetComponent<RectTransform>(), 0, -280, 760, 110);
        }

        static void Center(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        void Update()
        {
            if (_left <= 0) return;
            _left -= Time.unscaledDeltaTime;
            _count.text = _left > 0 ? Mathf.CeilToInt(_left) + " 秒" : "播放完毕";
            if (_left <= 0)
            {
                if (_rewarded) _claim.interactable = true;
                else Close(true);
            }
        }

        void Close(bool ok)
        {
            var d = _done;
            _done = null;
            Destroy(gameObject);
            if (d != null) d(ok);
        }
    }

    // ———————————————————— 微信小游戏广告 ————————————————————
    // 需要先导入微信官方的 Unity 转换插件（minigame-unity-webgl-transform），它提供 WeChatWASM 命名空间。
    // 下面按插件常见的 API 写法实现；不同插件版本签名可能略有差异，编译报错时对照插件里的 WX.cs 改一下即可。
#if GUFA_WECHAT && !UNITY_EDITOR
    public class WeChatAdProvider : IAdProvider
    {
        readonly WeChatWASM.WXRewardedVideoAd _rewarded;
        readonly WeChatWASM.WXInterstitialAd _interstitial;
        Action<bool> _pendingReward;
        Action _pendingInterstitial;

        public WeChatAdProvider(string rewardedId, string interstitialId)
        {
            if (!string.IsNullOrEmpty(rewardedId))
            {
                _rewarded = WeChatWASM.WX.CreateRewardedVideoAd(new WeChatWASM.WXCreateRewardedVideoAdParam { adUnitId = rewardedId });
                _rewarded.OnClose(res => FinishReward(res != null && res.isEnded));
                _rewarded.OnError(err => Debug.LogWarning("[Ads] 激励视频错误 " + (err != null ? err.errMsg : "")));
            }
            if (!string.IsNullOrEmpty(interstitialId))
            {
                _interstitial = WeChatWASM.WX.CreateInterstitialAd(new WeChatWASM.WXCreateInterstitialAdParam { adUnitId = interstitialId });
                _interstitial.OnClose(() => FinishInterstitial());
                _interstitial.OnError(err => Debug.LogWarning("[Ads] 插屏错误 " + (err != null ? err.errMsg : "")));
            }
        }

        public void ShowRewarded(string placement, Action<bool> onDone)
        {
            if (_rewarded == null) { onDone(false); return; }
            _pendingReward = onDone;
            // 第一次 Show 失败时重新拉取一次再播
            _rewarded.Show(null, _ =>
                _rewarded.Load(__ => _rewarded.Show(null, ___ => FinishReward(false)), ___ => FinishReward(false)));
        }

        public void ShowInterstitial(string placement, Action onClosed)
        {
            if (_interstitial == null) { onClosed(); return; }
            _pendingInterstitial = onClosed;
            _interstitial.Show(null, _ => FinishInterstitial()); // 没填充就直接继续，不卡流程
        }

        void FinishReward(bool ok)
        {
            var cb = _pendingReward;
            _pendingReward = null;
            if (cb != null) cb(ok);
        }

        void FinishInterstitial()
        {
            var cb = _pendingInterstitial;
            _pendingInterstitial = null;
            if (cb != null) cb();
        }
    }
#endif
}
