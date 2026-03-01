using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Plugins.Framework_WWJ.Main.Base
{
    public interface IGeneralSo
    {
        
    }

    public class GeneralSO : SerializedScriptableObject, IGeneralSo
    {
#if UNITY_EDITOR
        [HideReferenceObjectPicker, HideLabel, Title("SO标签")] public SoTagData soTag;
#endif

    }

    [System.Serializable]
    public struct SoTagData
    {
        public List<SoTag> soTag;
    }
    
    [System.Serializable]
    public struct SoTag
    {
        public string tag;
    }
}