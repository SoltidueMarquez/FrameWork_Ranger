using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// 在 Framework Center 内提供项目 Markdown 文档的轻量阅读入口。
    /// </summary>
    [FrameworkArchitecture(
        "Center 帮助页",
        "读取项目 Markdown，提供轻量排版与外部打开入口。",
        FrameworkArchitectureLayer.EditorIntegration,
        220,
        typeof(FrameworkCenterPage))]
    [FrameworkCenterPageExtension]
    internal sealed class FrameworkHelpPage : FrameworkCenterPage
    {
        private string m_documentPath =
            "Assets/Plugins/FrameWork_Ranger/Docs/README.md";

        public FrameworkHelpPage()
        {
        }

        public override string PageId => "framework.help";
        public override string DisplayName => "帮助";
        public override string Description => "阅读当前页面关联的 FrameWork_Ranger 项目文档。";
        public override string Category => "帮助";
        public override int Order => 1000;

        internal void SetDocumentPath(string assetPath)
        {
            if (!string.IsNullOrWhiteSpace(assetPath))
            {
                m_documentPath = assetPath.Replace('\\', '/');
            }
        }

        public override void OnGUI(FrameworkCenterPageContext context)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.SelectableLabel(m_documentPath, EditorStyles.textField, GUILayout.Height(20f));
            if (GUILayout.Button("在外部编辑器打开", GUILayout.Width(132f)))
            {
                var document = AssetDatabase.LoadAssetAtPath<TextAsset>(m_documentPath);
                if (document != null)
                {
                    AssetDatabase.OpenAsset(document);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);

            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(m_documentPath);
            if (textAsset == null)
            {
                EditorGUILayout.HelpBox("找不到关联的 Markdown 文档。", MessageType.Warning);
                return;
            }

            DrawMarkdown(textAsset.text);
        }

        private static void DrawMarkdown(string markdown)
        {
            using (var reader = new StringReader(markdown ?? string.Empty))
            {
                string line;
                var inCodeBlock = false;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("```", StringComparison.Ordinal))
                    {
                        inCodeBlock = !inCodeBlock;
                        continue;
                    }

                    if (inCodeBlock)
                    {
                        EditorGUILayout.SelectableLabel(line, EditorStyles.textArea, GUILayout.Height(18f));
                    }
                    else if (line.StartsWith("# ", StringComparison.Ordinal))
                    {
                        EditorGUILayout.Space(8f);
                        EditorGUILayout.LabelField(line.Substring(2), FrameworkCenterStyles.PageTitle);
                    }
                    else if (line.StartsWith("## ", StringComparison.Ordinal))
                    {
                        EditorGUILayout.Space(6f);
                        EditorGUILayout.LabelField(line.Substring(3), FrameworkCenterStyles.CardTitle);
                    }
                    else if (line.StartsWith("- ", StringComparison.Ordinal))
                    {
                        EditorGUILayout.LabelField($"• {line.Substring(2)}", FrameworkCenterStyles.Description);
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        EditorGUILayout.LabelField(line, FrameworkCenterStyles.Description);
                    }
                }
            }
        }
    }
}
