using System;
using UnityEditor;
using UnityEngine;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// 绘制架构节点的职责、契约、协作关系和源码操作。
    /// </summary>
    [FrameworkArchitecture(
        "架构节点详情",
        "显示选中类型的完整信息，并提供 Project 定位与 Rider 打开。",
        FrameworkArchitectureLayer.EditorIntegration,
        360,
        typeof(FrameworkSourceScriptIndex))]
    internal static class FrameworkArchitectureDetailDrawer
    {
        internal static void DrawType(FrameworkArchitectureTypeDescriptor descriptor)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(310f));
            EditorGUILayout.LabelField("节点详情", EditorStyles.boldLabel);
            if (descriptor == null)
            {
                EditorGUILayout.HelpBox("点击一个节点查看名称、职责与源码信息。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField(descriptor.Metadata.DisplayName, FrameworkCenterStyles.CardTitle);
            EditorGUILayout.LabelField(descriptor.Metadata.Responsibility, FrameworkCenterStyles.Description);
            EditorGUILayout.Space(6f);
            DrawValue("类型", descriptor.Type.FullName);
            DrawValue("程序集", descriptor.AssemblyName);
            DrawValue("分组", descriptor.Group.GroupId);
            DrawValue("层级", descriptor.Metadata.Layer.ToString());
            DrawValue("种类", descriptor.Kind.ToString());
            DrawValue("基类", descriptor.BaseType?.FullName ?? "<无>");
            DrawTypes("直接接口", descriptor.DirectInterfaces);
            DrawTypes("关键协作", descriptor.Metadata.RelatedTypes);
            DrawValue(
                "源码",
                descriptor.Script == null ? "<未定位>" : AssetDatabase.GetAssetPath(descriptor.Script));

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(descriptor.Script == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Ping 脚本"))
                {
                    Selection.activeObject = descriptor.Script;
                    EditorGUIUtility.PingObject(descriptor.Script);
                }

                if (GUILayout.Button("打开脚本"))
                {
                    AssetDatabase.OpenAsset(descriptor.Script);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        internal static void DrawGroup(
            FrameworkArchitectureGroupDescriptor descriptor,
            bool isExpanded,
            bool canToggle,
            Action<FrameworkArchitectureGroupDescriptor> toggleGroup)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(310f));
            EditorGUILayout.LabelField("分组详情", EditorStyles.boldLabel);
            if (descriptor == null)
            {
                EditorGUILayout.HelpBox("点击一个分组查看职责和内部规模。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField(descriptor.DisplayName, FrameworkCenterStyles.CardTitle);
            EditorGUILayout.LabelField(descriptor.Responsibility, FrameworkCenterStyles.Description);
            EditorGUILayout.Space(6f);
            DrawValue("稳定路径", string.IsNullOrEmpty(descriptor.GroupId) ? "<根目录>" : descriptor.GroupId);
            DrawValue("内部类型", descriptor.DescendantTypeCount.ToString());
            DrawValue("生产程序集", descriptor.DescendantAssemblyCount.ToString());
            DrawValue(
                "直属程序集",
                descriptor.AssemblyNames.Count == 0
                    ? "<无>"
                    : string.Join("\n", descriptor.AssemblyNames));
            DrawValue(
                "下级分组",
                descriptor.Children.Count == 0
                    ? "<无>"
                    : string.Join(
                        "、",
                        System.Linq.Enumerable.Select(
                            descriptor.Children,
                            child => child.DisplayName)));

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(!canToggle))
            {
                if (GUILayout.Button(isExpanded ? "收起分组" : "展开分组"))
                {
                    toggleGroup?.Invoke(descriptor);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawValue(string label, string value)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel(value ?? string.Empty, EditorStyles.textField, GUILayout.Height(18f));
        }

        private static void DrawTypes(string label, System.Collections.Generic.IReadOnlyList<Type> types)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            if (types == null || types.Count == 0)
            {
                EditorGUILayout.LabelField("<无>", EditorStyles.miniLabel);
                return;
            }

            for (var i = 0; i < types.Count; i++)
            {
                EditorGUILayout.LabelField(types[i]?.FullName ?? "<null>", EditorStyles.miniLabel);
            }
        }
    }
}
