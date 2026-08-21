using UnityEngine.SceneManagement;

namespace Framework_WWJ
{
    /// <summary>
    /// Runtime 使用的轻量场景身份值，避免纯场景协调逻辑长期持有 Unity Scene 结构。
    /// </summary>
    [FrameworkArchitecture(
        "场景身份描述",
        "保存活动场景 Handle、标准化路径和名称，作为 SceneScope 的稳定所有者令牌。",
        FrameworkArchitectureLayer.RuntimeDriving,
        25,
        typeof(FrameworkSceneCoordinator))]
    internal readonly struct FrameworkSceneDescriptor
    {
        internal ulong Handle { get; }

        internal string Path { get; }

        internal string Name { get; }

        internal FrameworkSceneDescriptor(ulong handle, string path, string name)
        {
            Handle = handle;
            Path = FrameworkProjectSettingsResolver.NormalizeScenePath(path);
            Name = name ?? string.Empty;
        }

        internal static FrameworkSceneDescriptor FromScene(Scene scene)
        {
            return new FrameworkSceneDescriptor(scene.handle.GetRawData(), scene.path, scene.name);
        }
    }
}
