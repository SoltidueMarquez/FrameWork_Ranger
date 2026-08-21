using System;

namespace Framework_WWJ.ResourceManagement
{
    /// <summary>
    /// 同时描述加载后端与后端内位置的稳定资源键。
    /// </summary>
    [FrameworkArchitecture(
        "资源键",
        "以明确后端与位置组成不可变资源身份，并提供校验、相等与哈希语义。",
        FrameworkArchitectureLayer.Contracts,
        120,
        typeof(ResourceBackendKind))]
    public readonly struct ResourceKey : IEquatable<ResourceKey>
    {
        #region 公开属性

        public ResourceBackendKind Backend { get; }

        public string Location { get; }

        public bool IsValid => TryValidate(out _);

        #endregion

        private ResourceKey(ResourceBackendKind backend, string location)
        {
            Backend = backend;
            Location = location;
        }

        /// <summary>
        /// 创建一个 Unity Resources 键。路径应相对于 Resources 目录且不带扩展名。
        /// </summary>
        public static ResourceKey FromResources(string resourcesPath)
        {
            return new ResourceKey(ResourceBackendKind.UnityResources, resourcesPath);
        }

        /// <summary>
        /// 创建一个 Addressables 键。地址保持原值并按 Ordinal 语义比较。
        /// </summary>
        public static ResourceKey FromAddressables(string address)
        {
            return new ResourceKey(ResourceBackendKind.Addressables, address);
        }

        public bool Equals(ResourceKey other)
        {
            return Backend == other.Backend &&
                   string.Equals(Location, other.Location, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ResourceKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Backend * 397) ^
                       (Location == null ? 0 : StringComparer.Ordinal.GetHashCode(Location));
            }
        }

        public override string ToString()
        {
            return $"{Backend}:{Location ?? "<null>"}";
        }

        public static bool operator ==(ResourceKey left, ResourceKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ResourceKey left, ResourceKey right)
        {
            return !left.Equals(right);
        }

        internal bool TryValidate(out string error)
        {
            if (!Enum.IsDefined(typeof(ResourceBackendKind), Backend))
            {
                error = $"资源后端值无效：{Backend}。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Location))
            {
                error = "资源位置不能为空或只包含空白字符。";
                return false;
            }

            if (Backend != ResourceBackendKind.UnityResources)
            {
                error = null;
                return true;
            }

            if (Location[0] == '/' || Location.IndexOf('\\') >= 0)
            {
                error = "Unity Resources 路径必须使用正斜杠，且不能以斜杠开头。";
                return false;
            }

            if (Location.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
            {
                error = "Unity Resources 路径应相对于 Resources 目录，不能包含 Resources/ 前缀。";
                return false;
            }

            var lastSlash = Location.LastIndexOf('/');
            var lastDot = Location.LastIndexOf('.');
            if (lastDot > lastSlash)
            {
                error = "Unity Resources 路径不能包含文件扩展名。";
                return false;
            }

            error = null;
            return true;
        }
    }
}
