using System;
using UnityEditor;

namespace FrameWork_Ranger.Editor
{
    [FrameworkArchitecture(
        "配置工作台页签",
        "区分配置编辑与组合依赖图两个右侧工作区。",
        FrameworkArchitectureLayer.EditorIntegration,
        208)]
    internal enum FrameworkConfigurationWorkspaceTab
    {
        Edit = 0,
        DependencyGraph = 1,
    }

    [FrameworkArchitecture(
        "配置工作台选择种类",
        "区分项目、Global、默认 Scene 与场景覆盖导航目标。",
        FrameworkArchitectureLayer.EditorIntegration,
        209)]
    internal enum FrameworkConfigurationSelectionKind
    {
        Project,
        Global,
        DefaultScene,
        SceneBinding,
    }

    [FrameworkArchitecture(
        "配置工作台选择",
        "保存当前配置作用域的稳定标识与场景覆盖索引。",
        FrameworkArchitectureLayer.EditorIntegration,
        210,
        typeof(FrameworkConfigurationSelectionKind))]
    internal readonly struct FrameworkConfigurationSelection
    {
        internal FrameworkConfigurationSelection(
            FrameworkConfigurationSelectionKind kind,
            string contextId,
            int bindingIndex = -1)
        {
            Kind = kind;
            ContextId = contextId;
            BindingIndex = bindingIndex;
        }

        internal FrameworkConfigurationSelectionKind Kind { get; }

        internal string ContextId { get; }

        internal int BindingIndex { get; }
    }

    /// <summary>
    /// 保存配置工作台当前作用域和页签。状态只属于当前 Unity 会话，不进入配置资产。
    /// </summary>
    [FrameworkArchitecture(
        "配置工作台状态",
        "用稳定作用域标识保存项目配置页的当前导航与编辑页签。",
        FrameworkArchitectureLayer.EditorIntegration,
        212,
        typeof(FrameworkProjectSettings))]
    internal sealed class FrameworkConfigurationWorkspaceState
    {
        private const string SessionPrefix = "FrameWork_Ranger.ConfigurationWorkspace.";
        private const string ProjectContextId = "Project";
        private const string GlobalContextId = "Global";
        private const string DefaultSceneContextId = "DefaultScene";
        private const string SceneGuidPrefix = "Scene:";
        private const string SceneIndexPrefix = "SceneIndex:";

        private readonly string m_selectedContextKey;
        private readonly string m_activeTabKey;

        internal FrameworkConfigurationWorkspaceState(string hostIdentity)
        {
            if (string.IsNullOrWhiteSpace(hostIdentity))
            {
                throw new ArgumentException("配置工作台宿主标识不能为空。", nameof(hostIdentity));
            }

            var keyPrefix = SessionPrefix + hostIdentity + ".";
            m_selectedContextKey = keyPrefix + "SelectedContext";
            m_activeTabKey = keyPrefix + "ActiveTab";
        }

        internal FrameworkConfigurationWorkspaceTab ActiveTab
        {
            get
            {
                var value = SessionState.GetInt(m_activeTabKey, (int)FrameworkConfigurationWorkspaceTab.Edit);
                return Enum.IsDefined(typeof(FrameworkConfigurationWorkspaceTab), value)
                    ? (FrameworkConfigurationWorkspaceTab)value
                    : FrameworkConfigurationWorkspaceTab.Edit;
            }
            set => SessionState.SetInt(m_activeTabKey, (int)value);
        }

