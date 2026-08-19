using System;

namespace Framework_WWJ.ResourceManagement
{
    /// <summary>
    /// 隔离 Resource Runtime 与具体资源后端句柄的最小所有权契约。
    /// </summary>
    public interface IResourceBackendHandle<out T> : IDisposable where T : UnityEngine.Object
    {
        T Value { get; }

        bool IsValid { get; }
    }
}
