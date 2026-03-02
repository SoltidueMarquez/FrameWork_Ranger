using Sirenix.OdinInspector;
using UnityEngine;

namespace Plugins.Framework_WWJ
{
    public enum CfgType
    {
        SO,
        Text,
    }
    [System.Serializable]
    public class CfgAssetInfo : AssetInfo
    {
        public override bool ShowAssetDataBase => false;
        public override bool ShowResources => false;
        public override bool ShowAssetBundle => false;

        [LabelText("配置文件类型")]
        public CfgType cfgType;
        
        public CfgAssetInfo(string assetBundleName, string assetPath, string resourcePath, string addressablePath) :
            base(assetBundleName, assetPath, resourcePath, addressablePath)
        {

        }
    }
}