        internal FrameworkConfigurationSelection Resolve(FrameworkProjectSettings settings)
        {
            var savedContextId = SessionState.GetString(m_selectedContextKey, string.Empty);
            if (string.IsNullOrEmpty(savedContextId))
            {
                return settings != null && settings.GlobalConfig != null
                    ? Select(FrameworkConfigurationSelectionKind.Global, GlobalContextId)
                    : Select(FrameworkConfigurationSelectionKind.Project, ProjectContextId);
            }

            if (savedContextId == ProjectContextId)
            {
                return new FrameworkConfigurationSelection(
                    FrameworkConfigurationSelectionKind.Project,
                    ProjectContextId);
            }

            if (savedContextId == GlobalContextId)
            {
                return new FrameworkConfigurationSelection(
                    FrameworkConfigurationSelectionKind.Global,
                    GlobalContextId);
            }

            if (savedContextId == DefaultSceneContextId)
            {
                return new FrameworkConfigurationSelection(
                    FrameworkConfigurationSelectionKind.DefaultScene,
                    DefaultSceneContextId);
            }

            if (settings != null && TryResolveSceneBinding(settings, savedContextId, out var selection))
            {
                return selection;
            }

            return settings != null && settings.GlobalConfig != null
                ? Select(FrameworkConfigurationSelectionKind.Global, GlobalContextId)
                : Select(FrameworkConfigurationSelectionKind.Project, ProjectContextId);
        }

        internal FrameworkConfigurationSelection SelectProject()
        {
            return Select(FrameworkConfigurationSelectionKind.Project, ProjectContextId);
        }

        internal FrameworkConfigurationSelection SelectGlobal()
        {
            return Select(FrameworkConfigurationSelectionKind.Global, GlobalContextId);
        }

        internal FrameworkConfigurationSelection SelectDefaultScene()
        {
            return Select(FrameworkConfigurationSelectionKind.DefaultScene, DefaultSceneContextId);
        }

        internal FrameworkConfigurationSelection SelectSceneBinding(
            FrameworkSceneBinding binding,
            int index)
        {
            return Select(
                FrameworkConfigurationSelectionKind.SceneBinding,
                BuildSceneContextId(binding, index),
                index);
        }

        internal void Clear()
        {
            SessionState.EraseString(m_selectedContextKey);
            SessionState.EraseInt(m_activeTabKey);
        }

        internal static string BuildSceneContextId(FrameworkSceneBinding binding, int index)
        {
            return string.IsNullOrWhiteSpace(binding?.SceneGuid)
                ? SceneIndexPrefix + index
                : SceneGuidPrefix + binding.SceneGuid;
        }

        private FrameworkConfigurationSelection Select(
            FrameworkConfigurationSelectionKind kind,
            string contextId,
            int bindingIndex = -1)
        {
            SessionState.SetString(m_selectedContextKey, contextId);
            return new FrameworkConfigurationSelection(kind, contextId, bindingIndex);
        }

        private bool TryResolveSceneBinding(
            FrameworkProjectSettings settings,
            string contextId,
            out FrameworkConfigurationSelection selection)
        {
            if (contextId.StartsWith(SceneGuidPrefix, StringComparison.Ordinal))
            {
                var guid = contextId.Substring(SceneGuidPrefix.Length);
                for (var i = 0; i < settings.SceneBindings.Count; i++)
                {
                    if (settings.SceneBindings[i]?.SceneGuid == guid)
                    {
                        selection = new FrameworkConfigurationSelection(
                            FrameworkConfigurationSelectionKind.SceneBinding,
                            contextId,
                            i);
                        return true;
                    }
                }
            }
            else if (contextId.StartsWith(SceneIndexPrefix, StringComparison.Ordinal) &&
                     int.TryParse(contextId.Substring(SceneIndexPrefix.Length), out var index) &&
                     index >= 0 && index < settings.SceneBindings.Count)
            {
                var binding = settings.SceneBindings[index];
                var stableContextId = BuildSceneContextId(binding, index);
                if (stableContextId != contextId)
                {
                    SessionState.SetString(m_selectedContextKey, stableContextId);
                }

                selection = new FrameworkConfigurationSelection(
                    FrameworkConfigurationSelectionKind.SceneBinding,
                    stableContextId,
                    index);
                return true;
            }

            selection = default;
            return false;
        }
    }
}
