using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework_WWJ.ResourceManagement.UnityResources
{
    /// <summary>
    /// 使用 Unity Resources.LoadAsync 的内置资源后端。
    /// </summary>
    [FrameworkArchitecture(
        "Unity Resources Provider",
        "把 Resources.LoadAsync 结果包装为框架后端句柄，不触发跨后端回退。",
        FrameworkArchitectureLayer.RuntimeDriving,
        100,
        typeof(ResourceProviderBase),
        typeof(UnityResourcesBackendHandle<>))]
    [Serializable]
    public sealed class UnityResourcesProvider : ResourceProviderBase
    {
        public override ResourceBackendKind Backend => ResourceBackendKind.UnityResources;

        public override string ProviderName => "Unity Resources";

        protected override async UniTask<IResourceBackendHandle<T>> OnLoadAsync<T>(
            string location,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = Resources.LoadAsync(location, typeof(T));
            await request.ToUniTask(cancellationToken: cancellationToken);
            var value = request.asset as T;
            return new UnityResourcesBackendHandle<T>(value);
        }
    }
}
