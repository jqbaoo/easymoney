using EasyMoney.App.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.Tests
{
    /// <summary>
    /// UiFactory 的尺寸契约测试。App 层此前完全没有测试覆盖，界面只能靠 Play 肉眼验，
    /// 「滚动区默认缩成 100x100」这种毛病就一直没人发现——直到弹窗里的文字被裁掉。
    ///
    /// 这里只测锚点能决定的尺寸：RectTransform 的 rect 由锚点和父容器即时算出，
    /// 不需要 Canvas，也不需要 LayoutRebuilder，所以 EditMode 下跑得动、也不飘。
    /// 布局组（LayoutGroup）算出来的高度依赖一次真实的布局重建，这里不碰。
    /// </summary>
    public class UiFactoryLayoutTests
    {
        private const float PARENT_WIDTH = 600f;
        private const float PARENT_HEIGHT = 800f;

        private GameObject m_Root;
        private RectTransform m_Parent;

        [SetUp]
        public void SetUp()
        {
            m_Root = new GameObject("TestRoot", typeof(RectTransform));
            m_Parent = m_Root.GetComponent<RectTransform>();
            m_Parent.sizeDelta = new Vector2(PARENT_WIDTH, PARENT_HEIGHT);
        }

        [TearDown]
        public void TearDown()
        {
            // EditMode 下 GameObject 不会随场景销毁，必须手动清，
            // 否则会在编辑器里越积越多
            Object.DestroyImmediate(m_Root);
        }

        [Test]
        public void CreateScroll_FillsItsParent()
        {
            ScrollRect oScroll = UiFactory.CreateScroll(m_Parent, "Scroll", out RectTransform _);

            RectTransform oScrollRect = oScroll.GetComponent<RectTransform>();
            Assert.AreEqual(PARENT_WIDTH, oScrollRect.rect.width, 0.01f,
                "滚动区没有铺满父容器宽度，内容会被 Viewport 的 RectMask2D 裁掉");
            Assert.AreEqual(PARENT_HEIGHT, oScrollRect.rect.height, 0.01f,
                "滚动区没有铺满父容器高度");
        }

        [Test]
        public void CreateScroll_InsetsContentByPagePadding()
        {
            UiFactory.CreateScroll(m_Parent, "Scroll", out RectTransform oContent);

            Assert.AreEqual(PARENT_WIDTH - Theme.PAGE_PADDING * 2f, oContent.rect.width, 0.01f,
                "内容区应当比滚动区左右各缩进一个 PAGE_PADDING");
        }

        [Test]
        public void CreateScroll_ViewportFillsScrollArea()
        {
            ScrollRect oScroll = UiFactory.CreateScroll(m_Parent, "Scroll", out RectTransform _);

            Assert.AreEqual(PARENT_WIDTH, oScroll.viewport.rect.width, 0.01f);
            Assert.AreEqual(PARENT_HEIGHT, oScroll.viewport.rect.height, 0.01f);
        }
    }
}
