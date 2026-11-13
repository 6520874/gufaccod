using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GufaCoding
{
    /// <summary>不依赖 DOTween 的几个小动效：弹出、淡入上浮、抖动、脉冲、闪烁。</summary>
    public static class UITween
    {
        class Runner : MonoBehaviour { }

        static Runner _runner;

        static Runner R
        {
            get
            {
                if (_runner == null)
                {
                    var go = new GameObject("[UITween]");
                    go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                    // 编辑器（非播放模式）下 DontDestroyOnLoad 会抛 InvalidOperationException，
                    // 预览、自动截图这类在编辑器里跑 UI 的流程会直接崩。只在真正运行时保持常驻。
                    if (Application.isPlaying) Object.DontDestroyOnLoad(go);
                    _runner = go.AddComponent<Runner>();
                }
                return _runner;
            }
        }

        public static float EaseOut(float x) { return 1f - Mathf.Pow(1f - x, 3f); }

        public static float Back(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        static CanvasGroup Group(Component c)
        {
            var cg = c.GetComponent<CanvasGroup>();
            if (cg == null) cg = c.gameObject.AddComponent<CanvasGroup>();
            return cg;
        }

        /// <summary>从小到大带回弹地弹出（弹窗用）。</summary>
        public static void PopIn(Transform t, float dur = 0.26f, float from = 0.82f)
        {
            if (t == null) return;
            R.StartCoroutine(PopRoutine(t, Group(t), dur, from));
        }

        static IEnumerator PopRoutine(Transform t, CanvasGroup cg, float dur, float from)
        {
            float e = 0f;
            t.localScale = new Vector3(from, from, 1f);
            cg.alpha = 0f;
            while (e < dur)
            {
                if (t == null) yield break;
                e += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(e / dur);
                float s = Mathf.LerpUnclamped(from, 1f, Back(p));
                t.localScale = new Vector3(s, s, 1f);
                cg.alpha = Mathf.Clamp01(p * 2.5f);
                yield return null;
            }
            if (t == null) yield break;
            t.localScale = Vector3.one;
            cg.alpha = 1f;
        }

        /// <summary>淡入 + 从下往上浮一点（页面切换用）。</summary>
        public static void FadeUp(RectTransform rt, float dur = 0.22f, float dy = 36f)
        {
            if (rt == null) return;
            R.StartCoroutine(FadeUpRoutine(rt, Group(rt), dur, dy));
        }

        static IEnumerator FadeUpRoutine(RectTransform rt, CanvasGroup cg, float dur, float dy)
        {
            Vector2 end = rt.anchoredPosition;
            float e = 0f;
            cg.alpha = 0f;
            rt.anchoredPosition = end - new Vector2(0, dy);
            while (e < dur)
            {
                if (rt == null) yield break;
                e += Time.unscaledDeltaTime;
                float p = EaseOut(Mathf.Clamp01(e / dur));
                cg.alpha = p;
                rt.anchoredPosition = end - new Vector2(0, dy * (1f - p));
                yield return null;
            }
            if (rt == null) yield break;
            cg.alpha = 1f;
            rt.anchoredPosition = end;
        }

        /// <summary>淡入淡出。</summary>
        public static void Fade(CanvasGroup cg, float from, float to, float dur)
        {
            if (cg == null) return;
            R.StartCoroutine(FadeRoutine(cg, from, to, dur));
        }

        static IEnumerator FadeRoutine(CanvasGroup cg, float from, float to, float dur)
        {
            float e = 0f;
            while (e < dur)
            {
                if (cg == null) yield break;
                e += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(e / dur));
                yield return null;
            }
            if (cg != null) cg.alpha = to;
        }

        /// <summary>左右抖动（点错时用）。</summary>
        public static void Shake(RectTransform rt, float amp = 16f, float dur = 0.32f)
        {
            if (rt == null) return;
            R.StartCoroutine(ShakeRoutine(rt, amp, dur));
        }

        static IEnumerator ShakeRoutine(RectTransform rt, float amp, float dur)
        {
            Vector2 o = rt.anchoredPosition;
            float e = 0f;
            while (e < dur)
            {
                if (rt == null) yield break;
                e += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Clamp01(e / dur);
                rt.anchoredPosition = o + new Vector2(Mathf.Sin(e * 70f) * amp * k, 0);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = o;
        }

        /// <summary>放大一下再回来。</summary>
        public static void Punch(Transform t, float to = 1.12f, float dur = 0.28f)
        {
            if (t == null) return;
            R.StartCoroutine(PunchRoutine(t, to, dur));
        }

        static IEnumerator PunchRoutine(Transform t, float to, float dur)
        {
            float e = 0f;
            while (e < dur)
            {
                if (t == null) yield break;
                e += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(e / dur);
                float s = 1f + (to - 1f) * Mathf.Sin(p * Mathf.PI);
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (t != null) t.localScale = Vector3.one;
        }

        /// <summary>一直闪烁（光标用），对象销毁时自动停止。</summary>
        public static void Blink(Graphic g, float period = 0.9f)
        {
            if (g == null) return;
            R.StartCoroutine(BlinkRoutine(g, period));
        }

        static IEnumerator BlinkRoutine(Graphic g, float period)
        {
            var wait = new WaitForSecondsRealtime(period / 2f);
            while (g != null)
            {
                g.enabled = !g.enabled;
                yield return wait;
            }
        }
    }
}
