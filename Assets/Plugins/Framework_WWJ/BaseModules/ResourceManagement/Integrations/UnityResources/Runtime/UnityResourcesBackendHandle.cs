using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework_WWJ.ResourceManagement.UnityResources
{
    /// <summary>
    /// Unity Resources 的逻辑所有权句柄；释放时只解除框架引用，不触发全局资源整理。
    /// </summary>
    [FrameworkArchitecture(
        "Unity Resources 句柄",
        "保存 Resources 加载结果并实现框架后端句柄契约；释放只解除框架引用。",
        FrameworkArchitectureLayer.Contracts,
        110,
        typeof(IResourceBackendHandle<>))]
    internal sealed class UnityResourcesBackendHandle<T> : IResourceBackendHandle<T> where T : Object
    {
        private T m_value;

        public T Value => m_value;

        public bool IsValid => m_value != null;

        internal UnityResourcesBackendHandle(T value)
        {
            m_value = value;
        }

        public void Dispose()
        {
            m_value = null;
        }
    }
}
