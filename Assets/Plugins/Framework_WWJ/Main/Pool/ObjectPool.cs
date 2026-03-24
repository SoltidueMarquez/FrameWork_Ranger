using System;
using System.Collections.Generic;
using UnityEngine;

namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 【GameObject 专用池】在 <see cref="GeneralPool{GameObject}"/> 之上封装 Unity 相关逻辑：
    /// <list type="number">
    /// <item>用 Prefab <see cref="GameObject.Instantiate"/> 创建实例；</item>
    /// <item>统一挂到场景里自动创建的 <c>(dynamic)Pool_池名</c> 节点下，并 <c>DontDestroyOnLoad</c>；</item>
    /// <item>取出时 <c>SetActive(true)</c>，归还时先调 <see cref="IObjectPoolSupport"/> 再 <c>SetActive(false)</c>；</item>
    /// <item>对每个克隆体缓存其身上所有 <see cref="IObjectPoolSupport"/>，避免每次 Spawn/Despawn 都 <c>GetComponents</c>。</item>
    /// </list>
    /// </summary>
    [Serializable]
    public class ObjectPool
    {
        /// <summary>生成时放在远离原点的位置，减少第一帧在摄像机前“闪一下”的概率（可按项目改数值）。</summary>
        private const float SPAWN_POSITION = 5000f;

        /// <summary>构造时传入的池逻辑名，与 <see cref="ObjectPoolItemData.name"/> 一致。</summary>
        public string poolName { get; private set; }
        /// <summary>当前池规模计数，与内部 <see cref="GeneralPool{GameObject}.poolSize"/> 同步。</summary>
        public int poolSize => m_pool.poolSize;

        /// <summary>底层通用池，负责空闲/在用与回调时序。</summary>
        private GeneralPool<GameObject> m_pool;
        /// <summary>克隆源 Prefab。</summary>
        private GameObject m_template;
        /// <summary>本池所有克隆体的父节点 Transform。</summary>
        private Transform m_parent;
        /// <summary>仅用于给实例起名后缀递增，便于调试区分。</summary>
        private int m_index;

        /// <summary>每个 GameObject 实例 → 其实现 <see cref="IObjectPoolSupport"/> 的组件数组（含子节点）。</summary>
        private Dictionary<GameObject, IObjectPoolSupport[]> m_supportCache;
        /// <summary>模板Prefab上若完全找不到任何 IObjectPoolSupport，则全程跳过回调分支（微优化）。</summary>
        private bool m_hasSupportCallbacks;

        /// <summary>
        /// 创建一个 GameObject 对象池。
        /// </summary>
        /// <param name="name">池名，用于父物体命名与上层字典 key。</param>
        /// <param name="template">要克隆的模板；不可为 null。</param>
        /// <param name="autoExpansionAmount">底层池扩容步长，传给 <see cref="GeneralPool{GameObject}"/>。</param>
        /// <param name="rootParent">可选；若指定，池根节点会挂在其下，否则仅在场景根下创建池节点。</param>
        public ObjectPool(string name, GameObject template, int autoExpansionAmount, Transform rootParent = null)
        {
            poolName = name;
            m_template = template;
            m_index = 0;
            m_supportCache = new Dictionary<GameObject, IObjectPoolSupport[]>();

            // 只在模板存在且层级里确实有回调组件时，才在运行时走回调逻辑
            m_hasSupportCallbacks = m_template != null && m_template.GetComponentInChildren<IObjectPoolSupport>(true) != null;

            // 每个池一个独立父物体，Hierarchy 里好辨认、也方便整体 DontDestroyOnLoad
            GameObject parentGo = new GameObject($"(dynamic)Pool_{name}");
            m_parent = parentGo.transform;
            if (rootParent != null) m_parent.SetParent(rootParent);
            GameObject.DontDestroyOnLoad(parentGo);

            // 四个委托对应：新建、取出、归还、销毁
            m_pool = new GeneralPool<GameObject>(
                autoExpansionAmount,
                CreateInstance,
                OnTakeItemEvent,
                OnTakeBackItemEvent,
                OnDestroyEvent
            );
        }

        /// <summary>从池中取一个 GameObject（可能触发扩容）。</summary>
        public GameObject TakeItem() => m_pool.TakeItem();

        /// <summary>取出并在通用池取出流程之后执行自定义初始化。</summary>
        public GameObject TakeItem(Action<GameObject> onTake) => m_pool.TakeItem(onTake);

        /// <summary>归还实例到本池；若对象不属于本池逻辑，仍会被放进底层空闲列表（业务上应保证成对 Spawn/Despawn）。</summary>
        public void TakeBackItem(GameObject go) => m_pool.TakeBackItem(go);

        /// <summary>强制把所有标记为在用的实例走一遍归还流程（常用于切场景前）。</summary>
        public void TakeBackAll() => m_pool.TakeBackAll();

        /// <summary>同步预热：一次性创建 <paramref name="count"/> 个空闲实例。</summary>
        public void Prepare(int count) => m_pool.Prepare(count);

        /// <summary>异步缓加载用：只多造 1 个空闲实例（内部是一次 Expansion(1)）。</summary>
        public void PrepareOne() => m_pool.Expansion(1);

        /// <summary>按通用池规则从空闲尾部销毁多余实例（受 Prepare 时记录的初始下限约束）。</summary>
        public void Reduction() => m_pool.Reduction();

        /// <summary>是否当前没有任何外借中的实例（全在池里睡）。</summary>
        public bool IsLeisure() => m_pool.IsLeisure();

        /// <summary>销毁所有空闲克隆体；不处理仍在外面的实例。</summary>
        public void Clear() => m_pool.Clear();

        #region 供 GeneralPool 注入的四个回调

        /// <summary>工厂：克隆模板，默认 inactive，并视情况缓存 IObjectPoolSupport。</summary>
        private GameObject CreateInstance()
        {
            GameObject go = GameObject.Instantiate(m_template, Vector3.one * SPAWN_POSITION, Quaternion.identity, m_parent);
            m_index++;
            go.name = $"{m_template.name}_{m_index}";
            go.SetActive(false);

            if (m_hasSupportCallbacks)
            {
                CacheSupports(go);
            }

            return go;
        }

        /// <summary>底层已从空闲弹出、即将交给调用方：先激活，再依次 OnTakeItem。</summary>
        private void OnTakeItemEvent(GameObject go)
        {
            if (go == null) return;

            go.SetActive(true);

            if (m_hasSupportCallbacks)
            {
                IObjectPoolSupport[] supports = GetOrCacheSupports(go);
                for (int i = 0; i < supports.Length; i++)
                {
                    try { supports[i].OnTakeItem(); }
                    catch (Exception e) { Debug.LogError($"[ObjectPool] {go.name} OnTakeItem 异常: {e}"); }
                }
            }
        }

        /// <summary>底层归还流程：先业务 OnTakeBack，再关显并挂回池节点下。</summary>
        private void OnTakeBackItemEvent(GameObject go)
        {
            if (go == null) return;

            if (m_hasSupportCallbacks)
            {
                IObjectPoolSupport[] supports = GetOrCacheSupports(go);
                for (int i = 0; i < supports.Length; i++)
                {
                    try { supports[i].OnTakeBack(); }
                    catch (Exception e) { Debug.LogError($"[ObjectPool] {go.name} OnTakeBack 异常: {e}"); }
                }
            }

            go.SetActive(false);
            if (go.transform.parent != m_parent)
            {
                go.transform.SetParent(m_parent, false);
            }
        }

        /// <summary>缩容或 Clear 时真正 Destroy，并去掉缓存条目。</summary>
        private void OnDestroyEvent(GameObject go)
        {
            if (go == null) return;
            m_supportCache.Remove(go);
            GameObject.Destroy(go);
        }

        #endregion

        /// <summary>在实例诞生时把其（及子物体）上所有 IObjectPoolSupport 扫一遍并写入字典。</summary>
        private void CacheSupports(GameObject go)
        {
            if (go == null) return;
            if (!m_supportCache.ContainsKey(go))
            {
                m_supportCache[go] = go.GetComponentsInChildren<IObjectPoolSupport>(true);
            }
        }

        /// <summary>优先读缓存；若没有（例如动态加了组件）则现场扫一遍再缓存。</summary>
        private IObjectPoolSupport[] GetOrCacheSupports(GameObject go)
        {
            if (m_supportCache.TryGetValue(go, out var supports))
            {
                return supports;
            }
            supports = go.GetComponentsInChildren<IObjectPoolSupport>(true);
            m_supportCache[go] = supports;
            return supports;
        }
    }
}
