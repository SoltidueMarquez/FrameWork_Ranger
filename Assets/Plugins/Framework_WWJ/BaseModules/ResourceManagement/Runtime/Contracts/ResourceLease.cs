using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework_WWJ.ResourceManagement
{
    /// <summary>
    /// 表示调用方对一份已加载资源拥有的独立借用权。
    /// </summary>
    public sealed class ResourceLease<T> : IDisposable where T : Object
    {
        private readonly ResourceLeaseState m_state;

        public ResourceKey Key => m_state.Key;

        public T Value => (T)m_state.GetValue();

        public bool IsValid => m_state.IsValid;

        internal ResourceLease(ResourceLeaseState state)
        {
            m_state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Release()
        {
            m_state.Release();
        }

        public void Dispose()
        {
            Release();
        }
    }
}
