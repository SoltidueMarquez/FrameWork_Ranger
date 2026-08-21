using System;
using System.Collections.Generic;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// Framework Center 的公开页面扩展契约。
    /// Editor 程序集可以派生此类型、声明 <see cref="FrameworkCenterPageExtensionAttribute"/>
    /// 并提供公共无参构造，无需修改中心窗口即可被自动发现。
    /// </summary>
    [FrameworkArchitecture(
        "Center 页面契约",
        "约束可发现编辑器页面的身份、检索元数据、生命周期和绘制入口。",
        FrameworkArchitectureLayer.EditorIntegration,
        100)]
    public abstract class FrameworkCenterPage
    {
        /// <summary>
        /// 获取跨版本保持稳定的页面 ID。
        /// </summary>
        public abstract string PageId { get; }

        /// <summary>
        /// 获取页面显示名称。
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// 获取页面职责摘要。
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// 获取页面导航分类。
        /// </summary>
        public abstract string Category { get; }

        /// <summary>
        /// 获取页面在分类中的稳定排序值。
        /// </summary>
        public virtual int Order => 0;

        /// <summary>
        /// 获取搜索使用的附加关键词。
        /// </summary>
        public virtual IReadOnlyList<string> Keywords => Array.Empty<string>();

        /// <summary>
        /// 获取相对于项目根目录的帮助文档资产路径；为空表示页面没有独立帮助文档。
        /// </summary>
        public virtual string HelpDocumentPath => string.Empty;

        /// <summary>
        /// 获取页面是否使用 Framework Center 提供的外层内容滚动。
        /// 需要固定工具栏或左右独立滚动区的页面可以返回 false，并在 <see cref="OnGUI"/> 中自行管理滚动。
        /// </summary>
        public virtual bool UseHostContentScroll => true;

        /// <summary>
        /// 页面成为当前标签时调用。
        /// </summary>
        public virtual void OnActivated(FrameworkCenterPageContext context)
        {
        }

        /// <summary>
        /// 页面离开当前标签或窗口关闭时调用。
        /// </summary>
        public virtual void OnDeactivated(FrameworkCenterPageContext context)
        {
        }

        /// <summary>
        /// 绘制页面内容。
        /// </summary>
        public abstract void OnGUI(FrameworkCenterPageContext context);
    }
}
