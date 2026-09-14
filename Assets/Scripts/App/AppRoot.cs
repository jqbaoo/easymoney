using System.IO;
using EasyMoney.App.UI;
using EasyMoney.App.UI.Pages;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasyMoney.App
{
    /// <summary>
    /// 应用入口。负责搭出界面骨架（安全区 → 标题栏 / 内容区 / 标签栏）并装配页面路由。
    ///
    /// 原型阶段用 RuntimeInitializeOnLoadMethod 自动启动，不需要往场景里挂任何东西，
    /// 打开任意场景点 Play 即可看到界面。接入正式流程后可改为场景内挂载。
    /// </summary>
    public sealed class AppRoot : MonoBehaviour
    {
        private static readonly (string Key, string Icon, string Label)[] TABS =
        {
            ("record", IconNames.TAB_RECORD, "记一笔"),
            ("list", IconNames.TAB_LIST, "账单"),
            ("account", IconNames.TAB_ACCOUNT, "账户"),
            ("report", IconNames.TAB_REPORT, "报表")
        };

        private AppContext m_Context;
        private PageRouter m_Router;
        private TabBar m_TabBar;
        private Text m_HeaderTitle;
        private RectTransform m_ContentArea;
        private RectTransform m_CanvasRoot;
        private string m_CurrentKey = TABS[0].Key;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void _autoBoot()
        {
#pragma warning disable CS0618
            if (FindObjectOfType<AppRoot>() != null)
            {
                return;
            }
#pragma warning restore CS0618

            GameObject oGo = new GameObject("[AppRoot]");
            oGo.AddComponent<AppRoot>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;

            _openDatabase();

            _ensureEventSystem();

            _buildSkeleton();
            _buildPages();

            m_Router.Show(m_CurrentKey);
        }

        private void OnDestroy()
        {
            if (m_Context == null)
            {
                return;
            }

            m_Context.DataChanged -= _onDataChanged;
            m_Context.Dispose();
            m_Context = null;
        }

        /// <summary>
        /// 打开数据库并装配仓储。库文件放 persistentDataPath——
        /// Android 上这是唯一可写且不会被系统清理的目录。
        /// </summary>
        private void _openDatabase()
        {
            m_Context = new AppContext();
            m_Context.Initialize(Path.Combine(Application.persistentDataPath, "easymoney.db"));
            m_Context.DataChanged += _onDataChanged;
        }

        /// <summary>
        /// 数据变了就重走当前页的 OnShow。只刷当前页即可：隐藏的页面切回去时
        /// PageRouter 会再调一次 OnShow，那时自然读到最新数据。
        /// </summary>
        private void _onDataChanged()
        {
            if (m_Router != null)
            {
                m_Router.RefreshCurrent();
            }
        }

        private void OnEnable()
        {
            Theme.PaletteChanged += _onPaletteChanged;
        }

        private void OnDisable()
        {
            Theme.PaletteChanged -= _onPaletteChanged;
        }

        /// <summary>
        /// 换配色后重建整棵界面树。颜色是在构建时写进每个 Graphic 的，
        /// 与其维护一套「遍历所有节点改色」的增量逻辑（漏一个就是脏界面），
        /// 不如整体重建——换肤不是高频操作，这点开销无所谓。
        /// </summary>
        private void _onPaletteChanged()
        {
            if (m_CanvasRoot != null)
            {
                m_CanvasRoot.gameObject.SetActive(false);
                Destroy(m_CanvasRoot.gameObject);
            }

            m_Router = null;
            m_TabBar = null;
            m_HeaderTitle = null;
            m_ContentArea = null;

            _buildSkeleton();
            _buildPages();

            m_Router.Show(m_CurrentKey);
        }

        // ── 骨架 ────────────────────────────────────

        private void _buildSkeleton()
        {
            RectTransform oCanvas = _createCanvas();
            m_CanvasRoot = oCanvas;

            // 全局背景垫在最底层，页面内容都盖在它上面。
            //
            // ⚠️ 必须挂在 Canvas 下，不能挂进 SafeArea：SafeArea 会让开底部导航栏，
            // 背景跟着一起缩的话导航栏那一条就没人画了，露出相机的清屏色——
            // SampleScene 里是块蓝灰，比内容被挡还难看，而且会让人以为安全区算错了。
            // 先建 = 先绘制 = 在下层，所以它要建在 SafeArea 之前
            Image oBackground = UiFactory.CreatePanel(oCanvas, "Background", Theme.BACKGROUND);
            UiFactory.Stretch(oBackground.rectTransform);

            // 安全区：刘海屏与全面屏手机上内缩，避开刘海与系统栏。
            // 底部让开 Android 导航栏那一步见 SafeAreaFitter / SafeAreaLayout
            RectTransform oSafeArea = UiFactory.CreateNode(oCanvas, "SafeArea");
            UiFactory.Stretch(oSafeArea);
            oSafeArea.gameObject.AddComponent<SafeAreaFitter>();

            _buildHeader(oSafeArea);

            m_ContentArea = UiFactory.CreateNode(oSafeArea, "ContentArea");
            UiFactory.StretchWithInsets(m_ContentArea, Theme.HEADER_HEIGHT, Theme.TABBAR_HEIGHT);

            m_TabBar = new TabBar();
            m_TabBar.Build(oSafeArea, TABS);
            m_TabBar.TabClicked += sKey => m_Router.Show(sKey);
        }

        private static RectTransform _createCanvas()
        {
            GameObject oCanvasGo = new GameObject("Canvas", typeof(RectTransform));

            Canvas oCanvas = oCanvasGo.AddComponent<Canvas>();
            oCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler oScaler = oCanvasGo.AddComponent<CanvasScaler>();
            oScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            oScaler.referenceResolution = new Vector2(Theme.REF_WIDTH, Theme.REF_HEIGHT);
            oScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            // 0 = 按宽度匹配。竖屏应用的宽度恒定，高度随屏幕比例伸缩，
            // 这样在 18:9 / 20:9 的机器上内容不会被横向拉伸。
            oScaler.matchWidthOrHeight = 0f;

            oCanvasGo.AddComponent<GraphicRaycaster>();

            return oCanvasGo.GetComponent<RectTransform>();
        }

        private void _buildHeader(RectTransform oSafeArea)
        {
            Image oHeader = UiFactory.CreatePanel(oSafeArea, "Header", Theme.SURFACE);
            UiFactory.AnchorTop(oHeader.rectTransform, Theme.HEADER_HEIGHT);

            m_HeaderTitle = UiFactory.CreateText(oHeader.transform, "Title", "EasyMoney",
                Theme.FONT_TITLE, TextAnchor.MiddleCenter, null, Theme.WEIGHT_TITLE);
            UiFactory.Stretch(m_HeaderTitle.rectTransform);

            Image oDivider = UiFactory.CreatePanel(oHeader.transform, "Divider", Theme.DIVIDER);
            oDivider.rectTransform.anchorMin = new Vector2(0f, 0f);
            oDivider.rectTransform.anchorMax = new Vector2(1f, 0f);
            oDivider.rectTransform.pivot = new Vector2(0.5f, 0f);
            oDivider.rectTransform.anchoredPosition = Vector2.zero;
            oDivider.rectTransform.sizeDelta = new Vector2(0f, Theme.DIVIDER_HEIGHT);
        }

        // ── 页面 ────────────────────────────────────

        private void _buildPages()
        {
            m_Router = new PageRouter();

            _register(TABS[0].Key, new RecordPage());
            _register(TABS[1].Key, new TransactionListPage());
            _register(TABS[2].Key, new AccountPage());
            _register(TABS[3].Key, new ReportPage());

            m_Router.PageChanged += _onPageChanged;
        }

        private void _register(string sKey, PageBase oPage)
        {
            RectTransform oRoot = UiFactory.CreateNode(m_ContentArea, $"Page_{sKey}");
            UiFactory.Stretch(oRoot);

            oPage.Attach(oRoot);
            m_Router.Register(sKey, oPage);
        }

        private void _onPageChanged(string sKey)
        {
            m_CurrentKey = sKey;
            m_TabBar.SetSelected(sKey);
            m_HeaderTitle.text = _titleOf(sKey);
        }

        private static string _titleOf(string sKey)
        {
            foreach ((string sEachKey, string sIcon, string sLabel) in TABS)
            {
                if (sEachKey == sKey)
                {
                    return sLabel;
                }
            }

            return "EasyMoney";
        }

        private static void _ensureEventSystem()
        {
#pragma warning disable CS0618
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }
#pragma warning restore CS0618

            GameObject oEventSystem = new GameObject("EventSystem");
            oEventSystem.AddComponent<EventSystem>();
            oEventSystem.AddComponent<StandaloneInputModule>();
        }
    }
}
