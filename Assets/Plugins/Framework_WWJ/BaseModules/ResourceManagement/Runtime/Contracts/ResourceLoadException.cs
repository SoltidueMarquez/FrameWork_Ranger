using System;

namespace Framework_WWJ.ResourceManagement
{
    /// <summary>
    /// 表示后端已经接管请求，但资源加载失败、结果为空或结果类型不匹配。
    /// </summary>
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
