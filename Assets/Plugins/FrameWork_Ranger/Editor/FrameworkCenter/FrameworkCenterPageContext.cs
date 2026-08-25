using UnityEditor;
using UnityEngine;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// 页面与 Framework Center 宿主交互的受控上下文。
    /// </summary>
    [FrameworkArchitecture(
        "Center 页面上下文",
        "允许页面导航、打开帮助、选择资产和请求重绘，但不暴露窗口内部集合。",
        FrameworkArchitectureLayer.EditorIntegration,
        110,
        typeof(FrameworkCenterWindow))]
    public sealed class FrameworkCenterPageContext
    {
        private readonly FrameworkCenterWindow m_window;

        internal FrameworkCenterPageContext(FrameworkCenterWindow window)
        {
            m_window = window;
        }

        /// <summary>
        /// 打开并激活指定页面。
        /// </summary>
        public void OpenPage(string pageId)
        {
            m_window.OpenPage(pageId);
        }

        /// <summary>
        /// 在帮助标签中打开指定项目 Markdown 文档。
        /// </summary>
        public void OpenHelp(string assetPath)
        {
            m_window.OpenHelp(assetPath);
        }

        /// <summary>
        /// 在 Project 窗口中选择并定位一个 Unity 对象。
        /// </summary>
        public void SelectObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
        }

        /// <summary>
        /// 请求中心窗口在下一次 Editor 更新时重绘。
        /// </summary>
        public void Repaint()
        {
            m_window.Repaint();
        }
    }
}
