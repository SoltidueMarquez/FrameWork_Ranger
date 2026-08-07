using UnityEngine.SceneManagement;

namespace Framework_WWJ
{
    /// <summary>
    /// Runtime 使用的轻量场景身份值，避免纯场景协调逻辑长期持有 Unity Scene 结构。
    /// </summary>
    internal readonly struct FrameworkSceneDescriptor
    {
        internal int Handle { get; }

        internal string Path { get; }

        internal string Name { get; }

        internal FrameworkSceneDescriptor(int handle, string path, string name)
        {
            Handle = handle;
            Path = FrameworkProjectSettingsResolver.NormalizeScenePath(path);
            Name = name ?? string.Empty;
        }

        internal static FrameworkSceneDescriptor FromScene(Scene scene)
        {
            return new FrameworkSceneDescriptor(scene.handle, scene.path, scene.name);
        }
    }
}
