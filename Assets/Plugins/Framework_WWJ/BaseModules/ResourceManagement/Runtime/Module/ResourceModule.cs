using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework_WWJ.ResourceManagement
{
    /// <summary>
    /// 跨场景资源借用门面；缓存、后端和运行句柄全部由内嵌 ResourceHandler 管理。
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceModule", menuName = "Framework WWJ/Modules/Resource Management")]
    [FrameworkArchitecture(
        "资源管理模块",
        "以显式后端 ResourceKey 提供异步加载与 Lease 所有权。",
        FrameworkArchitectureLayer.PublicFacade,
        100,
        typeof(ResourceHandler),
        typeof(ResourceKey))]
    public sealed class ResourceModule : HandlerModuleBase<ResourceHandler>
    {
        /// <summary>
        /// 从 ResourceKey 指定的唯一后端异步取得一份独立 Lease。
        /// </summary>
        public UniTask<ResourceLease<T>> AcquireAsync<T>(
            ResourceKey key,
            CancellationToken cancellationToken = default)
            where T : Object
        {
            return Handler.AcquireAsync<T>(key, cancellationToken);
        }

        internal ResourceDiagnosticsSnapshot CreateDiagnosticsSnapshot()
        {
            return Handler.CreateDiagnosticsSnapshot();
        }

        internal IReadOnlyList<ResourceProviderBase> GetConfiguredProviders()
        {
            return Handler.ConfiguredProviders;
        }

        internal bool HasConfiguredHandler => Handler != null;
    }
}
