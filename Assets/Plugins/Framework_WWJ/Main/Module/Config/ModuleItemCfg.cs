using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Plugins.Framework_WWJ
{
    [System.Serializable]
    public class ModuleItemCfg
    {
        [OdinSerialize, LabelText("模块Key")] public string moduleKey;
        [OdinSerialize, LabelText("模块")] public IModule module;
        [OdinSerialize, LabelText("是否启用")] public bool on;
#if UNITY_EDITOR
        [HideInInspector]
        public bool preview;
#endif
    }
}