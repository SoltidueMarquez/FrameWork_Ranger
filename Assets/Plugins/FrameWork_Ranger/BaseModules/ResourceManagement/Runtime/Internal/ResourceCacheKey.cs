using System;

namespace FrameWork_Ranger.ResourceManagement
{
    /// <summary>
    /// 缓存以完整 ResourceKey 与调用方请求的精确类型共同分区。
    /// </summary>
    [FrameworkArchitecture(
        "资源缓存键",
        "以 ResourceKey 与精确请求类型共同划分缓存和合并加载边界。",
        FrameworkArchitectureLayer.GraphAndScope,
        110,
        typeof(ResourceKey))]
    internal readonly struct ResourceCacheKey : IEquatable<ResourceCacheKey>
    {
        internal ResourceKey ResourceKey { get; }

        internal Type AssetType { get; }

        internal ResourceCacheKey(ResourceKey resourceKey, Type assetType)
        {
            ResourceKey = resourceKey;
            AssetType = assetType ?? throw new ArgumentNullException(nameof(assetType));
        }

        public bool Equals(ResourceCacheKey other)
        {
            return ResourceKey.Equals(other.ResourceKey) && AssetType == other.AssetType;
        }

        public override bool Equals(object obj)
        {
            return obj is ResourceCacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ResourceKey.GetHashCode() * 397) ^ AssetType.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"{ResourceKey}<{AssetType.FullName}>";
        }
    }
}
