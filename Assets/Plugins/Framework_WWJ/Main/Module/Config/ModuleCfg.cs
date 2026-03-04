using System;
using System.Collections.Generic;
using Plugins.Framework_WWJ.Main.Base;
using Plugins.Framework_WWJ.Utils;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 通用 SO（ScriptableObject）生命周期回调接口。
    /// - 某些配置 SO 不只是“存数据”，还需要在框架初始化/反初始化时执行自定义逻辑（如注册表、构建索引）。
    /// - 这类 SO 可以实现 IGenralSOLifeLoopCallBack，统一由 ModuleCfg.StaticCfgInit / StaticCfgUnInit 调用，
    ///   这样 MainLoader 不必关心每个配置类的细节，只处理“整体初始化/整体卸载”。
    /// </summary>
    public interface IGenralSOLifeLoopCallBack
    {
        /// <summary>
        /// 当框架整体 Born / 静态配置被加载时调用。
        /// </summary>
        public void Init();

        /// <summary>
        /// 当框架整体 Die / 静态配置被卸载时调用。
        /// </summary>
        public void UnInit();
    }
    
    /// <summary>
    /// 模块化配置基类（ScriptableObject）。
    /// 你可以把它理解为“某一组模块 + 其关联配置”的打包描述对象，主要职责：
    /// 1. 提供模块列表（modules）：
    ///    - MainLoader 在 Born 阶段会读取 modules（List&lt;ModuleItemCfg&gt;），
    ///      按 moduleKey/module/on 把模块注册进框架。
    /// 2. 静态配置管理（cfgDatas）：
    ///    - 将若干 GeneralSO 配置挂在 cfgDatas 里，并用字符串 key 标记；
    ///    - 通过 GetCfgStatic / StaticCfgInit / StaticCfgUnInit 统一管理这些配置的生命周期。
    /// 3. 动态配置管理（hotCfgDatas + HotRuntimeDatas）：
    ///    - 通过 HotCfgData + HotRuntimeData 描述一批“运行时按路径加载”的配置（如热更表、文本）；
    ///    - 具体的加载逻辑在 DynamicCfgInit / DynamicCfgUnInit 中（你目前已按 AB 相关需求将其整体注释掉）。
    ///
    /// 在你当前的精简骨架中：
    /// - 最核心的是 modules（供模块加载使用）；
    /// - cfgDatas / hotCfgDatas 及相关方法属于“配置扩展系统”，可以按需启用或暂时忽略。
    /// 子类如 MainRuntimeCfg 会重写 modules，把多个配置包（mainRuntimeCfgPackages）合并在一起。
    /// </summary>
    public partial class ModuleCfg : GeneralSO
    {
        /// <summary>
        /// 当前配置下的模块项列表（模块 key、实例、是否启用）。
        /// MainLoaderBase 在 Born 时会遍历该列表：
        /// - 对 on == true 的项调用 AddModule(moduleKey, module)；
        /// - 若子类重写了 modules，可以从多个列表/包体合并再返回。
        /// </summary>
        public virtual List<ModuleItemCfg> modules { get; }

        /// <summary>
        /// 静态配置字典：key -> CfgData（SO 引用 + 是否启用）。
        /// - 这些配置一般是“随项目一起构建”的静态 SO（非热更新）；
        /// - 在 Born 时可以通过 StaticCfgInit 统一调用其中实现 IGenralSOLifeLoopCallBack 的配置；
        /// - 在 Die 时通过 StaticCfgUnInit 统一清理。
        /// </summary>
        [Title("模块化")] 
        [OdinSerialize, NonSerialized, LabelText("静态配置数据")] public Dictionary<string, CfgData> cfgDatas;

        /// <summary>
        /// 动态配置数据列表。
        /// - hotCfgDatas 自身只是一组“key + 资源路径信息”的描述；
        /// - 具体的加载逻辑在 DynamicCfgInit / DynamicCfgUnInit 中（当前为满足你精简 AB/动态加载需求，已整体注释）；
        /// - 运行时加载结果会存入 HotRuntimeDatas 字典。
        /// </summary>
        [SerializeField,LabelText("动态配置数据")] public List<HotCfgData> hotCfgDatas;


        #region 静态配置
        
        /// <summary>
        /// 按类型从静态配置中查找第一个匹配的 T 类型配置（不按 key，仅按类型）。
        /// 典型用法：GetCfgStatic&lt;SomeCfgSO&gt;() 直接拿到某个配置 SO。
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
        /// - 适合“多份同类型配置，用 key 区分”的场景，例如多套关卡配置。
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
        }

        /// <summary>
        /// 静态配置初始化：遍历所有已启用的静态配置项，若其 cfg 实现了 IGenralSOLifeLoopCallBack，则调用 Init()。
        /// - 一般在 MainLoaderBase.Born() 中被调用；
        /// - 适合做“索引构建、事件订阅”等一次性的初始化操作。
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
        /// 静态配置反初始化：对所有已启用的静态配置项，若实现了 IGenralSOLifeLoopCallBack，则调用 UnInit()。
        /// - 一般在 MainLoaderBase.Die() 中被调用；
        /// - 用于撤销 Init 中做的注册、释放引用等。
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
        /// - 第一次调用时构建缓存 m_staticCfgDatas；
        /// - 之后的调用直接返回缓存，避免重复分配和遍历；
        /// - 在编辑器中，当 SO 被修改或 PlayMode 状态变化时，通过 InvalidateCfgCache() 使该缓存失效。
        /// </summary>
        public Dictionary<string, CfgData> GetStaticCfgData()
        {
            if (m_staticCfgDatas == null)
            {
                m_staticCfgDatas = new Dictionary<string, CfgData>();
                if (cfgDatas != null)
                    m_staticCfgDatas.SafeAddRange(cfgDatas);
                var extra = GetExtraStaticCfgData();
                if (extra != null)
                    m_staticCfgDatas.SafeAddRange(extra);
            }
            return m_staticCfgDatas;
        }
        
        /// <summary>
        /// 动态配置在运行时的加载结果：key -> HotRuntimeData（SO 或文本内容），
        /// - 由 DynamicCfgInit 填充，DynamicCfgUnInit 清空；
        /// - 你当前已经将具体的加载逻辑整体注释，只保留结构和字段，为将来扩展做准备。
        /// </summary>
        public Dictionary<string,HotRuntimeData> HotRuntimeDatas { get; private set; }

        #endregion

        
        #region 动态配置加载
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
//                     if (hotItem.assetInfo == null)
//                     {
//                         Debug.LogWarning($"[ModuleCfg] DynamicCfgInit 跳过：key={hotItem.key}，assetInfo 为空。");
//                         continue;
//                     }
//                     if (!IsValidAssetInfoForLoad(hotItem.assetInfo))
//                     {
//                         Debug.LogWarning($"[ModuleCfg] DynamicCfgInit 跳过：key={hotItem.key}，当前加载模式下资源路径无效（如 Addressables 下 addressablePath 为空）。");
//                         continue;
//                     }
//                     Coroutine loadRoutine = null;
//                     try
//                     {
//                         if (hotItem.assetInfo.cfgType == CfgType.SO)
//                         {
//                             loadRoutine = ResourceManager.Instance.LoadAsset<GeneralSO>(hotItem.assetInfo, null, (so) =>
//                             {
//                                 HotRuntimeDatas.Add(hotItem.key, new HotRuntimeSOData()
//                                 {
//                                     so = so,
//                                 });
//                             });
//                         }
//                         else if (hotItem.assetInfo.cfgType == CfgType.Text)
//                         {
//                             loadRoutine = ResourceManager.Instance.LoadAsset<TextAsset>(hotItem.assetInfo, null,
//                                 (textAsset) =>
//                                 {
//                                     HotRuntimeDatas.Add(hotItem.key, new HotRuntimeTextData()
//                                     {
//                                         text = textAsset.text,
//                                     });
//                                 });
//                         }
//                     }
//                     catch (Exception ex)
//                     {
//                         ConsoleLogger.LogError($"[ModuleCfg] DynamicCfgInit 加载失败：key={hotItem.key}，{ex.Message}\n{ex.StackTrace}");
//                     }
//
//                     if (loadRoutine != null)
//                         yield return loadRoutine;
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
//         /// <summary>
//         /// 根据当前 ResourceManager 加载模式判断 assetInfo 是否具备有效路径，避免 InvalidKeyException 等错误加载。
//         /// </summary>
//         private static bool IsValidAssetInfoForLoad(ResourceInfoBase assetInfo)
//         {
//             if (assetInfo == null) return false;
//             var mode = ResourceManager.Instance.loadMode;
//             switch (mode)
//             {
//                 case ResourceLoadMode.Addressables:
//                     return !string.IsNullOrEmpty(assetInfo.addressablePath);
//                 case ResourceLoadMode.Resource:
//                     return !string.IsNullOrEmpty(assetInfo.resourcePath);
//                 case ResourceLoadMode.AssetBundle:
//                     return !string.IsNullOrEmpty(assetInfo.assetBundleName) && !string.IsNullOrEmpty(assetInfo.assetPath);
//                 default:
//                     return true;
//             }
//         }
        #endregion

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
        [HideInInspector] public bool preview;
#endif
    }

    /// <summary>
    /// 动态配置数据
    /// </summary>
    [System.Serializable]
    public class HotCfgData : IKetGetter<string>
    {
        [LabelText("唯一标识")] public string key;

        [Title("路径配置"), HideLabel] public CfgAssetInfo assetInfo;
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