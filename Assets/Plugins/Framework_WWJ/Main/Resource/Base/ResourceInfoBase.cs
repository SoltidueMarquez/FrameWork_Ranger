using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 资源信息基类
    /// </summary>
    [System.Serializable]
    public abstract class ResourceInfoBase
    {
        public virtual bool ShowAssetDataBase => true;
        public virtual bool ShowAssetBundle => true;
        public virtual bool ShowResources => true;
        public virtual bool ShowAddressable => true;
        
        /// <summary>
        /// AssetBundle的名称
        /// </summary>
        [ShowIf(nameof(ShowAssetBundle))] public string assetBundleName;
        /// <summary>
        /// Asset的路径
        /// </summary>
        [ShowIf(nameof(ShowAssetDataBase))] public string assetPath;
        /// <summary>
        /// Resources文件夹中的路径
        /// </summary>
        [ShowIf(nameof(ShowResources))] public string resourcePath;
        /// <summary>
        /// Addressable路径
        /// </summary>
        [ShowIf(nameof(ShowAddressable))] public string addressablePath;

        public ResourceInfoBase()
        {
            
        }

        public ResourceInfoBase(string assetBundleName, string assetPath, string resourcePath,string addressablePath)
        {
            this.assetBundleName = string.IsNullOrEmpty(assetBundleName) ? assetBundleName : assetBundleName.ToLower();
            this.assetPath = assetPath;
            this.resourcePath = resourcePath;
            this.addressablePath = addressablePath;
        }

        /// <summary>
        /// 获取资源的Resource全路径
        /// </summary>
        public string GetResourceFullPath()
        {
            return $"ResourcesPath: Resources/{resourcePath}";
        }
        /// <summary>
        /// 获取资源的AssetBundle全路径
        /// </summary>
        public string GetAssetBundleFullPath(string assetBundleRootPath)
        {
            return $"AssetBundlePath: {assetBundleRootPath}{assetBundleName}  AssetPath:{assetPath}";
        }
        
        /// <summary>
        /// 获取资源的Addressable全路径
        /// </summary>
        public string GetAddressableFullPath(string assetBundleRootPath)
        {
            return $"AddressablePath: {assetBundleRootPath}{addressablePath}";
        }
    }
}