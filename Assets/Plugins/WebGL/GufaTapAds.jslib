mergeInto(LibraryManager.library, {
  // TapTap 小游戏 / H5 激励视频 + 插屏桥接。
  //
  // 两条硬约束（都踩过）：
  // 1) *.jslib 顶层代码只在构建期执行，不进运行期产物。
  // 2) Unity 只把 C# 侧 DllImport 得上的函数编进包，顶层辅助对象（$Name:{...}）会被整块丢掉。
  //    所以所有逻辑必须写在导出函数体内，状态挂在 window.__gufaTapAds。
  //
  // 状态机：0=空闲 1=加载中 2=播放中 3=完成(激励看完/插屏关闭) 4=中途关闭 5=出错

  GufaTapAdsIsSupported: function () {
    return (typeof window !== 'undefined' && !!window.tap &&
      typeof window.tap.createRewardedVideoAd === 'function') ? 1 : 0;
  },

  GufaTapAdsInit: function (spaceIdPtr) {
    if (typeof window === 'undefined' || !window.tap ||
        typeof window.tap.createRewardedVideoAd !== 'function') {
      return;
    }
    var st = window.__gufaTapAds;
    if (!st) {
      st = window.__gufaTapAds = {
        spaceId: '', rewarded: null, interstitial: null,
        state: 0, errorCode: 0, retried: false
      };
    }
    var spaceId = spaceIdPtr ? UTF8ToString(spaceIdPtr) : '';
    if (spaceId) st.spaceId = spaceId;
    if (!st.spaceId) return;

    // 创建逻辑挂在状态对象上，供 show* 复用；定义在 Init 体内所以会一起进产物
    st.ensureRewarded = function () {
      if (st.rewarded) return st.rewarded;
      if (typeof window.tap === 'undefined' || !window.tap ||
          typeof window.tap.createRewardedVideoAd !== 'function') return null;
      var ad;
      try {
        ad = window.tap.createRewardedVideoAd({ adUnitId: st.spaceId });
      } catch (e) {
        st.state = 5; st.errorCode = (e && e.errCode) || 0; return null;
      }
      if (!ad) { st.state = 5; return null; }
      st.rewarded = ad;
      if (typeof ad.onError === 'function') {
        ad.onError(function (err) {
          st.errorCode = (err && err.errCode) || 0;
          if (st.state === 2) st.state = 5;
        });
      }
      if (typeof ad.onClose === 'function') {
        ad.onClose(function (res) {
          if (st.state !== 1 && st.state !== 2) return;
          st.state = (res && res.isEnded) ? 3 : 4;
        });
      }
      if (typeof ad.load === 'function') {
        var pre = ad.load();
        if (pre && typeof pre.catch === 'function') {
          pre.catch(function (err) { st.errorCode = (err && err.errCode) || 0; });
        }
      }
      return ad;
    };

    st.ensureInterstitial = function () {
      if (st.interstitial) return st.interstitial;
      if (typeof window.tap === 'undefined' || !window.tap ||
          typeof window.tap.createInterstitialAd !== 'function') return null;
      var ad;
      try {
        ad = window.tap.createInterstitialAd({ adUnitId: st.spaceId });
      } catch (e) {
        return null;
      }
      if (!ad) return null;
      st.interstitial = ad;
      if (typeof ad.onClose === 'function') {
        ad.onClose(function () {
          if (st.state === 1 || st.state === 2) st.state = 3;
        });
      }
      if (typeof ad.onError === 'function') {
        ad.onError(function (err) {
          st.errorCode = (err && err.errCode) || 0;
          if (st.state === 1 || st.state === 2) st.state = 5;
        });
      }
      if (typeof ad.load === 'function') {
        var pre = ad.load();
        if (pre && typeof pre.catch === 'function') pre.catch(function () { });
      }
      return ad;
    };

    st.ensureRewarded();
  },

  GufaTapAdsShowRewarded: function () {
    var st = (typeof window !== 'undefined') ? window.__gufaTapAds : null;
    if (!st) return;
    var ad = (typeof st.ensureRewarded === 'function') ? st.ensureRewarded() : st.rewarded;
    if (!ad) { st.state = 5; return; }

    st.state = 1;
    st.retried = false;
    st.errorCode = 0;

    // 同步异常会穿透 wasm 调用栈，C# 的 try/catch 接不住，必须在这里挡住
    try {
      ad.show().then(function () {
        if (st.state === 1) st.state = 2;
      }).catch(function (showError) {
        st.errorCode = (showError && showError.errCode) || 0;
        if (st.retried) { st.state = 5; return; }
        st.retried = true;                       // 未就绪：load 后只重试一次
        ad.load().then(function () {
          return ad.show();
        }).then(function () {
          if (st.state === 1) st.state = 2;
        }).catch(function (retryError) {
          st.errorCode = (retryError && retryError.errCode) || st.errorCode;
          st.state = 5;
        });
      });
    } catch (syncError) {
      st.errorCode = (syncError && syncError.errCode) || 0;
      st.state = 5;
    }
  },

  GufaTapAdsShowInterstitial: function () {
    var st = (typeof window !== 'undefined') ? window.__gufaTapAds : null;
    if (!st) return;
    var ad = (typeof st.ensureInterstitial === 'function') ? st.ensureInterstitial() : st.interstitial;
    // 插屏拿不到就当已关闭，绝不阻塞游戏流程
    if (!ad) { st.state = 3; return; }

    st.state = 1;
    st.errorCode = 0;
    try {
      ad.show().then(function () {
        if (st.state === 1) st.state = 2;
      }).catch(function (err) {
        st.errorCode = (err && err.errCode) || 0;
        st.state = 3;
      });
    } catch (syncError) {
      st.state = 3;
    }
  },

  GufaTapAdsPoll: function () {
    var st = (typeof window !== 'undefined') ? window.__gufaTapAds : null;
    return st ? (st.state | 0) : 0;
  },

  GufaTapAdsErrorCode: function () {
    var st = (typeof window !== 'undefined') ? window.__gufaTapAds : null;
    return st ? (st.errorCode | 0) : 0;
  },

  GufaTapAdsReset: function () {
    var st = (typeof window !== 'undefined') ? window.__gufaTapAds : null;
    if (!st) return;
    st.state = 0;
    st.errorCode = 0;
  },

  GufaTapAdsDestroy: function () {
    var st = (typeof window !== 'undefined') ? window.__gufaTapAds : null;
    if (!st) return;
    ['rewarded', 'interstitial'].forEach(function (k) {
      var ad = st[k];
      if (ad && typeof ad.destroy === 'function') { try { ad.destroy(); } catch (e) { } }
      st[k] = null;
    });
    st.state = 0;
    st.errorCode = 0;
  }
});
