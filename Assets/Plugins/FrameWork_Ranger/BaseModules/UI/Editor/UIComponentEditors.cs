using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI.Editor
{
    [CustomEditor(typeof(UIButton)), CanEditMultipleObjects] public sealed class UIButtonInspector : OdinEditor { }
    [CustomEditor(typeof(UIToggle)), CanEditMultipleObjects] public sealed class UIToggleInspector : OdinEditor { }
    [CustomEditor(typeof(UIControlVisual)), CanEditMultipleObjects] public sealed class UIControlVisualInspector : OdinEditor { }
    [CustomEditor(typeof(UIDragEvents)), CanEditMultipleObjects] public sealed class UIDragEventsInspector : OdinEditor { }
    [CustomEditor(typeof(UIWindowDrag)), CanEditMultipleObjects] public sealed class UIWindowDragInspector : OdinEditor { }
    [CustomEditor(typeof(UIFoldout)), CanEditMultipleObjects] public sealed class UIFoldoutInspector : OdinEditor { }
    [CustomEditor(typeof(UIAccordionGroup)), CanEditMultipleObjects] public sealed class UIAccordionInspector : OdinEditor { }
    [CustomEditor(typeof(UILayoutItem)), CanEditMultipleObjects]
    public sealed class UILayoutItemInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ControlWidth"), new GUIContent("布局控制宽度"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ControlHeight"), new GUIContent("布局控制高度"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OverrideAlignment"), new GUIContent("独立对齐"));
            UIComponentGUI.Enum(serializedObject.FindProperty("Alignment"), "对齐方式", UIComponentGUI.Alignments);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Offset"), new GUIContent("位置偏移", "X 向右、Y 向下。"));
            serializedObject.ApplyModifiedProperties();
        }
    }
    internal static class UIComponentGUI
    {
        internal static readonly string[] Alignments = { "左上", "上居中", "右上", "左居中", "正中", "右居中", "左下", "下居中", "右下" };
        internal static void Enum(SerializedProperty property, string label, string[] choices)
        {
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues; EditorGUI.BeginChangeCheck();
            int value = EditorGUILayout.Popup(label, property.enumValueIndex, choices);
            if (EditorGUI.EndChangeCheck()) property.enumValueIndex = value;
            EditorGUI.showMixedValue = false;
        }
        internal static void LayoutProperty(SerializedProperty property, string label)
        {
            if (property.name == "m_ChildAlignment") { Enum(property, label, Alignments); return; }
            EditorGUILayout.PropertyField(property, new GUIContent(label), true);
        }
    }
    [CustomEditor(typeof(UIGradient)), CanEditMultipleObjects] public sealed class UIGradientInspector : OdinEditor { }
    [CustomEditor(typeof(UIOutlineGroup)), CanEditMultipleObjects] public sealed class UIOutlineGroupInspector : OdinEditor { }
    [CustomEditor(typeof(UIContentFitter)), CanEditMultipleObjects]
    public sealed class UIContentFitterInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var modes = new[] { "不控制", "最小尺寸", "偏好尺寸" };
            UIComponentGUI.Enum(serializedObject.FindProperty("m_HorizontalFit"), "宽度适配", modes);
            UIComponentGUI.Enum(serializedObject.FindProperty("m_VerticalFit"), "高度适配", modes);
            foreach (var field in new[] { ("LimitWidth", "限制宽度"), ("WidthRange", "宽度范围"), ("LimitHeight", "限制高度"), ("HeightRange", "高度范围") })
                EditorGUILayout.PropertyField(serializedObject.FindProperty(field.Item1), new GUIContent(field.Item2));
            serializedObject.ApplyModifiedProperties();
        }
    }
    [CustomEditor(typeof(UIStackLayout), true), CanEditMultipleObjects]
    public sealed class UIStackLayoutInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            foreach (var field in new[] { ("m_Padding", "内边距"), ("m_ChildAlignment", "整体对齐"), ("m_spacing", "子项间距"), ("m_expandWidth", "扩展子项宽度"), ("m_expandHeight", "扩展子项高度") })
                UIComponentGUI.LayoutProperty(serializedObject.FindProperty(field.Item1), field.Item2);
            EditorGUILayout.HelpBox("子项可添加“布局子项设置”覆盖对齐、偏移和尺寸控制。", MessageType.None);
            serializedObject.ApplyModifiedProperties();
        }
    }
    [CustomEditor(typeof(UIGridLayout)), CanEditMultipleObjects]
    public sealed class UIGridLayoutInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            foreach (var field in new[] { ("m_Padding", "内边距"), ("m_ChildAlignment", "整体对齐"), ("Columns", "列数"), ("CellSize", "单元尺寸"), ("Spacing", "格子间距") })
                UIComponentGUI.LayoutProperty(serializedObject.FindProperty(field.Item1), field.Item2);
            serializedObject.ApplyModifiedProperties();
        }
    }
    public static class UIComponentMenu
    {
        private static GameObject Create(string name, System.Type component, MenuCommand command)
        {
            var go = new GameObject(name, typeof(RectTransform), component);
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject ?? Selection.activeGameObject);
            Undo.RegisterCreatedObjectUndo(go, "创建 Ranger UI " + name);
            Selection.activeGameObject = go;
            return go;
        }
        [MenuItem("GameObject/Ranger UI/图片", false, 10)] private static void Image(MenuCommand command) => Create("Image", typeof(UIImage), command);
        [MenuItem("GameObject/Ranger UI/纹理图片", false, 11)] private static void Raw(MenuCommand command) => Create("RawImage", typeof(UIRawImage), command);
        [MenuItem("GameObject/Ranger UI/按钮", false, 12)]
        private static void Button(MenuCommand command)
        {
            var go = Create("Button", typeof(UIImage), command);
            var button = go.AddComponent<UIButton>(); button.targetGraphic = go.GetComponent<UIImage>();
            ((RectTransform)go.transform).sizeDelta = new Vector2(160, 40);
        }
        [MenuItem("GameObject/Ranger UI/切换项", false, 13)]
        private static void Toggle(MenuCommand command)
        {
            var go = Create("Toggle", typeof(UIImage), command);
            var toggle = go.AddComponent<UIToggle>(); toggle.targetGraphic = go.GetComponent<UIImage>();
            var mark = new GameObject("Checkmark", typeof(RectTransform), typeof(UIImage));
            mark.transform.SetParent(go.transform, false); ((RectTransform)mark.transform).sizeDelta = new Vector2(18, 18);
            toggle.graphic = mark.GetComponent<UIImage>();
        }
        [MenuItem("GameObject/Ranger UI/横向布局", false, 20)] private static void Horizontal(MenuCommand command) => Create("Horizontal", typeof(UIHorizontalLayout), command);
        [MenuItem("GameObject/Ranger UI/纵向布局", false, 21)] private static void Vertical(MenuCommand command) => Create("Vertical", typeof(UIVerticalLayout), command);
        [MenuItem("GameObject/Ranger UI/网格布局", false, 22)] private static void Grid(MenuCommand command) => Create("Grid", typeof(UIGridLayout), command);
        [MenuItem("GameObject/Ranger UI/滚动列表", false, 23)]
        private static void Scroll(MenuCommand command)
        {
            var root = Create("Scroll View", typeof(ScrollRect), command);
            ((RectTransform)root.transform).sizeDelta = new Vector2(360, 240);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(UIImage), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false); ((RectTransform)viewport.transform).StretchToParent();
            var content = new GameObject("Content", typeof(RectTransform), typeof(UIVerticalLayout), typeof(UIContentFitter));
            content.transform.SetParent(viewport.transform, false);
            var rect = (RectTransform)content.transform; rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f, 1); rect.sizeDelta = Vector2.zero;
            content.GetComponent<UIContentFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = root.GetComponent<ScrollRect>(); scroll.content = rect; scroll.viewport = (RectTransform)viewport.transform; scroll.horizontal = false;
        }
        [MenuItem("GameObject/Ranger UI/折叠组", false, 24)]
        private static void Foldout(MenuCommand command)
        {
            var root = Create("Foldout", typeof(UIVerticalLayout), command);
            var header = new GameObject("Header", typeof(RectTransform), typeof(UIImage), typeof(UIButton), typeof(LayoutElement));
            header.transform.SetParent(root.transform, false); header.GetComponent<LayoutElement>().preferredHeight = 40;
            header.GetComponent<UIButton>().targetGraphic = header.GetComponent<UIImage>();
            var content = new GameObject("Content", typeof(RectTransform), typeof(UIVerticalLayout), typeof(LayoutElement));
            content.transform.SetParent(root.transform, false); content.GetComponent<LayoutElement>().preferredHeight = 100;
            var fold = root.AddComponent<UIFoldout>(); fold.enabled = false; fold.Header = header.GetComponent<UIButton>(); fold.Content = content; fold.enabled = true;
        }
    }
}
