using System;

namespace FrameWork_Ranger.ResourceManagement
{
    /// <summary>
    /// 表示后端已经接管请求，但资源加载失败、结果为空或结果类型不匹配。
    /// </summary>
    [FrameworkArchitecture(
        "资源加载异常",
        "携带资源键、请求类型和 Provider 名称，保留资源加载失败的原始原因。",
        FrameworkArchitectureLayer.Contracts,
        140,
        typeof(ResourceKey),
        typeof(ResourceProviderBase))]
    public sealed class ResourceLoadException : Exception
    {
        public ResourceKey Key { get; }

        public Type AssetType { get; }

        public string ProviderName { get; }

        public ResourceLoadException(
            ResourceKey key,
            Type assetType,
            string providerName,
            string message,
            Exception innerException = null)
            : base(message, innerException)
        {
            Key = key;
            AssetType = assetType;
            ProviderName = providerName;
        }
    }
}
