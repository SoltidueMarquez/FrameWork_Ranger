using UnityEngine;

namespace FrameWork_Ranger
{
    /// <summary>
    /// 保存单个 SceneScope 需要装配的模块模板。
    /// </summary>
    [FrameworkArchitecture(
        "场景模块配置",
        "保存一个 SceneScope 需要装配的模块模板。",
        FrameworkArchitectureLayer.Configuration,
        30,
        typeof(ModuleConfigBase))]
    [CreateAssetMenu(fileName = "FrameworkSceneConfig", menuName = "Framework WWJ/Scene Config")]
    public sealed class FrameworkSceneConfig : ModuleConfigBase
    {
    }
}
