using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace Framework_WWJ.ResourceManagement.Addressables
{
    /// <summary>
    /// 独占一份 Addressables 引用计数的后端句柄。
    /// </summary>
    [FrameworkArchitecture(
        "Addressables 句柄",
        "独占 Addressables 原生 Handle 的一份引用计数，并确保只释放一次。",
        FrameworkArchitectureLayer.Contracts,
        110,
        typeof(IResourceBackendHandle<>))]
    internal sealed class AddressablesResourceBackendHandle<T> : IResourceBackendHandle<T> where T : Object
    {
        private AsyncOperationHandle<T> m_handle;
        private T m_value;
        private bool m_released;

        public T Value => m_released ? null : m_value;

        public bool IsValid => !m_released && m_handle.IsValid() && m_value != null;

        internal AddressablesResourceBackendHandle(AsyncOperationHandle<T> handle)
        {
            m_handle = handle;
            m_value = handle.Result;
        }

        public void Dispose()
        {
            if (m_released)
            {
                return;
            }

            m_released = true;
            m_value = null;
            if (m_handle.IsValid())
            {
                UnityAddressables.Release(m_handle);
            }

            m_handle = default;
        }
    }
}
