using System.Collections.Generic;

namespace FrameWork_Ranger
{
    /// <summary>
    /// 中央项目设置针对一个活动场景的只读解析结果。
    /// </summary>
    [FrameworkArchitecture(
        "项目设置解析结果",
        "保存目标 GlobalConfig、SceneConfig、命中方式和诊断。",
        FrameworkArchitectureLayer.Configuration,
        70)]
    internal sealed class FrameworkProjectSettingsResult
    {
        internal FrameworkGlobalConfig GlobalConfig { get; }

        internal FrameworkSceneConfig SceneConfig { get; }

        internal bool UsesSceneOverride { get; }

        internal IReadOnlyList<FrameworkProjectSettingsDiagnostic> Diagnostics { get; }

        internal bool IsValid
        {
            get
            {
                for (var i = 0; i < Diagnostics.Count; i++)
                {
                    if (Diagnostics[i].Severity == FrameworkProjectSettingsDiagnosticSeverity.Error)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        internal FrameworkProjectSettingsResult(
            FrameworkGlobalConfig globalConfig,
            FrameworkSceneConfig sceneConfig,
            bool usesSceneOverride,
            IReadOnlyList<FrameworkProjectSettingsDiagnostic> diagnostics)
        {
            GlobalConfig = globalConfig;
            SceneConfig = sceneConfig;
            UsesSceneOverride = usesSceneOverride;
            Diagnostics = diagnostics;
        }
    }
}
