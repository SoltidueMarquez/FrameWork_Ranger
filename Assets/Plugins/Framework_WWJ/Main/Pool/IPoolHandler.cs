using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 【注册一条池时用到的数据】名称、模板、预热数量、扩容步长、自动缩减间隔等。
    /// 可序列化，方便在 Inspector 或配置里填表。
    /// </summary>
    [Serializable]
    public class ObjectPoolItemData
    {
        /// <summary>池的唯一名字，<see cref="IPoolHandler.Spawn"/> / <see cref="IPoolHandler.Despawn"/> 用此字符串索引。</summary>
        [LabelText("池的唯一名字")] public string name;
        /// <summary>克隆用的 Prefab 或场景里的模板对象。</summary>
        [LabelText("模板对象")] public GameObject template;
        /// <summary>启动时要准备（实例化）多少个空闲实例；可与缓加载配合分帧创建。</summary>
        [LabelText("启动时准备实例个数")] public int prepareCount;
        /// <summary>当空闲不够时，一次向 <see cref="GeneralPool{GameObject}"/> 扩容多少个（至少会在内部被规范为 1）。</summary>
        [LabelText("单次扩容个数")] public int autoExpansionAmount = 1;
        /// <summary>自动缩减：池完全无“在用”时，累计闲置多少秒触发一次 <see cref="ObjectPool.Reduction"/>（具体逻辑见 <see cref="PoolHandler"/>）。</summary>
        [LabelText("自动缩减计时")]public float autoReductionTime = 30f;
    }

    /// <summary>
    /// 【对象池业务接口】继承 <see cref="IModuleHandler"/>，由 <see cref="PoolHandler"/> 实现、<see cref="PoolModule"/> 对外转发。
    /// 设计上把“多池字典、缓加载、缩减”放在 Handler，模块只负责挂进框架生命周期。
    /// </summary>
    public interface IPoolHandler : IModuleHandler
    {
        /// <summary>按池名取出一个实例（内部 <see cref="ObjectPool.TakeItem"/>）。</summary>
        GameObject Spawn(string name);
        /// <summary>取出实例并在取出后执行一次自定义逻辑（如设置父节点、坐标）。</summary>
        GameObject Spawn(string name, Action<GameObject> onSpawn);
        /// <summary>将实例归还到指定名称的池。</summary>
        void Despawn(string name, GameObject target);
        /// <summary>运行时注册一个新池；若开启缓加载且 <see cref="ObjectPoolItemData.prepareCount"/> 大于 0，会分帧预热。</summary>
        void RegisterSpawnPool(ObjectPoolItemData itemData);
        /// <summary>注销池并清空该池内空闲实例（在用中的需业务层先 Despawn）。</summary>
        void UnRegisterSpawnPool(string name);
        /// <summary>是否已存在该名字的池。</summary>
        bool IsExistSpawnPool(string name);
        /// <summary>清空指定池的空闲列表（销毁空闲克隆体）。</summary>
        void Clear(string name);
        /// <summary>清空所有已注册池的空闲实例并移除注册。</summary>
        void ClearAll();
    }
}
