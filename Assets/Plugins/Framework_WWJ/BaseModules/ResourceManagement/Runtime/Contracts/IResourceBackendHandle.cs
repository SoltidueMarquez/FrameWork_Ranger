using System;

namespace Framework_WWJ.ResourceManagement
{
    /// <summary>
    /// 隔离 Resource Runtime 与具体资源后端句柄的最小所有权契约。
    /// </summary>
    [FrameworkArchitecture(
        "资源后端句柄契约",
        "统一暴露后端加载结果、有效性与一次性释放语义，隔离具体后端原生句柄。",
        FrameworkArchitectureLayer.Contracts,
        100)]
    public interface IResourceBackendHandle<out T> : IDisposable where T : UnityEngine.Object
    {
        T Value { get; }

        bool IsValid { get; }
    }
}
