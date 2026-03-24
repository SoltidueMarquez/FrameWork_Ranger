using System;
using UnityEngine;

namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 【对象池模块 — 门面（Manager）】
    /// <para>继承 <see cref="HandlerModuleBase{IPoolHandler}"/>：模块本身只负责接入框架生命周期（Born/Init/Update…），
    /// 具体多池管理、缓加载、缩减全在 <see cref="PoolHandler"/>（Handler）里。</para>
    /// <para>使用方式：</para>
    /// <list type="number">
    /// <item>在 <see cref="ModuleCfg"/> 里像 <see cref="AudioModule"/> 一样增加一条模块，类型选 <see cref="PoolModule"/>；</item>
    /// <item>启动后通过 <see cref="Instance"/> 或 Loader 取到的模块引用调用 <see cref="RegisterSpawnPool"/> / <see cref="Spawn"/> / <see cref="Despawn"/>。</item>
    /// </list>
    /// </summary>
    public class PoolModule : HandlerModuleBase<IPoolHandler>
    {
        /// <summary>全局快捷访问：在模块 Born 时赋值，Die 时清空。多实例时以最后 Born 的为准。</summary>
        private static PoolModule m_instance;
        public static PoolModule Instance => m_instance;

        /// <summary>指定用反射创建的具体 Handler 类型，必须为 <see cref="IPoolHandler"/> 的实现类。</summary>
        protected override Type handleType => typeof(PoolHandler);

        protected override void OnVaildBorn()
        {
            base.OnVaildBorn();
            m_instance = this;
            if (enableDebug) Debug.Log("[PoolModule] Born");
        }

        protected override void OnVaildDie()
        {
            if (m_instance == this) m_instance = null;
            base.OnVaildDie();
            if (enableDebug) Debug.Log("[PoolModule] Die");
        }

        #region 对外 API（薄封装，全部转发给 handler）

        /// <summary>按池名生成实例。</summary>
        public GameObject Spawn(string name) => handler?.Spawn(name);

        /// <summary>生成并在取出后执行自定义初始化。</summary>
        public GameObject Spawn(string name, Action<GameObject> onSpawn) => handler?.Spawn(name, onSpawn);

        /// <summary>归还到指定池。</summary>
        public void Despawn(string name, GameObject target) => handler?.Despawn(name, target);

        /// <summary>运行时注册一条池配置。</summary>
        public void RegisterSpawnPool(ObjectPoolItemData itemData) => handler?.RegisterSpawnPool(itemData);

        /// <summary>注销并销毁该池空闲对象。</summary>
        public void UnRegisterSpawnPool(string name) => handler?.UnRegisterSpawnPool(name);

        /// <summary>是否已存在该池名。</summary>
        public bool IsExistSpawnPool(string name) => handler?.IsExistSpawnPool(name) ?? false;

        /// <summary>清空指定池的空闲克隆体。</summary>
        public void Clear(string name) => handler?.Clear(name);

        /// <summary>清空所有池并注销。</summary>
        public void ClearAll() => handler?.ClearAll();

        #endregion
    }
}
