using System.Collections.Generic;
using UnityEngine;

namespace FrameWork_Ranger.Pooling.Reference
{
    /// <summary>
    /// GlobalScope 引用池唯一业务门面。
    /// </summary>
    [CreateAssetMenu(fileName = "ReferencePoolModule", menuName = "FrameWork_Ranger/Modules/Reference Pool")]
    [FrameworkArchitecture(
        "引用池模块",
        "向业务层提供精确类型引用对象的严格借还与安全空闲清理。",
        FrameworkArchitectureLayer.PublicFacade,
        125,
        typeof(ReferencePoolHandler),
        typeof(IReferencePoolItem))]
    public sealed class ReferencePoolModule : HandlerModuleBase<ReferencePoolHandler>
    {
        public T Rent<T>() where T : class, IReferencePoolItem, new()
        {
            return Handler.Rent<T>();
        }

        public void Return(IReferencePoolItem item)
        {
            Handler.Return(item);
        }

        public bool TryReturn(IReferencePoolItem item)
        {
            return Handler.TryReturn(item);
        }

        public void ClearInactive<T>() where T : class, IReferencePoolItem
        {
            Handler.ClearInactive<T>();
        }

        internal IReadOnlyList<ReferencePoolDiagnosticsSnapshot> CreateDiagnosticsSnapshots()
        {
            return Handler.CreateDiagnosticsSnapshots();
        }

        internal PoolCapacitySettings GetDefaultCapacity()
        {
            return Handler?.DefaultCapacity;
        }

        internal IReadOnlyList<ReferencePoolTypeOverride> GetTypeOverrides()
        {
            return Handler?.TypeOverrides;
        }

        internal bool HasConfiguredHandler => Handler != null;
    }
}
