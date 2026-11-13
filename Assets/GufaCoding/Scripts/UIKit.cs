using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GufaCoding
{
    /// <summary>
    /// 纯代码搭 uGUI 的小工具：圆角、投影、按压回弹、布局。整个游戏不依赖预制体。
    /// </summary>
    public static class UIKit
    {
        static Font _font, _mono;

#if UNITY_2022_2_OR_NEWER
        const string BuiltinFontName = "LegacyRuntime.ttf";
#else
        const string BuiltinFontName = "Arial.ttf";
#endif

        // ———————————————— 字体 ————————————————

        /// <summary>正文字体：优先 Resources/GufaFont，其次系统中文字体。微信小游戏里必须放 GufaFont，否则中文不显示。</summary>
        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.Load<Font>("GufaFont");
                    if (_font == null)
                        _font = OsFont(new[] { "Microsoft YaHei", "PingFang SC", "Hiragino Sans GB", "Noto Sans CJK SC", "SimHei", "Arial" });
                }
                return _font;
            }
        }

        /// <summary>代码字体：优先 Resources/GufaMono，其次 GufaFont，其次系统等宽字体。</summary>
        public static Font Mono
        {
            get
            {
                if (_mono == null)
                {
                    _mono = Resources.Load<Font>("GufaMono");
                    if (_mono == null) _mono = Resources.Load<Font>("GufaFont");
                    if (_mono == null)
                        _mono = OsFont(new[] { "Consolas", "Menlo", "Courier New", "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC" });
                }
                return _mono;
            }
        }

        static Font OsFont(string[] names)
        {
            Font f = null;
            try { f = Font.CreateDynamicFontFromOSFont(names, 32); } catch (Exception) { }
            if (f == null) f = Resources.GetBuiltinResource<Font>(BuiltinFontName);
            return f;
        }

        // ———————————————— 图片资源 ————————————————

        static Sprite _logo;
        static bool _logoTried;

        /// <summary>Resources/GufaLogo.png（印章 Logo），没有就返回 null。</summary>
        public static Sprite Logo
        {
            get
            {
                if (!_logoTried)
                {
                    _logoTried = true;
                    var tex = Resources.Load<Texture2D>("GufaLogo");
                    if (tex != null)
                        _logo = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
                return _logo;
            }
        }

        static readonly Dictionary<int, Sprite> _round = new Dictionary<int, Sprite>();

        /// <summary>运行时生成一张带抗锯齿的圆角九宫格图，半径单位为 UI 像素。</summary>
        public static Sprite Round(int radius)
        {
            radius = Mathf.Clamp(radius, 2, 128);
            Sprite cached;
            if (_round.TryGetValue(radius, out cached) && cached != null) return cached;

            int size = radius * 2 + 4;
            float half = size / 2f, inner = half - radius;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - inner, 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - inner, 0f);
                float a = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);

            int b = radius + 1;
            var s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            s.name = "Round" + radius;
            _round[radius] = s;
            return s;
        }

        // ———————————————— 颜色 ————————————————

        public static Color Hex(string hex, float alpha = 1f)
        {
            Color c;
            if (!ColorUtility.TryParseHtmlString(hex, out c)) c = Color.magenta;
            c.a = alpha;
            return c;
        }

        public static Color WithA(Color c, float a) { c.a = a; return c; }

        // ———————————————— 节点与定位 ————————————————

        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>铺满父节点，四边留出指定边距。</summary>
        public static RectTransform Stretch(this RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>贴着父节点顶部的一条横带。</summary>
        public static RectTransform TopBand(this RectTransform rt, float height, float top = 0, float inset = 0)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(inset, -top - height);
            rt.offsetMax = new Vector2(-inset, -top);
            return rt;
        }

        /// <summary>任意锚点 + 边距定位。</summary>
        public static RectTransform Box(this RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        /// <summary>固定尺寸，挂在某个锚点上。</summary>
        public static RectTransform Pin(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        // ———————————————— 控件 ————————————————

        public static Image Panel(Transform parent, Color color, string name = "Panel", int radius = 0)
        {
            var rt = Node(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (radius > 0)
            {
                img.sprite = Round(radius);
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, bool mono = false, bool bold = false, bool rich = false)
        {
            var rt = Node(parent, "Label");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = mono ? Mono : Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = rich;
            t.raycastTarget = false;
            t.lineSpacing = 1.1f;
            return t;
        }

        public static void DropShadow(Graphic g, float distance = 8f, float alpha = 0.35f)
        {
            var s = g.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            s.effectColor = new Color(0, 0, 0, alpha);
            s.effectDistance = new Vector2(0, -distance);
        }

        /// <summary>给任意 Image 加上按钮行为（点击、按压回弹、禁用不变灰——颜色由调用方决定）。</summary>
        public static Button MakeButton(Image img, UnityAction onClick)
        {
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.55f);
            colors.fadeDuration = 0.06f;
            b.colors = colors;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            img.gameObject.AddComponent<Pressable>();
            if (onClick != null) b.onClick.AddListener(onClick);
            return b;
        }

        public static Button Btn(Transform parent, string text, Color bg, Color fg, int size, UnityAction onClick,
            bool mono = false, TextAnchor align = TextAnchor.MiddleCenter, int radius = 18, bool shadow = true, bool bold = true)
        {
            var img = Panel(parent, bg, "Button", radius);
            if (shadow) DropShadow(img, 6f, 0.3f);
            var b = MakeButton(img, onClick);
            var t = Label(img.transform, text, size, fg, align, mono, bold);
            t.rectTransform.Stretch(28, 4, 28, 4);
            return b;
        }

        // ———————————————— 布局 ————————————————

        public static VerticalLayoutGroup VList(GameObject go, float spacing, int left, int right, int top, int bottom,
            TextAnchor align = TextAnchor.UpperCenter)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(left, right, top, bottom);
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HList(GameObject go, float spacing, bool expandWidth = true,
            TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = expandWidth;
            h.childForceExpandHeight = expandWidth;
            return h;
        }

        static LayoutElement LE(Component c)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            return le;
        }

        /// <summary>在布局组里固定高度。</summary>
        public static T H<T>(this T c, float height) where T : Component
        {
            var le = LE(c);
            le.minHeight = height;
            le.preferredHeight = height;
            return c;
        }

        /// <summary>在布局组里固定宽度。</summary>
        public static T W<T>(this T c, float width) where T : Component
        {
            var le = LE(c);
            le.minWidth = width;
            le.preferredWidth = width;
            return c;
        }

        /// <summary>在布局组里吃掉剩余高度。</summary>
        public static T Flex<T>(this T c) where T : Component
        {
            LE(c).flexibleHeight = 1;
            return c;
        }

        /// <summary>在布局组里吃掉剩余宽度。</summary>
        public static T FlexW<T>(this T c) where T : Component
        {
            LE(c).flexibleWidth = 1;
            return c;
        }
    }

    /// <summary>按下缩小、松开回弹的手感。</summary>
    public class Pressable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float pressedScale = 0.95f;
        float _target = 1f;
        Selectable _sel;

        void Awake() { _sel = GetComponent<Selectable>(); }

        public void OnPointerDown(PointerEventData e)
        {
            if (_sel == null || _sel.IsInteractable()) _target = pressedScale;
        }

        public void OnPointerUp(PointerEventData e) { _target = 1f; }
        public void OnPointerExit(PointerEventData e) { _target = 1f; }

        void Update()
        {
            float s = transform.localScale.x;
            if (Mathf.Abs(s - _target) < 0.0005f) return;
            s = Mathf.Lerp(s, _target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 28f));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
