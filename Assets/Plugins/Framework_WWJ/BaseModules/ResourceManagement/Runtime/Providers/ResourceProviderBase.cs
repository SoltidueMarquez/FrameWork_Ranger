using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Framework_WWJ.ResourceManagement
{
    /// <summary>
    /// 资源后端的 Odin 多态配置基类。业务调用者只能经由 ResourceModule 使用后端。
    /// </summary>
    [FrameworkArchitecture(
        "资源 Provider 基类",
        "定义可多态配置的后端初始化、泛型加载和关停契约，隐藏具体后端 API。",
        FrameworkArchitectureLayer.ModuleModel,
        100,
        typeof(ResourceBackendKind),
        typeof(IResourceBackendHandle<>))]
    [Serializable]
    public abstract class ResourceProviderBase
    {
        public abstract ResourceBackendKind Backend { get; }

        public abstract string ProviderName { get; }

        protected virtual UniTask OnInitializeAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        protected abstract UniTask<IResourceBackendHandle<T>> OnLoadAsync<T>(
            string location,
            CancellationToken cancellationToken)
            where T : UnityEngine.Object;

        protected virtual UniTask OnShutdownAsync()
        {
            return UniTask.CompletedTask;
        }

        internal UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            return OnInitializeAsync(cancellationToken);
        }

        internal UniTask<IResourceBackendHandle<T>> LoadAsync<T>(
            string location,
            CancellationToken cancellationToken)
            where T : UnityEngine.Object
        {
            return OnLoadAsync<T>(location, cancellationToken);
        }

        internal UniTask ShutdownAsync()
        {
            return OnShutdownAsync();
        }
    }
}
