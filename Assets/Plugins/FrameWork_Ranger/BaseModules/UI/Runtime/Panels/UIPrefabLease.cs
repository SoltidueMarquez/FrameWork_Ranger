using System;
using UnityEngine;
namespace FrameWork_Ranger.UI
{
    /// <summary>把资源租约的释放与实例销毁绑定；内部适配不增加公共加载后端。</summary>
    [FrameworkArchitecture("UI Prefab 租约", "适配资源模块租约并幂等释放。", FrameworkArchitectureLayer.RuntimeDriving, 135)]
    internal sealed class UIPrefabLease : IDisposable
    {
        internal readonly GameObject Prefab;
        private Action m_release;
        internal UIPrefabLease(GameObject prefab, Action release) { Prefab = prefab; m_release = release; }
        public void Dispose() { var release = m_release; m_release = null; release?.Invoke(); }
    }
}
