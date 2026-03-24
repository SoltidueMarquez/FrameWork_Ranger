using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 【对象池逻辑核心】纯数据与逻辑类（非 MonoBehaviour），由 <see cref="PoolModule"/> 在 Born 时反射创建并挂到 <see cref="IModuleHandler.module"/>。
    /// <para>职责划分：</para>
    /// <list type="bullet">
    /// <item>维护 <c>池名 → ObjectPool</c> 字典；</item>
    /// <item><b>缓加载</b>：在 <see cref="Update"/> 里每帧最多预热 <see cref="m_maxSpawnPerFrame"/> 个，避免开局 Instantiate 尖峰；</item>
    /// <item><b>自动缩减</b>：在 <see cref="FixedUpdate"/> 里若某池完全空闲则累加计时，到期调用 <see cref="ObjectPool.Reduction"/>。</item>
    /// </list>
    /// </summary>
    [Serializable]
    public class PoolHandler : IPoolHandler
    {
        /// <summary>反向指向所属模块，便于 Handler 读模块上的配置或开关（与参考框架 IModuleHandler 一致）。</summary>
        public IModule module { get; set; }

        /// <summary>所有已注册池。</summary>
        private Dictionary<string, ObjectPool> m_pools;
        /// <summary>每个池距离下一次触发 <see cref="ObjectPool.Reduction"/> 的剩余倒计时（秒）；键与 <see cref="m_pools"/> 一致。</summary>
        private Dictionary<string, float> m_poolIdleTimers;

        [LabelText("对象池配置")]
        [SerializeField] private ObjectPoolCfg m_poolCfg;

        [Header("缓加载配置")]
        [Tooltip("为 true 时，prepareCount 会拆到多帧完成；为 false 则在 Register 时立刻 Prepare 整批。")]
        [SerializeField] private bool m_enableSlowPrepare = true;
        [Tooltip("每一帧（Update）最多新创建多少个空闲实例，防止单帧卡顿。")]
        [SerializeField] private int m_maxSpawnPerFrame = 5;

        /// <summary>缓加载任务队列：按注册顺序依次把各池预热到目标数量。</summary>
        private List<SlowPrepareTask> m_slowPrepareTasks;
        /// <summary>当前正在处理的任务下标。</summary>
        private int m_currentTaskIndex = -1;
        private bool m_isSlowPrepareRunning = false;

        /// <summary>描述“某一个池还要造多少个实例才算预热完成”。</summary>
        private class SlowPrepareTask
        {
            public string poolName;
            public int targetCount;
            public ObjectPool pool;

            public SlowPrepareTask(string name, int target, ObjectPool poolRef)
            {
                poolName = name;
                targetCount = target;
                pool = poolRef;
            }

            /// <summary>用 <see cref="ObjectPool.poolSize"/> 与目标比：达到或超过即视为本任务完成。</summary>
            public bool IsCompleted() => pool != null && pool.poolSize >= targetCount;
        }

        /// <summary>模块 Born：分配字典与任务列表。</summary>
        public void Born()
        {
            m_pools = new Dictionary<string, ObjectPool>();
            m_poolIdleTimers = new Dictionary<string, float>();
            m_slowPrepareTasks = new List<SlowPrepareTask>();
        }

        /// <summary>模块 Die：清空所有池并置空引用。</summary>
        public void Die()
        {
            ClearAll();
            m_pools = null;
            m_poolIdleTimers = null;
            m_slowPrepareTasks = null;
        }

        /// <summary>模块 Init：自动从关联的 <see cref="ObjectPoolCfg"/> 加载初始池配置。</summary>
        public void Init()
        {
            if (m_poolCfg != null && m_poolCfg.objectPoolItemDatas != null)
            {
                foreach (var item in m_poolCfg.objectPoolItemDatas)
                {
                    RegisterSpawnPool(item);
                }
            }
        }

        /// <summary>模块反初始化：清空池，释放克隆体。</summary>
        public void UnInit()
        {
            ClearAll();
        }

        /// <summary>每帧推进缓加载队列。</summary>
        public void Update()
        {
            UpdateSlowPrepare();
        }

        /// <summary>按固定时间步检查各池是否长期全空闲，以触发缩减。</summary>
        public void FixedUpdate()
        {
            UpdateAutoReduction();
        }

        public void Pause() { }

        public void Run() { }

        #region IPoolHandler 实现

        /// <summary>
        /// 注册新池：重名会打日志并忽略。
        /// 若开启缓加载且需要预热，则只建池并把任务加入 <see cref="m_slowPrepareTasks"/>；否则直接 <see cref="ObjectPool.Prepare"/>。
        /// </summary>
        public void RegisterSpawnPool(ObjectPoolItemData itemData)
        {
            if (itemData == null || itemData.template == null) return;
            if (m_pools.ContainsKey(itemData.name))
            {
                Debug.LogWarning($"[PoolHandler] 对象池 {itemData.name} 已存在。");
                return;
            }

            ObjectPool pool = new ObjectPool(itemData.name, itemData.template, itemData.autoExpansionAmount);
            m_pools.Add(itemData.name, pool);
            m_poolIdleTimers.Add(itemData.name, itemData.autoReductionTime);

            if (m_enableSlowPrepare && itemData.prepareCount > 0)
            {
                m_slowPrepareTasks.Add(new SlowPrepareTask(itemData.name, itemData.prepareCount, pool));
                m_isSlowPrepareRunning = true;
                if (m_currentTaskIndex == -1) m_currentTaskIndex = 0;
            }
            else if (itemData.prepareCount > 0)
            {
                pool.Prepare(itemData.prepareCount);
            }
        }

        /// <summary>注销：清空该池空闲实例并从字典移除。</summary>
        public void UnRegisterSpawnPool(string name)
        {
            if (m_pools.TryGetValue(name, out var pool))
            {
                pool.Clear();
                m_pools.Remove(name);
                m_poolIdleTimers.Remove(name);
            }
        }

        public bool IsExistSpawnPool(string name) => m_pools.ContainsKey(name);

        public GameObject Spawn(string name)
        {
            if (m_pools.TryGetValue(name, out var pool))
            {
                return pool.TakeItem();
            }
            Debug.LogError($"[PoolHandler] 对象池 {name} 不存在。");
            return null;
        }

        public GameObject Spawn(string name, Action<GameObject> onSpawn)
        {
            if (m_pools.TryGetValue(name, out var pool))
            {
                return pool.TakeItem(onSpawn);
            }
            Debug.LogError($"[PoolHandler] 对象池 {name} 不存在。");
            return null;
        }

        public void Despawn(string name, GameObject target)
        {
            if (m_pools.TryGetValue(name, out var pool))
            {
                pool.TakeBackItem(target);
            }
        }

        public void Clear(string name)
        {
            if (m_pools.TryGetValue(name, out var pool)) pool.Clear();
        }

        public void ClearAll()
        {
            if (m_pools == null) return;
            foreach (var pool in m_pools.Values) pool.Clear();
            m_pools.Clear();
            m_poolIdleTimers.Clear();
        }

        #endregion

        /// <summary>
        /// 缓加载主循环：每帧最多执行 <see cref="m_maxSpawnPerFrame"/> 次 <see cref="ObjectPool.PrepareOne"/>，
        /// 按任务顺序直到所有池达到各自的 targetCount。
        /// </summary>
        private void UpdateSlowPrepare()
        {
            if (!m_isSlowPrepareRunning || m_currentTaskIndex >= m_slowPrepareTasks.Count) return;

            int remainingSpawns = m_maxSpawnPerFrame;
            while (remainingSpawns > 0 && m_currentTaskIndex < m_slowPrepareTasks.Count)
            {
                var task = m_slowPrepareTasks[m_currentTaskIndex];
                if (task.IsCompleted())
                {
                    m_currentTaskIndex++;
                    continue;
                }

                task.pool.PrepareOne();
                remainingSpawns--;

                if (task.IsCompleted())
                {
                    m_currentTaskIndex++;
                }
            }

            if (m_currentTaskIndex >= m_slowPrepareTasks.Count)
            {
                m_isSlowPrepareRunning = false;
            }
        }

        /// <summary>
        /// 自动缩减：仅当某池 <see cref="ObjectPool.IsLeisure"/> 为真时递减倒计时；
        /// 归零时调用一次 <see cref="ObjectPool.Reduction"/>。
        /// <para>注意：当前实现未在“有在用实例”时把倒计时重置回配置的满值；若需要“一用池就重置闲置计时”，可在此 else 分支里赋值为 itemData.autoReductionTime（需额外缓存每池配置）。</para>
        /// </summary>
        private void UpdateAutoReduction()
        {
            if (m_pools == null) return;

            foreach (var name in m_pools.Keys)
            {
                var pool = m_pools[name];
                if (pool.IsLeisure())
                {
                    m_poolIdleTimers[name] -= Time.fixedDeltaTime;
                    if (m_poolIdleTimers[name] <= 0)
                    {
                        pool.Reduction();
                        // 可在此将 m_poolIdleTimers[name] 重置为配置间隔，形成“每隔 N 秒缩一批”的节拍
                    }
                }
                else
                {
                    // 可选：有在用对象时认为池不闲置，将计时器重置，避免边用边缩
                }
            }
        }
    }
}
