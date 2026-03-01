using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using ActFramework_ByHZR.BasicUtil;
using Plugins.Framework_WWJ.Main.Base;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 通用 SO（ScriptableObject）生命周期回调接口。
    /// 实现此接口的配置类可在框架初始化/反初始化时被统一调用 Init/UnInit，
    /// 用于在静态配置加载或卸载时执行自定义逻辑。
    /// </summary>
    public interface IGenralSOLifeLoopCallBack
    {
        public void Init();
        public void UnInit();
    }
    
    /// <summary>
    /// 模块化配置基类（ScriptableObject）。
    /// 负责：
    ///     ① 提供模块列表（modules）供 MainLoader 注册；
    ///     ② 管理静态配置（cfgDatas）与动态配置（hotCfgDatas）；
    ///     ③ 在框架 Born/Die 时执行静态配置的 Init/UnInit，在 Init 协程中执行动态配置的加载/卸载。
    /// 子类如 MainRuntimeCfg 可重写 modules 以合并多份配置包。
    /// </summary>
    public partial class ModuleCfg : GeneralSO
    {
        /// <summary>
        /// 当前配置下的模块项列表（模块 key、实例、是否启用）。
        /// MainLoaderBase 在 Born 时据此调用 AddModule，子类可合并多包或从 SO 字段组装。
        /// </summary>
        public virtual List<ModuleItemCfg> modules { get; }

        /// <summary>
        /// 静态配置字典：key -> CfgData（SO 引用 + 是否启用）。
        /// 编辑时配置，运行时只读，可被 GetStaticCfgData 合并子包。
        /// </summary>
        [Title("模块化")] 
        [OdinSerialize, NonSerialized, LabelText("静态配置数据")] public Dictionary<string, CfgData> cfgDatas;
        [SerializeField,LabelText("动态配置数据")] public List<HotCfgData> hotCfgDatas;

        /// <summary>
        /// 按类型从静态配置中查找第一个匹配的 T 类型配置（不按 key，仅按类型）。
        /// </summary>
        /// <typeparam name="T"> 继承自 GeneralSO 的配置类型。 </typeparam>
        /// <returns> 找到则返回该 T 实例，否则返回 null。 </returns>
        public T GetCfgStatic<T>() where T : GeneralSO
        {
            foreach (var item in GetStaticCfgData())
            {
                if (item.Value.cfg is T t)
                {
                    return t;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 按 key 从静态配置中获取指定类型的配置。仅当该项启用（on==true）时返回。
        /// </summary>
        /// <typeparam name="T"> 继承自 GeneralSO 的配置类型。 </typeparam>
        /// <param name="key"> 在 cfgDatas 中注册的键。 </param>
        /// <returns> 对应 key 的 T 类型配置实例。 </returns>
        /// <exception cref="Exception"> 未找到或类型不匹配或未启用时抛出。 </exception>
        public T GetCfgStatic<T>(string key) where T : GeneralSO
        {
            if (GetStaticCfgData().TryGetValue(key, out var so))
            {
                if (so.cfg is T t && so.on)
                {
                    return t;
                }
            }
            throw new Exception($"错误：没有找到配置文件 {typeof(T).Name} in key({key})");
            return null;
        }

        /// <summary>
        /// 静态配置初始化：遍历所有已启用的静态配置项，若其 cfg 实现了 IGenralSOLifeLoopCallBack，则调用 Init()。
        /// 通常在 MainLoaderBase.Born() 时调用。
        /// </summary>
        public void StaticCfgInit()
        {
            if (!GetStaticCfgData().IsEmpty())
            {
                foreach (var item in GetStaticCfgData())
                {
                    if (item.Value.on)
                    {
                        if (item.Value.cfg is IGenralSOLifeLoopCallBack initer)
                        {
#if UNITY_EDITOR
                            initer.Init();
#else
                            try
                            {
                                initer.Init();
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"Error:Cfg({item.Value.cfg.name}) Fail Init ");
                            }
#endif

                        }
                    }
                }
            }
        }

        /// <summary>
        /// 静态配置反初始化：对所有已启用的静态配置项，若实现了 IGenralSOLoopCallBack，则调用 UnInit()。
        /// 通常在 MainLoaderBase.Die() 时调用。
        /// </summary>
        public void StaticCfgUnInit()
        {
            if (!GetStaticCfgData().IsEmpty())
            {
                foreach (var item in GetStaticCfgData())
                {
                    if (item.Value.on)
                    {
                        if (item.Value.cfg is IGenralSOLifeLoopCallBack initer)
                        {
                            initer.UnInit();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取合并后的静态配置字典（本配置的 cfgDatas + 子类通过 GetExtraStaticCfgData 返回的额外项）。
        /// 结果会缓存到 m_staticCfgDatas，编辑或 PlayMode 切换后可通过 InvalidateCfgCache 清空缓存。
        /// </summary>
        public Dictionary<string, CfgData> GetStaticCfgData()
        {
            if (m_staticCfgDatas == null)
            {
                m_staticCfgDatas = new Dictionary<string, CfgData>();
                m_staticCfgDatas.SafeAddRange(cfgDatas);
                m_staticCfgDatas.SafeAddRange(GetExtraStaticCfgData());
            }
            return m_staticCfgDatas;
        }
        
        /// <summary>
        /// 动态配置在运行时的加载结果：key -> HotRuntimeData（SO 或文本内容），
        /// 由 DynamicCfgInit 填充，DynamicCfgUnInit 清空。
        /// </summary>
        public Dictionary<string,HotRuntimeData> HotRuntimeDatas { get; private set; }

//         #region 动态配置加载
//         
//         /// <summary>
//         /// 动态配置初始化协程：根据 hotCfgDatas 依次通过 ResourceManager 加载资源；
//         /// 若为 SO 类型则存入 HotRuntimeSOData，若为 Text 则存入 HotRuntimeTextData，并写入 HotRuntimeDatas。
//         /// 在 MainLoaderBase 的 Init 协程末尾（AfterFinishModuleLoading）中调用。
//         /// </summary>
//         public IEnumerator DynamicCfgInit()
//         {
//             if (!hotCfgDatas.IsEmpty())
//             {
// #if UNITY_EDITOR
//                 if (HotRuntimeDatas != null)
//                 {
//                     HotRuntimeDatas.Clear();
//                     HotRuntimeDatas = null;
//                 }
// #endif
//                 HotRuntimeDatas = new Dictionary<string, HotRuntimeData>();
//                 for (int i = 0; i < hotCfgDatas.Count; i++)
//                 {
//                     var hotItem = hotCfgDatas[i];
//                     if (hotItem.assetInfo.cfgType == CfgType.SO)
//                     {
//                         yield return ResourceManager.Instance.LoadAsset<GeneralSO>(hotItem.assetInfo,null, (so) =>
//                         {
//                             HotRuntimeDatas.Add(hotItem.key,new HotRuntimeSOData()
//                             {
//                                 so = so,
//                             });
//                         });
//                     }
//                     else if (hotItem.assetInfo.cfgType == CfgType.Text)
//                     {
//                         yield return ResourceManager.Instance.LoadAsset<TextAsset>(hotItem.assetInfo,null,
//                             (textAsset) =>
//                             {
//                                 HotRuntimeDatas.Add(hotItem.key,new HotRuntimeTextData()
//                                 {
//                                     text = textAsset.text,
//                                 });
//                             });
//                     }
//                 }
//             }
//         }
//
//         public IEnumerator DynamicCfgUnInit()
//         {
//             if (!hotCfgDatas.IsEmpty())
//             {
//                 for (int i = hotCfgDatas.Count - 1; i >= 0; i--)
//                 {
//                     var hotItem = hotCfgDatas[i];
//                     yield return ResourceManager.Instance.UnLoadAsset(hotItem.assetInfo);
//                 }
//                 HotRuntimeDatas.Clear();
//                 HotRuntimeDatas = null;
//             }
//         }
//
//         #endregion
        
        protected virtual Dictionary<string, CfgData> GetExtraStaticCfgData()
        {
            return null;
        } 

        private Dictionary<string, CfgData> m_staticCfgDatas;

        protected void InvalidateCfgCache()
        {
            m_staticCfgDatas = null;
            HotRuntimeDatas = null;
        }
    }
    
    public class CfgData
    {
        public GeneralSO cfg;
        public bool on;
#if UNITY_EDITOR
        [HideInInspector]
        public bool preview;
#endif
    }

    /// <summary>
    /// 动态配置数据
    /// </summary>
    [System.Serializable]
    public class HotCfgData : IKetGetter<string>
    {
        [LabelText("唯一标识")] public string key;
        // [Title("路径配置"), HideLabel] public CfgAssetInfo assetInfo;
        public string Key => key;
    }
    
    /// <summary>
    /// 动态运行配置数据
    /// </summary>
    [System.Serializable]
    public abstract class HotRuntimeData : IKetGetter<string>
    {
        public string Key => key;
        
        public string key;
    }
    
    /// <summary>
    /// 动态运行SO数据
    /// </summary>
    [System.Serializable]
    public class HotRuntimeSOData : HotRuntimeData
    {
        public GeneralSO so;
    }
    
    /// <summary>
    /// 动态运行SO数据
    /// </summary>
    [System.Serializable]
    public class HotRuntimeTextData : HotRuntimeData
    {
        public string text;
    }
    
    public interface IKetGetter<T>
    {
        public T Key { get; }
    }
}