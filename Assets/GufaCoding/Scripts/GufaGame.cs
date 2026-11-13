using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GufaCoding
{
    /// <summary>
    /// 古法编程模拟器 · 主流程。
    /// 挂到场景里任意空物体上即可运行，所有界面由代码生成。
    ///
    /// 流程：标题 → 选关 → 产品经理提需求 → 找 Bug / 补代码（限时、掉头发）
    ///       → 编译（插屏广告位）→ 可能的线上事故 → 结算（加班费 x2）→ 下一关 → 终章
    /// </summary>
    public class GufaGame : MonoBehaviour
    {
        // ———————— 配色：VS Code Dark+ ————————
        static readonly Color BG = UIKit.Hex("#1E1E1E");
        static readonly Color Surface = UIKit.Hex("#252526");
        static readonly Color Surface2 = UIKit.Hex("#2D2D30");
        static readonly Color Surface3 = UIKit.Hex("#37373D");
        static readonly Color Border = UIKit.Hex("#3C3C3C");
        static readonly Color Editor = UIKit.Hex("#1A1A1A");
        static readonly Color TabBar = UIKit.Hex("#181818");
        static readonly Color TextC = UIKit.Hex("#D4D4D4");
        static readonly Color TextHi = UIKit.Hex("#F2F2F2");
        static readonly Color DimC = UIKit.Hex("#8B8B8B");
        static readonly Color Gutter = UIKit.Hex("#6E7681");
        static readonly Color Green = UIKit.Hex("#4EC9B0");
        static readonly Color CommentC = UIKit.Hex("#6A9955");
        static readonly Color Yellow = UIKit.Hex("#DCDCAA");
        static readonly Color Gold = UIKit.Hex("#FFD54A");
        static readonly Color Accent = UIKit.Hex("#007ACC");
        static readonly Color Red = UIKit.Hex("#F14C4C");
        static readonly Color Orange = UIKit.Hex("#CE9178");
        static readonly Color SealRed = UIKit.Hex("#C8322B");
        static readonly Color HintBG = UIKit.Hex("#4D4320");
        static readonly Color OkBG = UIKit.Hex("#1F4A36");
        static readonly Color BadBG = UIKit.Hex("#5A1D1D");
        static readonly Color BlankBG = UIKit.Hex("#2E2A1A");
        static readonly Color DangerBtn = UIKit.Hex("#7A2626");
        static readonly Color Dark = UIKit.Hex("#111111");

        const float StatusH = 56f;

        // ———————— 广告位与每日上限 ————————
        // hint 请教大佬 / revive 植发复活 / delay 找老板延期 / double 加班费翻倍 / rollback 紧急回滚 / slack 摸鱼领工资
        static readonly Dictionary<string, int> AdCaps = new Dictionary<string, int>
        {
            { "hint", 20 }, { "revive", 5 }, { "delay", 10 }, { "double", 10 }, { "rollback", 5 }, { "slack", 3 },
        };

        const int InterstitialFromLevel = 3;  // 第 4 关起才出插屏（下标从 0 开始）
        const int InterstitialEvery = 2;      // 每通关 2 次最多一次插屏
        const float IncidentChance = 0.2f;    // 线上事故概率
        const float PmChangeChance = 0.3f;    // 产品经理中途改需求的概率（纯吐槽）

        static readonly string[] PmQuips =
        {
            "顺便把这个按钮改成五彩斑斓的黑。",
            "logo 再大一点，但是也再小一点。",
            "这个需求很简单，怎么实现我不管。",
            "用户想要一个一键变富的按钮。",
            "隔壁公司有个功能，你照着做一个，明天上。",
        };

        static readonly string[] MissQuips =
        {
            "不是这行……",
            "测试在群里 @ 你了。",
            "你盯着这行看了半天，它是对的。",
            "老板路过，叹了口气。",
        };

        static readonly string[] CompileLogs =
        {
            "$ gcc main.c -o main",
            "正在编译……去泡杯茶吧",
            "warning: 87 个警告（问题不大）",
            "链接器正在思考人生……",
            "笔记本风扇起飞中……",
            "Build succeeded. 0 error(s)",
        };

        // ———————— UI 根节点 ————————
        Canvas _canvas;
        CanvasScaler _scaler;
        RectTransform _screen, _page, _popupLayer;
        Text _statusLeft, _statusRight;
        GameObject _popup;
        RectTransform _toastRt;
        Text _toast;
        CanvasGroup _toastGroup;
        Coroutine _toastCo;
        float _safeTop = -1, _safeBottom = -1;

        // ———————— 本局状态 ————————
        int _levelIndex, _stageIndex, _hair, _hairLost, _bugsFixed, _pmChanges;
        float _timeLeft, _bonusTime;
        bool _timerOn, _hintUsed, _duckUsed;
        static int _levelsSinceInterstitial;

        // 游戏中界面的引用
        Text _hairText, _deadlineText;
        Image _deadlinePill, _hairFill;
        readonly List<Image> _lineImgs = new List<Image>();
        readonly List<Image> _optionImgs = new List<Image>();

        Level CurLevel { get { return LevelDatabase.Levels[_levelIndex]; } }
        Stage CurStage { get { return CurLevel.Stages[_stageIndex]; } }

        // ================================================================
        //  启动
        // ================================================================

        void Awake()
        {
            Application.targetFrameRate = 60;
            EnsureEventSystem();
            BuildCanvas();
            ShowTitle();
        }

        void EnsureEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            if (FindFirstObjectByType<EventSystem>() != null) return;
#else
            if (FindObjectOfType<EventSystem>() != null) return;
#endif
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        void BuildCanvas()
        {
            var cgo = new GameObject("GufaCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            _canvas = cgo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _scaler = cgo.GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1080, 1920);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            UpdateScaler();

            UIKit.Panel(cgo.transform, BG, "Background").rectTransform.Stretch();

            // 内容区固定 1080 宽、竖向铺满；宽屏时居中。上下边距在 ApplySafeArea 里按刘海屏调整
            _screen = UIKit.Node(cgo.transform, "Screen");
            _screen.anchorMin = new Vector2(0.5f, 0);
            _screen.anchorMax = new Vector2(0.5f, 1);
            _screen.pivot = new Vector2(0.5f, 0.5f);
            _screen.offsetMin = new Vector2(-540, 0);
            _screen.offsetMax = new Vector2(540, 0);

            BuildStatusBar();

            _popupLayer = UIKit.Node(cgo.transform, "Popups").Stretch();
            BuildToast(cgo.transform);
        }

        void UpdateScaler()
        {
            if (_scaler == null) return;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            // 比 9:16 更宽就按高度适配，更窄（长屏手机）就按宽度适配，保证 1080x1920 的内容区完整可见
            _scaler.matchWidthOrHeight = aspect > 1080f / 1920f ? 1f : 0f;
        }

        void ApplySafeArea()
        {
            if (_canvas == null || _screen == null) return;
            float sf = _canvas.scaleFactor;
            if (sf <= 0) return;
            Rect sa = Screen.safeArea;
            float top = Mathf.Max(0, (Screen.height - sa.yMax) / sf);
            float bottom = Mathf.Max(0, sa.yMin / sf);
            if (Mathf.Approximately(top, _safeTop) && Mathf.Approximately(bottom, _safeBottom)) return;
            _safeTop = top;
            _safeBottom = bottom;
            _screen.offsetMin = new Vector2(-540, bottom);
            _screen.offsetMax = new Vector2(540, -top);
        }

        void Update()
        {
            UpdateScaler();
            ApplySafeArea();

            if (_deadlinePill != null)
            {
                bool urgent = _timerOn && _timeLeft <= 5f;
                float s = urgent ? 1f + 0.07f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 7f)) : 1f;
                _deadlinePill.rectTransform.localScale = new Vector3(s, s, 1f);
            }

            if (!_timerOn) return;
            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0)
            {
                _timeLeft = 0;
                _timerOn = false;
                RefreshHud();
                OnTimeout();
            }
            else RefreshHud();
        }

        // ================================================================
        //  状态栏（VS Code 底部那条蓝色栏）
        // ================================================================

        void BuildStatusBar()
        {
            var bar = UIKit.Panel(_screen, Accent, "StatusBar");
            bar.rectTransform.Box(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, StatusH));
            _statusLeft = UIKit.Label(bar.transform, "", 26, Color.white, TextAnchor.MiddleLeft, true);
            _statusLeft.rectTransform.Stretch(28, 0, 28, 0);
            _statusRight = UIKit.Label(bar.transform, "", 26, Color.white, TextAnchor.MiddleRight, true);
            _statusRight.rectTransform.Stretch(28, 0, 28, 0);
            RefreshStatus();
        }

        void RefreshStatus(string left = null)
        {
            if (_statusLeft == null) return;
            _statusLeft.text = left ?? ("main*   错误 0   警告 87");
            _statusRight.text = "工资 ¥" + SaveData.Salary + "    C    UTF-8";
        }

        // ================================================================
        //  标题 / 选关 / 需求 / 小卖部
        // ================================================================

        void ShowTitle()
        {
            NewPage();
            var col = Column(26, 80, 0);
            Spacer(col, 130);

            // Logo：Resources/GufaLogo.png，没有就画一个红印章
            var logoBox = UIKit.Node(col, "LogoBox");
            logoBox.H(380);
            if (UIKit.Logo != null)
            {
                var logo = UIKit.Panel(logoBox, Color.white, "Logo");
                logo.sprite = UIKit.Logo;
                logo.preserveAspect = true;
                logo.raycastTarget = false;
                logo.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 380));
                UITween.PopIn(logo.transform, 0.45f, 1.6f);
            }
            else
            {
                var seal = UIKit.Panel(logoBox, SealRed, "Seal", 28);
                seal.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, 320));
                seal.rectTransform.localEulerAngles = new Vector3(0, 0, 7);
                UIKit.Label(seal.transform, "编 古\n程 法", 96, Color.white, TextAnchor.MiddleCenter, false, true).rectTransform.Stretch();
                UITween.PopIn(seal.transform, 0.45f, 1.6f);
            }

            var title = AddLabel(col, "古法编程模拟器", 100, TextHi, 150, TextAnchor.MiddleCenter, false, true);
            var glow = title.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            glow.effectColor = UIKit.WithA(Green, 0.45f);
            glow.effectDistance = new Vector2(0, -5);

            var subRow = UIKit.Node(col, "SubRow");
            subRow.H(70);
            var sub = UIKit.Label(subRow, "// AI 时代，最后的手工程序员", 40, CommentC, TextAnchor.MiddleCenter, true);
            sub.rectTransform.Stretch();
            var cursor = UIKit.Panel(subRow, Green, "Cursor");
            cursor.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), new Vector2(330, 0), new Vector2(22, 46));
            UITween.Blink(cursor);

            Spacer(col, 40);
            var stats = UIKit.Node(col, "Stats");
            stats.H(190);
            UIKit.HList(stats.gameObject, 20);
            StatTile(stats, "¥" + SaveData.Salary, "工资余额", Gold);
            StatTile(stats, SaveData.TotalBugs.ToString(), "修好的 Bug", Green);
            StatTile(stats, SaveData.TotalHairLost.ToString(), "掉的头发", Red);

            Spacer(col, 40);
            AddButton(col, "开始上班", Accent, 150, () => ShowLevelSelect(), 52);
            AddButton(col, "工位小卖部", Surface3, 130, () => ShowShop(), 44);
            Flex(col);
            AddLabel(col, "没有 AI，没有自动补全，只有你和编译器。", 32, DimC, 60);
        }

        void StatTile(Transform parent, string value, string caption, Color color)
        {
            var tile = UIKit.Panel(parent, Surface2, "Tile", 22);
            var v = UIKit.Label(tile.transform, value, 54, color, TextAnchor.MiddleCenter, false, true);
            v.rectTransform.Box(new Vector2(0, 0.42f), Vector2.one, new Vector2(10, 0), new Vector2(-10, -14));
            v.resizeTextForBestFit = true;
            v.resizeTextMinSize = 28;
            v.resizeTextMaxSize = 54;
            var c = UIKit.Label(tile.transform, caption, 30, DimC);
            c.rectTransform.Box(Vector2.zero, new Vector2(1, 0.42f), new Vector2(10, 18), new Vector2(-10, 0));
        }

        void ShowLevelSelect()
        {
            NewPage();
            Header("第一章 · C 语言时代", () => ShowTitle());
            var col = Column(16, 50, 150);
            Spacer(col, 6);
            for (int i = 0; i < LevelDatabase.Levels.Length; i++) LevelCard(col, i);
            Flex(col);
        }

        void LevelCard(Transform parent, int i)
        {
            var lv = LevelDatabase.Levels[i];
            bool done = SaveData.IsCleared(i);
            bool open = i == 0 || SaveData.IsCleared(i - 1);
            bool next = open && !done;

            var card = UIKit.Panel(parent, open ? Surface2 : Surface, "Level", 22);
            card.H(126);
            if (next) UIKit.DropShadow(card, 6, 0.4f);
            int idx = i;
            var btn = UIKit.MakeButton(card, () => ShowDemand(idx));
            btn.interactable = open;

            var badge = UIKit.Panel(card.transform, done ? Green : next ? Accent : Border, "Badge", 40);
            badge.rectTransform.Pin(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(80, 80));
            UIKit.Label(badge.transform, (i + 1).ToString("00"), 34, done ? Dark : Color.white, TextAnchor.MiddleCenter, true, true)
                .rectTransform.Stretch();

            var t = UIKit.Label(card.transform, lv.Title, 40, open ? TextHi : DimC, TextAnchor.LowerLeft, false, true);
            t.rectTransform.Box(new Vector2(0, 0.5f), Vector2.one, new Vector2(132, 2), new Vector2(-210, -10));
            var sub = UIKit.Label(card.transform, lv.Stages.Length + " 个任务 · 工资 ¥" + lv.Reward, 28, DimC, TextAnchor.UpperLeft);
            sub.rectTransform.Box(Vector2.zero, new Vector2(1, 0.5f), new Vector2(132, 10), new Vector2(-210, -4));

            string tag = done ? "已上线" : next ? "开工" : "未解锁";
            Color tagBg = done ? UIKit.WithA(Green, 0.18f) : next ? Accent : Surface3;
            Color tagFg = done ? Green : next ? Color.white : DimC;
            var pill = UIKit.Panel(card.transform, tagBg, "Tag", 28);
            pill.rectTransform.Pin(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-26, 0), new Vector2(160, 58));
            pill.raycastTarget = false;
            UIKit.Label(pill.transform, tag, 30, tagFg, TextAnchor.MiddleCenter, false, true).rectTransform.Stretch();
        }

        void ShowDemand(int idx)
        {
            _levelIndex = idx;
            _pmChanges = 0;
            NewPage();
            var lv = CurLevel;
            Header("第" + (idx + 1) + "关 · " + lv.Title, () => ShowLevelSelect());
            var col = Column(26, 50, 150);
            Spacer(col, 40);

            // 产品经理头像 + 气泡
            var row = UIKit.Node(col, "PmRow");
            var hl = UIKit.HList(row.gameObject, 22, false, TextAnchor.UpperLeft);
            hl.childForceExpandHeight = false;
            var avatar = UIKit.Panel(row, Orange, "Avatar", 60);
            avatar.W(120).H(120);
            UIKit.Label(avatar.transform, "PM", 46, Dark, TextAnchor.MiddleCenter, true, true).rectTransform.Stretch();
            var bubble = UIKit.Panel(row, Surface2, "Bubble", 26);
            bubble.FlexW();
            UIKit.DropShadow(bubble, 6, 0.3f);
            UIKit.VList(bubble.gameObject, 14, 40, 40, 32, 40);
            AddLabel(bubble.transform, "产品经理", 34, Orange, 50, TextAnchor.MiddleLeft, false, true);
            AddLabel(bubble.transform, lv.Demand, 46, TextHi, -1, TextAnchor.UpperLeft);

            Spacer(col, 10);
            var info = UIKit.Node(col, "Info");
            info.H(170);
            UIKit.HList(info.gameObject, 18);
            StatTile(info, lv.Stages.Length.ToString(), "个任务", TextHi);
            StatTile(info, lv.TimePerStage.ToString("0") + "s", "每个限时", Yellow);
            StatTile(info, "¥" + lv.Reward, "本关工资", Gold);

            var perks = new StringBuilder();
            if (SaveData.Coffee > 0) perks.Append("咖啡 x" + SaveData.Coffee + "：开工自动喝一杯，每个任务 +10 秒");
            if (SaveData.HasDuck) perks.Append((perks.Length > 0 ? "\n" : "") + "橡皮鸭：本关可免费提示一次");
            if (perks.Length > 0) AddLabel(col, perks.ToString(), 32, Green, -1, TextAnchor.MiddleLeft);

            Flex(col);
            AddButton(col, "接需求，开干", Accent, 150, () => StartLevel(), 50);
            AddButton(col, "这需求做不了", Surface3, 120, () =>
                Popup(Orange, "产品经理", "这个需求很简单的，\n怎么实现我不管，今天下班前要。",
                    B("……好的", Accent, () => { _pmChanges++; StartLevel(); })), 40);
        }

        void ShowShop()
        {
            NewPage();
            Header("工位小卖部", () => ShowTitle());
            var col = Column(24, 50, 150);
            Spacer(col, 20);

            var wallet = UIKit.Node(col, "Wallet");
            wallet.H(170);
            UIKit.HList(wallet.gameObject, 18);
            StatTile(wallet, "¥" + SaveData.Salary, "工资余额", Gold);
            StatTile(wallet, SaveData.Coffee.ToString(), "咖啡库存", Orange);

            ShopItem(col, "速溶咖啡", "开工自动喝一杯\n本关每个任务 +10 秒", 60, () => { SaveData.Coffee++; });
            ShopItem(col, "橡皮鸭", SaveData.HasDuck ? "已拥有\n每关可免费提示一次" : "对着它讲一遍代码\n每关免费提示一次（永久）", 300,
                SaveData.HasDuck ? (Action)null : () => { SaveData.HasDuck = true; });

            Spacer(col, 10);
            AdButton(col, "摸鱼领工资 +¥80", "slack", 150, () =>
                Rewarded("slack", ok =>
                {
                    if (!ok) return;
                    SaveData.Salary += 80;
                    ShowShop();
                    Toast("摸鱼成功，工资 +¥80");
                }));
            Flex(col);
        }

        void ShopItem(Transform col, string itemName, string desc, int price, Action buy)
        {
            var row = UIKit.Panel(col, Surface2, "Item", 24);
            row.H(200);
            var icon = UIKit.Panel(row.transform, Surface3, "Icon", 24);
            icon.rectTransform.Pin(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(28, 0), new Vector2(120, 120));
            UIKit.Label(icon.transform, itemName.Substring(itemName.Length - 1), 56, buy == null ? Green : Gold, TextAnchor.MiddleCenter, false, true)
                .rectTransform.Stretch();

            var n = UIKit.Label(row.transform, itemName, 42, TextHi, TextAnchor.LowerLeft, false, true);
            n.rectTransform.Box(new Vector2(0, 0.55f), Vector2.one, new Vector2(176, 0), new Vector2(-260, -26));
            var d = UIKit.Label(row.transform, desc, 30, DimC, TextAnchor.UpperLeft);
            d.rectTransform.Box(Vector2.zero, new Vector2(1, 0.55f), new Vector2(176, 16), new Vector2(-260, -8));

            var b = UIKit.Btn(row.transform, buy == null ? "已拥有" : "¥" + price, buy == null ? Surface3 : Accent,
                buy == null ? DimC : Color.white, 40, null, false, TextAnchor.MiddleCenter, 30);
            b.GetComponent<RectTransform>().Pin(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-28, 0), new Vector2(200, 96));

            if (buy == null) { b.interactable = false; return; }
            b.onClick.AddListener(() =>
            {
                if (SaveData.Salary < price) { Toast("工资不够，先去上班吧"); UITween.Shake(b.GetComponent<RectTransform>()); return; }
                SaveData.Salary -= price;
                buy();
                ShowShop();
                Toast("买了" + itemName);
            });
        }

        // ================================================================
        //  关卡
        // ================================================================

        void StartLevel()
        {
            _stageIndex = 0;
            _hair = 100;
            _hairLost = 0;
            _bugsFixed = 0;
            _duckUsed = false;
            _bonusTime = 0;
            if (SaveData.Coffee > 0)
            {
                SaveData.Coffee--;
                _bonusTime = 10;
            }
            ShowStage();
            if (_bonusTime > 0) Toast("喝了一杯咖啡，每个任务 +10 秒");
        }

        void ShowStage()
        {
            NewPage();
            var lv = CurLevel;
            var st = CurStage;
            _hintUsed = false;

            BuildHud(lv);

            var col = Column(20, 40, 236, 24);

            // 任务说明
            AddLabel(col, st.Type == StageType.FindBug ? "找出有 Bug 的那一行，点它！" : "把空着的那一行补上",
                44, Gold, 70, TextAnchor.MiddleLeft, false, true);
            var quote = UIKit.Panel(col, Surface2, "Quote", 16);
            UIKit.VList(quote.gameObject, 0, 40, 30, 22, 22);
            var strip = UIKit.Panel(quote.transform, Orange, "Strip", 3);
            strip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            strip.rectTransform.Box(new Vector2(0, 0), new Vector2(0, 1), new Vector2(14, 16), new Vector2(20, -16));
            AddLabel(quote.transform, "测试 / 产品：" + st.Hint, 34, TextC, -1, TextAnchor.MiddleLeft);

            // 编辑器窗口
            var editor = UIKit.Panel(col, Editor, "Editor", 22);
            UIKit.DropShadow(editor, 10, 0.45f);
            UIKit.VList(editor.gameObject, 0, 0, 0, 0, 14);
            BuildEditorTab(editor.transform);
            var lines = UIKit.Node(editor.transform, "Lines");
            UIKit.VList(lines.gameObject, 4, 14, 14, 12, 0);
            for (int i = 0; i < st.Code.Length; i++) CodeLine(lines, st, i);

            // 选项
            if (st.Type == StageType.PickLine)
            {
                Spacer(col, 4);
                for (int j = 0; j < st.Options.Length; j++) OptionCard(col, st, j);
            }

            Flex(col);

            // 底部提示按钮
            var row = UIKit.Node(col, "Bottom");
            row.H(130);
            UIKit.HList(row.gameObject, 22);
            UIKit.Btn(row, "请教大佬 · 看广告", Accent, Color.white, 38, OnHint);
            if (SaveData.HasDuck)
            {
                Button duck = null;
                duck = UIKit.Btn(row, _duckUsed ? "橡皮鸭（本关已用）" : "橡皮鸭提示", Surface3, Gold, 38, () => OnDuck(duck));
                duck.interactable = !_duckUsed;
            }

            _timeLeft = lv.TimePerStage + _bonusTime;
            _timerOn = true;
            RefreshHud();
            RefreshStatus("main*   第" + (_levelIndex + 1) + "关   任务 " + (_stageIndex + 1) + "/" + lv.Stages.Length);
        }

        void BuildHud(Level lv)
        {
            var hud = UIKit.Panel(_page, Surface, "Hud");
            hud.rectTransform.TopBand(220);

            var title = UIKit.Label(hud.transform, "第" + (_levelIndex + 1) + "关 · " + lv.Title, 40, TextHi, TextAnchor.MiddleLeft, false, true);
            title.rectTransform.TopBand(72, 22, 40);

            var task = UIKit.Panel(hud.transform, Surface3, "Task", 26);
            task.rectTransform.Pin(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -28), new Vector2(190, 58));
            UIKit.Label(task.transform, "任务 " + (_stageIndex + 1) + "/" + lv.Stages.Length, 30, TextC, TextAnchor.MiddleCenter, false, true)
                .rectTransform.Stretch();

            _hairText = UIKit.Label(hud.transform, "", 34, TextC, TextAnchor.MiddleLeft);
            _hairText.rectTransform.TopBand(56, 108, 40);

            _deadlinePill = UIKit.Panel(hud.transform, HintBG, "Deadline", 30);
            _deadlinePill.rectTransform.Pin(new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-40, -136), new Vector2(270, 60));
            _deadlineText = UIKit.Label(_deadlinePill.transform, "", 32, Gold, TextAnchor.MiddleCenter, true, true);
            _deadlineText.rectTransform.Stretch();

            var barBg = UIKit.Panel(hud.transform, Dark, "HairBar", 12);
            barBg.rectTransform.TopBand(24, 176, 40);
            _hairFill = UIKit.Panel(barBg.transform, Green, "Fill", 12);
            _hairFill.rectTransform.Stretch();
        }

        void BuildEditorTab(Transform editor)
        {
            var tab = UIKit.Node(editor, "TabBar");
            tab.H(66);
            var bg = UIKit.Panel(tab, TabBar, "Bg", 22);
            bg.rectTransform.Stretch();
            var cover = UIKit.Panel(tab, TabBar, "Cover"); // 盖住下半部分圆角，只留上面两个角
            cover.rectTransform.Box(Vector2.zero, new Vector2(1, 0.5f), Vector2.zero, Vector2.zero);

            Color[] dots = { UIKit.Hex("#FF5F57"), UIKit.Hex("#FEBC2E"), UIKit.Hex("#28C840") };
            for (int i = 0; i < 3; i++)
            {
                var d = UIKit.Panel(tab, dots[i], "Dot", 9);
                d.rectTransform.Pin(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(28 + i * 34, 0), new Vector2(18, 18));
            }
            var fileTab = UIKit.Panel(tab, Editor, "File");
            fileTab.rectTransform.Box(new Vector2(0, 0), new Vector2(0, 1), new Vector2(140, 0), new Vector2(330, -10));
            UIKit.Label(fileTab.transform, "main.c", 28, TextC, TextAnchor.MiddleCenter, true).rectTransform.Stretch();
            var accent = UIKit.Panel(fileTab.transform, Accent, "Accent");
            accent.rectTransform.TopBand(4);
        }

        void CodeLine(Transform parent, Stage st, int i)
        {
            bool blank = st.Type == StageType.PickLine && st.Code[i].Contains("____");
            var row = UIKit.Panel(parent, blank ? BlankBG : UIKit.WithA(Color.white, 0f), "Line", 12);
            row.H(86);
            if (st.Type == StageType.FindBug)
            {
                int idx = i;
                var b = UIKit.MakeButton(row, () => OnLineTapped(idx));
                b.GetComponent<Pressable>().pressedScale = 0.985f;
                _lineImgs.Add(row);
            }

            var num = UIKit.Label(row.transform, (i + 1).ToString(), 32, Gutter, TextAnchor.MiddleRight, true);
            num.rectTransform.Box(Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(56, 0));
            var code = UIKit.Label(row.transform, CodeHighlighter.Highlight(st.Code[i]), 37, TextC, TextAnchor.MiddleLeft, true, false, true);
            code.rectTransform.Stretch(84, 0, 16, 0);
        }

        void OptionCard(Transform parent, Stage st, int j)
        {
            var card = UIKit.Panel(parent, Surface2, "Option", 20);
            card.H(100);
            UIKit.DropShadow(card, 5, 0.3f);
            int idx = j;
            UIKit.MakeButton(card, () => OnOptionTapped(idx));
            _optionImgs.Add(card);

            var badge = UIKit.Panel(card.transform, Surface3, "Letter", 24);
            badge.raycastTarget = false;
            badge.rectTransform.Pin(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(60, 60));
            UIKit.Label(badge.transform, ((char)('A' + j)).ToString(), 32, Green, TextAnchor.MiddleCenter, true, true).rectTransform.Stretch();

            var code = UIKit.Label(card.transform, CodeHighlighter.Highlight(st.Options[j]), 34, TextC, TextAnchor.MiddleLeft, true, false, true);
            code.rectTransform.Stretch(104, 0, 24, 0);
        }

        void RefreshHud()
        {
            if (_hairText != null) _hairText.text = "发量  " + _hair + " 根";
            if (_hairFill != null)
            {
                float p = Mathf.Clamp01(_hair / 100f);
                _hairFill.enabled = p > 0.03f;
                _hairFill.rectTransform.anchorMax = new Vector2(p, 1);
                _hairFill.color = _hair > 40 ? Green : Red;
            }
            if (_deadlineText != null)
            {
                bool urgent = _timeLeft <= 5f;
                _deadlineText.text = "Deadline " + Mathf.CeilToInt(_timeLeft) + "s";
                _deadlineText.color = urgent ? Color.white : Gold;
                _deadlinePill.color = urgent ? Red : HintBG;
            }
        }

        void OnLineTapped(int i)
        {
            if (!_timerOn) return;
            if (i == CurStage.Answer) Solve(_lineImgs[i]);
            else Miss(_lineImgs[i]);
        }

        void OnOptionTapped(int i)
        {
            if (!_timerOn) return;
            if (i == CurStage.Answer) Solve(_optionImgs[i]);
            else Miss(_optionImgs[i]);
        }

        void Miss(Image img)
        {
            StartCoroutine(Flash(img, BadBG));
            UITween.Shake(img.rectTransform);
            Toast(MissQuips[UnityEngine.Random.Range(0, MissQuips.Length)] + " 掉了 20 根头发");
            LoseHair(20);
        }

        void Solve(Image img)
        {
            _timerOn = false;
            img.color = OkBG;
            _bugsFixed++;
            SaveData.TotalBugs++;

            string body = CurStage.Explain;
            bool more = _stageIndex < CurLevel.Stages.Length - 1;
            if (more && UnityEngine.Random.value < PmChangeChance)
            {
                _pmChanges++;
                body += "\n\n产品经理：" + PmQuips[UnityEngine.Random.Range(0, PmQuips.Length)];
            }
            Popup(Green, "修好了！", body, B(more ? "下一个任务" : "提交，开始编译", Green, () => NextStage(), Dark));
        }

        void NextStage()
        {
            _stageIndex++;
            if (_stageIndex < CurLevel.Stages.Length) ShowStage();
            else StartCompile();
        }

        void LoseHair(int n)
        {
            _hair = Mathf.Max(0, _hair - n);
            _hairLost += n;
            SaveData.TotalHairLost += n;
            RefreshHud();
            if (_hairText != null) UITween.Punch(_hairText.transform, 1.15f, 0.3f);
            if (_hair <= 0)
            {
                _timerOn = false;
                Bald();
            }
        }

        void Resume(float seconds)
        {
            _timeLeft = Mathf.Max(_timeLeft, seconds);
            _timerOn = true;
            RefreshHud();
        }

        // —— 广告位：找老板延期 ——
        void OnTimeout()
        {
            Popup(Red, "Deadline 到了！", "老板已经在你身后站了五分钟。",
                AdB("找老板延期 +20 秒", "delay", () => Rewarded("delay", ok => { if (ok) Resume(20); else OnTimeout(); })),
                B("硬扛（掉 30 根头发，+15 秒）", DangerBtn, () => { LoseHair(30); if (_hair > 0) Resume(15); }));
        }

        // —— 广告位：植发复活 ——
        void Bald()
        {
            Popup(Red, "你秃了", "头发掉光了。强者诞生了，但需求还没做完。",
                AdB("去植发，原地复活", "revive", () => Rewarded("revive", ok =>
                {
                    if (!ok) { Bald(); return; }
                    _hair = 60;
                    Resume(15);
                    Toast("植发成功，发量 +60");
                })),
                B("回家种地（放弃本关）", DangerBtn, () => ShowLevelSelect()));
        }

        // —— 广告位：请教大佬（提示）——
        void OnHint()
        {
            if (!_timerOn) return;
            if (_hintUsed) { Toast("大佬已经指点过了，再问要挨骂了"); return; }
            _timerOn = false; // 看广告期间暂停 Deadline
            Rewarded("hint", ok =>
            {
                _timerOn = true;
                if (!ok) return;
                _hintUsed = true;
                ApplyHint();
                Toast("大佬瞄了一眼：「这不是很明显吗？」");
            });
        }

        void OnDuck(Button b)
        {
            if (!_timerOn || _duckUsed) return;
            _duckUsed = true;
            if (b != null) b.interactable = false;
            ApplyHint();
            Toast("你对着橡皮鸭把代码讲了一遍，突然就悟了。");
        }

        void ApplyHint()
        {
            var list = CurStage.Type == StageType.FindBug ? _lineImgs : _optionImgs;
            int a = CurStage.Answer;
            if (a < 0 || a >= list.Count) return;
            list[a].color = HintBG;
            UITween.Shake(list[a].rectTransform, 8f, 0.4f);
        }

        IEnumerator Flash(Image img, Color c)
        {
            var back = img.color;
            img.color = c;
            yield return new WaitForSeconds(0.4f);
            if (img != null && img.color == c) img.color = back;
        }

        // ================================================================
        //  编译（插屏广告位）/ 线上事故 / 结算 / 终章
        // ================================================================

        void StartCompile()
        {
            _timerOn = false;
            _levelsSinceInterstitial++;
            if (_levelIndex >= InterstitialFromLevel && _levelsSinceInterstitial >= InterstitialEvery)
            {
                _levelsSinceInterstitial = 0;
                Ads.ShowInterstitial("compile", () => StartCoroutine(CompileRoutine()));
            }
            else StartCoroutine(CompileRoutine());
        }

        IEnumerator CompileRoutine()
        {
            NewPage();
            RefreshStatus("main*   正在编译……");
            var col = Column(28, 50, 0);
            Spacer(col, 220);
            AddLabel(col, "编译中", 76, Green, 120, TextAnchor.MiddleCenter, false, true);

            var barBg = UIKit.Panel(col, Dark, "Bar", 16);
            barBg.H(32);
            var fill = UIKit.Panel(barBg.transform, Green, "Fill", 16);
            var fr = fill.rectTransform;
            fr.Stretch();
            fr.anchorMax = new Vector2(0, 1);
            fill.enabled = false;

            // 终端窗口
            var term = UIKit.Panel(col, Editor, "Terminal", 22);
            term.H(560);
            UIKit.DropShadow(term, 10, 0.45f);
            var termHead = UIKit.Panel(term.transform, TabBar, "Head", 22);
            termHead.rectTransform.TopBand(60);
            UIKit.Panel(termHead.transform, TabBar, "Cover").rectTransform.Box(Vector2.zero, new Vector2(1, 0.5f), Vector2.zero, Vector2.zero);
            UIKit.Label(termHead.transform, "bash — gcc", 26, DimC, TextAnchor.MiddleCenter, true).rectTransform.Stretch();
            var log = UIKit.Label(term.transform, "", 32, TextC, TextAnchor.UpperLeft, true, false, true);
            log.rectTransform.Stretch(34, 24, 34, 88);
            Flex(col);

            const float dur = 2.4f;
            float t = 0;
            int shown = 0;
            var sb = new StringBuilder();
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / dur);
                fill.enabled = p > 0.04f;
                fr.anchorMax = new Vector2(p, 1);
                int want = Mathf.Min(CompileLogs.Length, 1 + (int)(p * CompileLogs.Length));
                while (shown < want)
                {
                    string l = CompileLogs[shown++];
                    if (l.StartsWith("$")) sb.Append("<color=#4EC9B0>").Append(l).Append("</color>\n");
                    else if (l.StartsWith("warning")) sb.Append("<color=#CCA700>").Append(l).Append("</color>\n");
                    else if (l.StartsWith("Build")) sb.Append("<color=#89D185>").Append(l).Append("</color>\n");
                    else sb.Append(l).Append('\n');
                }
                log.text = sb.ToString() + ((int)(Time.unscaledTime * 3f) % 2 == 0 ? "_" : "");
                yield return null;
            }
            yield return new WaitForSeconds(0.4f);

            int reward = CurLevel.Reward;
            if (UnityEngine.Random.value < IncidentChance) Incident(reward);
            else ShowSettle(reward, false);
        }

        // —— 广告位：紧急回滚 ——
        void Incident(int reward)
        {
            Popup(Red, "线上事故！", "刚上线服务器就挂了，用户投诉电话被打爆。\n老板：谁上的线？",
                AdB("紧急回滚，免扣工资", "rollback", () => Rewarded("rollback", ok => { if (ok) ShowSettle(reward, false); else Incident(reward); })),
                B("背锅（工资减半）", DangerBtn, () => ShowSettle(reward / 2, true)));
        }

        // —— 广告位：加班费 x2 ——
        void ShowSettle(int reward, bool blamed)
        {
            NewPage();
            SaveData.SetCleared(_levelIndex);
            bool isLast = _levelIndex >= LevelDatabase.Levels.Length - 1;

            var col = Column(24, 60, 0);
            Spacer(col, 140);
            AddLabel(col, blamed ? "上线了（背了个锅）" : "需求已上线", 72, blamed ? Orange : Green, 110, TextAnchor.MiddleCenter, false, true);
            var money = AddLabel(col, "本关工资  ¥" + reward + (blamed ? "（扣了一半）" : ""), 46, Gold, 80, TextAnchor.MiddleCenter, false, true);
            UITween.Punch(money.transform, 1.15f, 0.4f);

            // 分享用的「日报卡」，后面可以接 wx.shareAppMessage 生成分享图
            var card = UIKit.Panel(col, Surface2, "Card", 26);
            UIKit.DropShadow(card, 10, 0.4f);
            UIKit.VList(card.gameObject, 10, 50, 50, 40, 44);
            AddLabel(card.transform, "古法程序员日报", 42, TextHi, 70, TextAnchor.MiddleLeft, false, true);
            AddLabel(card.transform, "// " + DateTime.Now.ToString("yyyy-MM-dd") + "   第" + (_levelIndex + 1) + "关 · " + CurLevel.Title, 28, CommentC, 46, TextAnchor.MiddleLeft, true);
            ReportRow(card.transform, "掉发", _hairLost + " 根", Red);
            ReportRow(card.transform, "修好的 Bug", _bugsFixed + " 个", Green);
            ReportRow(card.transform, "产品改需求", _pmChanges + " 次", Orange);
            ReportRow(card.transform, "今日称号", HairRank(_hairLost), Gold);
            if (UIKit.Logo != null)
            {
                var stamp = UIKit.Panel(card.transform, Color.white, "Stamp");
                stamp.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                stamp.sprite = UIKit.Logo;
                stamp.preserveAspect = true;
                stamp.raycastTarget = false;
                stamp.rectTransform.Pin(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -20), new Vector2(170, 170));
                stamp.color = UIKit.WithA(Color.white, 0.9f);
                UITween.PopIn(stamp.transform, 0.35f, 1.8f);
            }

            Spacer(col, 30);
            bool claimed = false;
            AdButton(col, "申请加班费，工资 x2", "double", 150, () =>
            {
                if (claimed) return;
                Rewarded("double", ok =>
                {
                    if (!ok || claimed) return;
                    claimed = true;
                    Claim(reward * 2, isLast);
                });
            });
            AddButton(col, "领工资 ¥" + reward, Surface3, 120, () =>
            {
                if (claimed) return;
                claimed = true;
                Claim(reward, isLast);
            }, 42);
            Flex(col);
        }

        void ReportRow(Transform parent, string key, string value, Color valueColor)
        {
            var row = UIKit.Node(parent, "Row");
            row.H(64);
            var k = UIKit.Label(row, key, 34, DimC, TextAnchor.MiddleLeft);
            k.rectTransform.Stretch();
            var v = UIKit.Label(row, value, 38, valueColor, TextAnchor.MiddleRight, false, true);
            v.rectTransform.Stretch();
            var line = UIKit.Panel(row, Border, "Divider");
            line.rectTransform.Box(Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 2));
        }

        void Claim(int amount, bool isLast)
        {
            SaveData.Salary += amount;
            if (isLast) ShowEnding();
            else ShowLevelSelect();
            Toast("工资 +¥" + amount);
        }

        static string HairRank(int lost)
        {
            if (lost <= 0) return "发量王者";
            if (lost <= 40) return "轻微脱发";
            if (lost <= 100) return "发际线后移";
            return "地中海预备役";
        }

        void ShowEnding()
        {
            NewPage();
            RefreshStatus("main   已合并到 AI 分支");
            var col = Column(30, 80, 0);
            Spacer(col, 200);
            AddLabel(col, "终章", 84, Gold, 130, TextAnchor.MiddleCenter, false, true);
            AddLabel(col, "第二天，公司接入了 AI。\n\n你熬了十个通宵写的代码，\n它三秒就写完了，还带注释。\n\n你默默合上了那本翻烂的《C 程序设计》。",
                42, TextC, -1);
            Spacer(col, 40);
            AddLabel(col, "// 古法编程，后继无人", 44, CommentC, 80, TextAnchor.MiddleCenter, true);
            Spacer(col, 60);
            AddButton(col, "回到工位", Accent, 140, () => ShowTitle(), 46);
            Flex(col);
        }

        // ================================================================
        //  广告封装
        // ================================================================

        int AdLeft(string placement)
        {
            int cap;
            if (!AdCaps.TryGetValue(placement, out cap)) cap = 99;
            return Mathf.Max(0, cap - SaveData.GetDaily(placement));
        }

        void Rewarded(string placement, Action<bool> done)
        {
            if (AdLeft(placement) <= 0)
            {
                Toast("今天的次数用完了，明天再来");
                done(false);
                return;
            }
            Ads.ShowRewarded(placement, ok =>
            {
                if (ok) SaveData.AddDaily(placement);
                else Toast("广告没看完，奖励飞了");
                done(ok);
            });
        }

        /// <summary>激励视频按钮：主文案 + 右侧「看广告 · 剩 N 次」小标签。</summary>
        Button AdButton(Transform parent, string label, string placement, float h, UnityAction onClick)
        {
            var b = UIKit.Btn(parent, label, Accent, Color.white, 42, onClick, false, TextAnchor.MiddleLeft, 22);
            b.H(h);
            b.GetComponentInChildren<Text>().rectTransform.Stretch(44, 4, 260, 4);
            AddAdTag(b.transform, placement);
            return b;
        }

        void AddAdTag(Transform button, string placement)
        {
            var tag = UIKit.Panel(button, UIKit.WithA(Color.white, 0.18f), "AdTag", 26);
            tag.raycastTarget = false;
            tag.rectTransform.Pin(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-28, 0), new Vector2(220, 56));
            UIKit.Label(tag.transform, "看广告 · 剩" + AdLeft(placement) + "次", 26, Color.white, TextAnchor.MiddleCenter, false, true)
                .rectTransform.Stretch();
        }

        // ================================================================
        //  UI 小工具
        // ================================================================

        /// <summary>换一页：销毁旧页面，新建一个带淡入动画的空页面。</summary>
        void NewPage()
        {
            _timerOn = false;
            ClosePopup();
            if (_page != null) Destroy(_page.gameObject);
            _hairText = null;
            _deadlineText = null;
            _deadlinePill = null;
            _hairFill = null;
            _lineImgs.Clear();
            _optionImgs.Clear();

            _page = UIKit.Node(_screen, "Page").Stretch(0, StatusH, 0, 0);
            _page.SetAsFirstSibling(); // 状态栏在上层
            UITween.FadeUp(_page, 0.22f, 30f);
            RefreshStatus();
        }

        RectTransform Column(float spacing, int pad, float top, int bottom = 36)
        {
            var rt = UIKit.Node(_page, "Column").Stretch(0, 0, 0, top);
            UIKit.VList(rt.gameObject, spacing, pad, pad, 30, bottom);
            return rt;
        }

        void Header(string title, Action back)
        {
            var bar = UIKit.Panel(_page, Surface, "Header");
            bar.rectTransform.TopBand(130);
            var line = UIKit.Panel(bar.transform, Border, "Line");
            line.rectTransform.Box(Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 2));
            var t = UIKit.Label(bar.transform, title, 44, TextHi, TextAnchor.MiddleCenter, false, true);
            t.rectTransform.Stretch(220, 0, 220, 0);
            if (back == null) return;
            var b = UIKit.Btn(bar.transform, "< 返回", Surface3, TextC, 34, () => back(), false, TextAnchor.MiddleCenter, 30, false);
            b.GetComponent<RectTransform>().Pin(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(180, 74));
        }

        Text AddLabel(Transform p, string s, int size, Color c, float h = -1,
            TextAnchor align = TextAnchor.MiddleCenter, bool mono = false, bool bold = false)
        {
            var t = UIKit.Label(p, s, size, c, align, mono, bold);
            if (h > 0) t.H(h);
            return t;
        }

        Button AddButton(Transform p, string s, Color bg, float h, UnityAction onClick, int size = 44, Color? fg = null)
        {
            var b = UIKit.Btn(p, s, bg, fg ?? Color.white, size, onClick, false, TextAnchor.MiddleCenter, 22);
            b.H(h);
            return b;
        }

        void Spacer(Transform p, float h) { UIKit.Node(p, "Space").H(h); }

        void Flex(Transform p) { UIKit.Node(p, "Flex").Flex(); }

        // —— 弹窗 ——
        class PopupBtn
        {
            public string Label;
            public Color Color;
            public Color Fg;
            public Action Action;
            public string AdPlacement; // 非空时显示「看广告 · 剩 N 次」
        }

        static PopupBtn B(string label, Color color, Action action, Color? fg = null)
        {
            return new PopupBtn { Label = label, Color = color, Fg = fg ?? Color.white, Action = action };
        }

        PopupBtn AdB(string label, string placement, Action action)
        {
            return new PopupBtn { Label = label, Color = Accent, Fg = Color.white, Action = action, AdPlacement = placement };
        }

        void Popup(string title, string body, params PopupBtn[] buttons)
        {
            Popup(Gold, title, body, buttons);
        }

        void Popup(Color titleColor, string title, string body, params PopupBtn[] buttons)
        {
            ClosePopup();
            var dim = UIKit.Panel(_popupLayer, new Color(0, 0, 0, 0.72f), "PopupDim");
            dim.rectTransform.Stretch();
            UITween.Fade(dim.gameObject.AddComponent<CanvasGroup>(), 0f, 1f, 0.15f);

            var box = UIKit.Panel(dim.transform, Surface, "PopupBox", 32);
            UIKit.DropShadow(box, 16, 0.5f);
            var rt = box.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(920, 0);
            UIKit.VList(box.gameObject, 22, 50, 50, 56, 50);
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 顶部彩条
            var strip = UIKit.Panel(box.transform, titleColor, "Strip", 4);
            strip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            strip.rectTransform.Pin(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(120, 8));

            AddLabel(box.transform, title, 58, titleColor, 90, TextAnchor.MiddleCenter, false, true);
            if (!string.IsNullOrEmpty(body)) AddLabel(box.transform, body, 38, TextC, -1, TextAnchor.MiddleLeft);
            Spacer(box.transform, 6);
            foreach (var pb in buttons)
            {
                var p = pb;
                UnityAction click = () =>
                {
                    ClosePopup();
                    if (p.Action != null) p.Action();
                };
                if (!string.IsNullOrEmpty(p.AdPlacement))
                {
                    var b = UIKit.Btn(box.transform, p.Label, p.Color, p.Fg, 38, click, false, TextAnchor.MiddleLeft, 22);
                    b.H(124);
                    b.GetComponentInChildren<Text>().rectTransform.Stretch(40, 4, 250, 4);
                    AddAdTag(b.transform, p.AdPlacement);
                }
                else
                {
                    var b = UIKit.Btn(box.transform, p.Label, p.Color, p.Fg, 38, click, false, TextAnchor.MiddleCenter, 22);
                    b.H(124);
                }
            }
            UITween.PopIn(box.transform, 0.28f, 0.82f);
            _popup = dim.gameObject;
        }

        void ClosePopup()
        {
            if (_popup != null) Destroy(_popup);
            _popup = null;
        }

        // —— Toast ——
        void BuildToast(Transform canvas)
        {
            var bg = UIKit.Panel(canvas, new Color(0.06f, 0.06f, 0.07f, 0.94f), "Toast", 44);
            bg.raycastTarget = false;
            UIKit.DropShadow(bg, 8, 0.4f);
            _toastRt = bg.rectTransform;
            _toastRt.Pin(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 300), new Vector2(900, 100)); // 底部按钮上方，不挡 HUD 和代码
            _toast = UIKit.Label(bg.transform, "", 34, Color.white, TextAnchor.MiddleCenter, false, true);
            _toast.rectTransform.Stretch(40, 6, 40, 6);
            _toastGroup = bg.gameObject.AddComponent<CanvasGroup>();
            _toastGroup.alpha = 0;
            _toastGroup.blocksRaycasts = false;
            _toastGroup.interactable = false;
        }

        void Toast(string msg)
        {
            _toast.text = msg;
            if (_toastCo != null) StopCoroutine(_toastCo);
            _toastCo = StartCoroutine(ToastRoutine());
        }

        IEnumerator ToastRoutine()
        {
            float e = 0f;
            while (e < 0.18f)
            {
                e += Time.unscaledDeltaTime;
                float p = UITween.EaseOut(Mathf.Clamp01(e / 0.18f));
                _toastGroup.alpha = p;
                _toastRt.anchoredPosition = new Vector2(0, 260 + 40 * p);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1.6f);
            for (float a = 1; a > 0; a -= Time.unscaledDeltaTime * 4f)
            {
                _toastGroup.alpha = a;
                yield return null;
            }
            _toastGroup.alpha = 0;
        }
    }
}
