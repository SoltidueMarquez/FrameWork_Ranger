using UnityEditor;
using UnityEngine;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// Framework Center 共用的自适应主题、排版和扁平绘制辅助。
    /// 所有颜色与控件间距集中在这里，窗口和页面只表达结构与行为。
    /// </summary>
    [FrameworkArchitecture(
        "Center 样式",
        "集中定义深浅主题、页面、卡片、标签、导航和节点图样式。",
        FrameworkArchitectureLayer.EditorIntegration,
        140)]
    internal static class FrameworkCenterStyles
    {
        #region 主题颜色

        internal static Color WindowBackgroundColor => EditorGUIUtility.isProSkin
            ? new Color(0.155f, 0.16f, 0.17f)
            : new Color(0.76f, 0.77f, 0.79f);

        internal static Color PanelColor => EditorGUIUtility.isProSkin
            ? new Color(0.19f, 0.195f, 0.21f)
            : new Color(0.84f, 0.85f, 0.87f);

        internal static Color CardColor => EditorGUIUtility.isProSkin
            ? new Color(0.225f, 0.23f, 0.245f)
            : new Color(0.91f, 0.92f, 0.94f);

        internal static Color HoverColor => EditorGUIUtility.isProSkin
            ? new Color(0.275f, 0.29f, 0.32f)
            : new Color(0.96f, 0.97f, 0.99f);

        internal static Color SelectedColor => EditorGUIUtility.isProSkin
            ? new Color(0.19f, 0.285f, 0.46f)
            : new Color(0.66f, 0.77f, 0.96f);

        internal static Color AccentColor => EditorGUIUtility.isProSkin
            ? new Color(0.30f, 0.56f, 0.90f)
            : new Color(0.18f, 0.43f, 0.82f);

        internal static Color BorderColor => EditorGUIUtility.isProSkin
            ? new Color(0.09f, 0.095f, 0.105f)
            : new Color(0.55f, 0.57f, 0.61f);

        internal static Color MutedTextColor => EditorGUIUtility.isProSkin
            ? new Color(0.68f, 0.70f, 0.74f)
            : new Color(0.31f, 0.33f, 0.37f);

        internal static Color GraphBackgroundColor => EditorGUIUtility.isProSkin
            ? new Color(0.125f, 0.13f, 0.145f)
            : new Color(0.79f, 0.81f, 0.84f);

        internal static Color GraphGridColor => EditorGUIUtility.isProSkin
            ? new Color(0.32f, 0.34f, 0.38f, 0.22f)
            : new Color(0.36f, 0.39f, 0.44f, 0.18f);

        #endregion

        #region 缓存样式

        private static bool s_cachedProSkin;
        private static bool s_themeInitialized;
        private static GUIStyle s_topTitle;
        private static GUIStyle s_topSubtitle;
        private static GUIStyle s_pageTitle;
        private static GUIStyle s_cardTitle;
        private static GUIStyle s_description;
        private static GUIStyle s_tab;
        private static GUIStyle s_navigationLabel;
        private static GUIStyle s_navigationCategory;
        private static GUIStyle s_statusBadge;
        private static GUIStyle s_graphToolbar;
        private static GUIStyle s_toolbarLabel;
        private static GUIStyle s_toolbarHint;
        private static GUIStyle s_graphLayerLabel;

        internal static GUIStyle TopTitle => GetStyle(ref s_topTitle, () => new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft,
        });

        internal static GUIStyle TopSubtitle => GetStyle(ref s_topSubtitle, () => new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = MutedTextColor },
        });

        internal static GUIStyle PageTitle => GetStyle(ref s_pageTitle, () => new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
        });

        internal static GUIStyle CardTitle => GetStyle(ref s_cardTitle, () => new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
        });

        internal static GUIStyle Description => GetStyle(ref s_description, () => new GUIStyle(EditorStyles.wordWrappedLabel)
        {
            normal = { textColor = MutedTextColor },
        });

        internal static GUIStyle Tab => GetStyle(ref s_tab, () => new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 6, 2, 2),
            clipping = TextClipping.Clip,
        });

        internal static GUIStyle NavigationLabel => GetStyle(ref s_navigationLabel, () => new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(14, 8, 2, 2),
            clipping = TextClipping.Clip,
        });

        internal static GUIStyle NavigationCategory => GetStyle(ref s_navigationCategory, () => new GUIStyle(EditorStyles.miniBoldLabel)
        {
            normal = { textColor = MutedTextColor },
        });

        internal static GUIStyle StatusBadge => GetStyle(ref s_statusBadge, () => new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(8, 8, 2, 2),
        });

        internal static GUIStyle GraphToolbar => GetStyle(ref s_graphToolbar, () => new GUIStyle(EditorStyles.toolbar)
        {
            margin = new RectOffset(0, 0, 0, 2),
            padding = new RectOffset(4, 4, 0, 0),
            fixedHeight = 24f,
        });

        internal static GUIStyle ToolbarLabel => GetStyle(ref s_toolbarLabel, () => new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
        });

        internal static GUIStyle ToolbarHint => GetStyle(ref s_toolbarHint, () => new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = MutedTextColor },
        });

        internal static GUIStyle GraphLayerLabel => GetStyle(ref s_graphLayerLabel, () => new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip,
        });

        #endregion

        #region 绘制辅助

        internal static void DrawPanel(Rect rect, Color color)
        {
            EditorGUI.DrawRect(rect, color);
            DrawBorder(rect, BorderColor);
        }

        internal static void DrawBorder(Rect rect, Color color, float thickness = 1f)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        internal static GUIStyle CreateGraphNodeStyle(float zoom)
        {
            return new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                wordWrap = false,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(11f * zoom), 7, 13),
                padding = new RectOffset(
                    Mathf.Max(2, Mathf.RoundToInt(6f * zoom)),
                    2,
                    2,
                    2),
            };
        }

        #endregion

        #region 内部实现

        private static GUIStyle GetStyle(ref GUIStyle cache, System.Func<GUIStyle> factory)
        {
            EnsureThemeCache();
            return cache ?? (cache = factory());
        }

        private static void EnsureThemeCache()
        {
            if (s_themeInitialized && s_cachedProSkin == EditorGUIUtility.isProSkin)
            {
                return;
            }

            s_themeInitialized = true;
            s_cachedProSkin = EditorGUIUtility.isProSkin;
            s_topTitle = null;
            s_topSubtitle = null;
            s_pageTitle = null;
            s_cardTitle = null;
            s_description = null;
            s_tab = null;
            s_navigationLabel = null;
            s_navigationCategory = null;
            s_statusBadge = null;
            s_graphToolbar = null;
            s_toolbarLabel = null;
            s_toolbarHint = null;
            s_graphLayerLabel = null;
        }

        #endregion
    }
}
