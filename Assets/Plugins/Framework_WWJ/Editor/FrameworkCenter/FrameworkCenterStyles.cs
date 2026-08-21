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

        internal static Color SuccessColor => EditorGUIUtility.isProSkin
            ? new Color(0.35f, 0.72f, 0.46f)
            : new Color(0.18f, 0.56f, 0.30f);

        internal static Color WarningColor => EditorGUIUtility.isProSkin
            ? new Color(0.92f, 0.66f, 0.24f)
            : new Color(0.78f, 0.46f, 0.08f);

        internal static Color ErrorColor => EditorGUIUtility.isProSkin
            ? new Color(0.88f, 0.34f, 0.34f)
            : new Color(0.72f, 0.18f, 0.18f);

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
        private static GUIStyle s_previewTab;
        private static GUIStyle s_navigationLabel;
        private static GUIStyle s_navigationCategory;
        private static GUIStyle s_statusBadge;
        private static GUIStyle s_graphToolbar;
        private static GUIStyle s_toolbarLabel;
        private static GUIStyle s_toolbarHint;
        private static GUIStyle s_graphLayerLabel;
        private static GUIStyle s_architectureTitle;
        private static GUIStyle s_architectureBody;
        private static GUIStyle s_architectureBadge;
        private static readonly Vector3[] s_roundedFillPoints = new Vector3[24];
        private static readonly Vector3[] s_roundedBorderPoints = new Vector3[25];

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

        internal static GUIStyle PreviewTab => GetStyle(ref s_previewTab, () => new GUIStyle(Tab)
        {
            fontStyle = FontStyle.Italic,
            normal = { textColor = MutedTextColor },
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

        internal static GUIStyle GetArchitectureTitle(float zoom)
        {
            var style = GetStyle(ref s_architectureTitle, () => new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0),
            });
            style.fontSize = Mathf.Clamp(Mathf.RoundToInt(12f * zoom), 8, 14);
            return style;
        }

        internal static GUIStyle GetArchitectureBody(float zoom)
        {
            var style = GetStyle(ref s_architectureBody, () => new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0),
                normal = { textColor = MutedTextColor },
            });
            style.fontSize = Mathf.Clamp(Mathf.RoundToInt(10f * zoom), 7, 11);
            return style;
        }

        internal static GUIStyle GetArchitectureBadge(float zoom)
        {
            var style = GetStyle(ref s_architectureBadge, () => new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                padding = new RectOffset(3, 3, 0, 0),
            });
            style.fontSize = Mathf.Clamp(Mathf.RoundToInt(9f * zoom), 7, 10);
            return style;
        }

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

        /// <summary>
        /// 使用复用顶点绘制圆角矩形，避免为每个节点创建纹理或临时数组。
        /// 调用方须位于 <see cref="Handles.BeginGUI"/> 与 <see cref="Handles.EndGUI"/> 之间。
        /// </summary>
        internal static void DrawRoundedRect(
            Rect rect,
            Color fill,
            Color border,
            float radius,
            float borderWidth = 1f)
        {
            var clampedRadius = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
            var pointIndex = 0;
            for (var corner = 0; corner < 4; corner++)
            {
                Vector2 center;
                float startAngle;
                switch (corner)
                {
                    case 0:
                        center = new Vector2(rect.xMin + clampedRadius, rect.yMin + clampedRadius);
                        startAngle = 180f;
                        break;
                    case 1:
                        center = new Vector2(rect.xMax - clampedRadius, rect.yMin + clampedRadius);
                        startAngle = 270f;
                        break;
                    case 2:
                        center = new Vector2(rect.xMax - clampedRadius, rect.yMax - clampedRadius);
                        startAngle = 0f;
                        break;
                    default:
                        center = new Vector2(rect.xMin + clampedRadius, rect.yMax - clampedRadius);
                        startAngle = 90f;
                        break;
                }

                for (var segment = 0; segment < 6; segment++)
                {
                    var radians = (startAngle + segment * 18f) * Mathf.Deg2Rad;
                    s_roundedFillPoints[pointIndex++] = new Vector3(
                        center.x + Mathf.Cos(radians) * clampedRadius,
                        center.y + Mathf.Sin(radians) * clampedRadius,
                        0f);
                }
            }

            var previousColor = Handles.color;
            Handles.color = fill;
            Handles.DrawAAConvexPolygon(s_roundedFillPoints);
            for (var i = 0; i < s_roundedFillPoints.Length; i++)
            {
                s_roundedBorderPoints[i] = s_roundedFillPoints[i];
            }

            s_roundedBorderPoints[s_roundedBorderPoints.Length - 1] = s_roundedFillPoints[0];
            Handles.color = border;
            Handles.DrawAAPolyLine(Mathf.Max(0.5f, borderWidth), s_roundedBorderPoints);
            Handles.color = previousColor;
        }

        internal static bool DrawPinButton(Rect rect, bool pinned)
        {
            var tooltip = pinned ? "取消固定" : "固定为快捷页签";
            var hovered = rect.Contains(Event.current.mousePosition);
            if (hovered)
            {
                EditorGUI.DrawRect(rect, HoverColor);
            }

            var clicked = GUI.Button(rect, new GUIContent(string.Empty, tooltip), GUIStyle.none);
            var color = pinned ? AccentColor : MutedTextColor;
            var centerX = Mathf.Round(rect.center.x);
            var head = new Rect(centerX - 4f, rect.y + 4f, 8f, 5f);
            if (pinned)
            {
                EditorGUI.DrawRect(head, color);
            }
            else
            {
                DrawBorder(head, color);
            }

            EditorGUI.DrawRect(new Rect(centerX - 1f, head.yMax, 2f, 6f), color);
            EditorGUI.DrawRect(new Rect(centerX - 2f, head.yMax + 6f, 4f, 1f), color);
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            return clicked;
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
            s_previewTab = null;
            s_navigationLabel = null;
            s_navigationCategory = null;
            s_statusBadge = null;
            s_graphToolbar = null;
            s_toolbarLabel = null;
            s_toolbarHint = null;
            s_graphLayerLabel = null;
            s_architectureTitle = null;
            s_architectureBody = null;
            s_architectureBadge = null;
        }

        #endregion
    }
}
