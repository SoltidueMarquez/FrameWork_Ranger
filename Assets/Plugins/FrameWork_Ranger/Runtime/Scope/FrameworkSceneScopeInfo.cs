namespace FrameWork_Ranger
{
    /// <summary>Runtime 创建的只读场景轮次。对象身份区分跨 Runtime 重启及同场景重装配。</summary>
    [FrameworkArchitecture("场景作用域身份", "为全局服务提供稳定的场景轮次，不暴露可变 Scope。",
        FrameworkArchitectureLayer.ModuleModel, 45)]
    public sealed class FrameworkSceneScopeInfo
    {
        public ulong SceneHandle { get; }
        public string ScenePath { get; }
        public long Generation { get; }

        internal FrameworkSceneScopeInfo(ulong sceneHandle, string scenePath, long generation)
        {
            SceneHandle = sceneHandle;
            ScenePath = scenePath ?? string.Empty;
            Generation = generation;
        }
    }
}
