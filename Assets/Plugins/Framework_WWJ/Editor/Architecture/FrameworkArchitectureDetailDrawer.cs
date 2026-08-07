using System;
using UnityEditor;
using UnityEngine;

namespace Framework_WWJ.Editor
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
        internal static void Draw(FrameworkArchitectureTypeDescriptor descriptor)
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
            DrawValue("层级", descriptor.Metadata.Layer.ToString());
            DrawValue("种类", descriptor.IsInterface ? "Interface" : "Class");
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
