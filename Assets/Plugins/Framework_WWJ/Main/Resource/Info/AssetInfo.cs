namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 资源信息
    /// </summary>
    [System.Serializable]
    public class AssetInfo : ResourceInfoBase
    {
        public AssetInfo(string assetBundleName, string assetPath, string resourcePath,string addressablePath) : base(assetBundleName, assetPath, resourcePath,addressablePath)
        {

        }
    }
}