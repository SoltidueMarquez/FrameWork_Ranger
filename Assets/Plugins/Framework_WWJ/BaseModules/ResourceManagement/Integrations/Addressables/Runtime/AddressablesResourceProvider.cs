using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace Framework_WWJ.ResourceManagement.Addressables
{
    /// <summary>
    /// 使用 Unity Addressables 的资源后端，原生 Handle 不向 Resource Runtime 或业务层泄漏。
    /// </summary>
    [Serializable]
    public sealed class AddressablesResourceProvider : ResourceProviderBase
    {
        public override ResourceBackendKind Backend => ResourceBackendKind.Addressables;

        public override string ProviderName => "Unity Addressables";

        protected override async UniTask OnInitializeAsync(CancellationToken cancellationToken)
        {
            var handle = UnityAddressables.InitializeAsync(false);
            try
            {
                await handle.ToUniTask(cancellationToken: cancellationToken);
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw handle.OperationException ??
                          new InvalidOperationException("Addressables 初始化没有成功完成。" );
                }
            }
            finally
            {
                if (handle.IsValid())
                {
                    UnityAddressables.Release(handle);
                }
            }
        }

        protected override async UniTask<IResourceBackendHandle<T>> OnLoadAsync<T>(
            string location,
            CancellationToken cancellationToken)
        {
            var handle = UnityAddressables.LoadAssetAsync<T>(location);
            var ownershipTransferred = false;
            try
            {
                await handle.ToUniTask(cancellationToken: cancellationToken);
                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    throw handle.OperationException ??
                          new InvalidOperationException($"Addressables 没有返回资源：{location}。" );
                }

                var backendHandle = new AddressablesResourceBackendHandle<T>(handle);
                ownershipTransferred = true;
                return backendHandle;
            }
            finally
            {
                if (!ownershipTransferred && handle.IsValid())
                {
                    UnityAddressables.Release(handle);
                }
            }
        }
    }
}
