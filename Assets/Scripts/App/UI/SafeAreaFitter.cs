using UnityEngine;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 安全区适配。把自身 RectTransform 内缩到 Screen.safeArea，
    /// 避开刘海、状态栏与底部 home 指示条。编辑器里 safeArea 等于全屏，不会产生偏移。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform m_Rect;
        private Rect m_LastSafeArea = Rect.zero;
        private Vector2Int m_LastScreenSize = Vector2Int.zero;

        private void Awake()
        {
            m_Rect = GetComponent<RectTransform>();
            _apply();
        }

        private void Update()
        {
            // 转屏或分屏会改变安全区，尺寸变了才重算，避免每帧无谓开销
            if (Screen.safeArea != m_LastSafeArea ||
                Screen.width != m_LastScreenSize.x ||
                Screen.height != m_LastScreenSize.y)
            {
                _apply();
            }
        }

        private void _apply()
        {
            if (m_Rect == null)
            {
                return;
            }

            Rect oSafeArea = Screen.safeArea;
            m_LastSafeArea = oSafeArea;
            m_LastScreenSize = new Vector2Int(Screen.width, Screen.height);

            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Vector2 oMin = oSafeArea.position;
            Vector2 oMax = oSafeArea.position + oSafeArea.size;

            oMin.x /= Screen.width;
            oMin.y /= Screen.height;
            oMax.x /= Screen.width;
            oMax.y /= Screen.height;

            m_Rect.anchorMin = oMin;
            m_Rect.anchorMax = oMax;
            m_Rect.offsetMin = Vector2.zero;
            m_Rect.offsetMax = Vector2.zero;
        }
    }
}
