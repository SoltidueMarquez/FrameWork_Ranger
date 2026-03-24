using System;
using System.Collections.Generic;

namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 【可选优化接口】若池内元素类型 T 实现本接口，则池在“回收”时可用元素自身保存的索引，
    /// 直接对在用列表做交换删除（swap-remove），而不必查字典 <c>m_inUseIndex</c>。
    /// 高频取还、大池场景下可减少哈希与字典写入开销。
    /// </summary>
    public interface IPoolIndexable
    {
        /// <summary>当前对象在池的“在用列表”中的下标，由 <see cref="GeneralPool{T}"/> 在加入在用区时写入。</summary>
        int PoolIndex { get; set; }
    }

    /// <summary>
    /// 【通用对象池】与 Unity 无关的纯 C# 池实现，管理任意引用类型 <typeparamref name="T"/>。
    /// <para><b>结构说明</b></para>
    /// <list type="bullet">
    /// <item><b>空闲区</b>：<c>m_list</c>，可取出的实例堆在这里。</item>
    /// <item><b>在用区</b>：<c>m_useList</c>，已借出、尚未归还的实例；配合字典或 <see cref="IPoolIndexable"/> 实现 O(1) 定位。</item>
    /// </list>
    /// <para><b>线程安全</b>：非线程安全，请在同一线程（如 Unity 主线程）使用。</para>
    /// <para><b>生命周期</b>：实现 <see cref="IDisposable"/>，可调用 <see cref="Dispose"/> 主动清空空闲区并触发销毁回调（与 .NET 标准用法一致，非 Unity 专有接口）。</para>
    /// </summary>
    /// <typeparam name="T">必须是引用类型（<c>class</c>）。</typeparam>
    public sealed class GeneralPool<T> : IDisposable where T : class
    {
        /// <summary>
        /// 池的“规模”计数：在 <see cref="Prepare"/> / <see cref="Expansion"/> / <see cref="Reduction"/> 中维护，表示当前逻辑上的池大小（含扩容累计）。
        /// </summary>
        public int poolSize { get; private set; }

        /// <summary>当前空闲列表中的实例数量（可直接取出的个数）。</summary>
        public int currentFreeCount => m_list.Count;

        /// <summary>当前在用列表中的实例数量（已取出尚未归还的个数）。</summary>
        public int currentUseCount => m_useList.Count;

        // —— 构造时注入的四个回调（由上层如 ObjectPool 传入具体行为）—— //
        /// <summary>需要新实例时调用，返回一个全新的 <typeparamref name="T"/>。</summary>
        private readonly Func<T> m_createFunc;
        /// <summary>从空闲转为“在用”之后调用（例如 GameObject.SetActive(true)）。</summary>
        private readonly Action<T> m_actionOnTakeItem;
        /// <summary>从“在用”归还到空闲之前调用（例如关闭显示、重置数据）。</summary>
        private readonly Action<T> m_actionOnTakeBackItem;
        /// <summary>真正销毁一个实例时调用（例如 Destroy、释放文件句柄）。</summary>
        private readonly Action<T> m_actionOnDestroy;

        /// <summary>空闲区：栈式从尾部取，减少移动元素。</summary>
        private readonly List<T> m_list;
        /// <summary>在用区：记录所有外借中的对象。</summary>
        private readonly List<T> m_useList;
        /// <summary>对象 → 在 <c>m_useList</c> 中的下标；未实现 <see cref="IPoolIndexable"/> 时用字典做 O(1) 查找。</summary>
        private Dictionary<T, int> m_inUseIndex;

        /// <summary>当空闲为 0 时，一次扩容创建多少个新实例（至少为 1）。</summary>
        private int m_autoExpansionAmount;
        /// <summary><see cref="Prepare"/> 时设定的初始规模；<see cref="Reduction"/> 不会把空闲数减到低于“初始预热”以下（避免缩得太狠）。</summary>
        private int m_initSize;

        /// <summary>
        /// 创建通用池。
        /// </summary>
        /// <param name="autoExpansionAmount">空闲不足时，每次扩容新增的数量。</param>
        /// <param name="createFunc">工厂：创建新实例。</param>
        /// <param name="actionOnTakeItem">取出后回调。</param>
        /// <param name="actionOnTakeBackItem">归还前回调。</param>
        /// <param name="actionOnDestroy">销毁实例时回调。</param>
        /// <param name="initialCapacity">列表与字典的初始容量，减少扩容次数。</param>
        public GeneralPool(
            int autoExpansionAmount,
            Func<T> createFunc,
            Action<T> actionOnTakeItem,
            Action<T> actionOnTakeBackItem,
            Action<T> actionOnDestroy,
            int initialCapacity = 16)
        {
            m_autoExpansionAmount = Math.Max(1, autoExpansionAmount);
            m_createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            m_actionOnTakeItem = actionOnTakeItem;
            m_actionOnTakeBackItem = actionOnTakeBackItem;
            m_actionOnDestroy = actionOnDestroy;

            m_list = new List<T>(initialCapacity);
            m_useList = new List<T>(initialCapacity);
            m_inUseIndex = new Dictionary<T, int>(initialCapacity);
        }

        /// <summary>是否“完全空闲”：没有任何实例处于外借状态（在用列表为空）。自动缩减逻辑常据此判断。</summary>
        public bool IsLeisure() => m_useList.Count == 0;

        /// <summary>
        /// 取出一个实例：若空闲列表为空则先按步长扩容；从空闲列表尾部弹出，加入在用区，再调用取出回调。
        /// </summary>
        public T TakeItem()
        {
            if (m_list.Count == 0)
            {
                Expansion(m_autoExpansionAmount);
            }

            int lastIdx = m_list.Count - 1;
            T item = m_list[lastIdx];
            m_list.RemoveAt(lastIdx);

            AddToInUse(item);
            m_actionOnTakeItem?.Invoke(item);
            return item;
        }

        /// <summary>
        /// 同 <see cref="TakeItem"/>，并在取出后额外执行一次自定义委托（例如设置位置、赋值）。
        /// </summary>
        public T TakeItem(Action<T> onTake)
        {
            T item = TakeItem();
            onTake?.Invoke(item);
            return item;
        }

        /// <summary>
        /// 归还一个实例：先执行归还前回调，再放入空闲列表，最后从在用列表中移除。
        /// <para>在用列表删除采用 <b>swap-remove</b>：把最后一个元素填到被删位置，再删末尾，整体为 O(1)。</para>
        /// </summary>
        public void TakeBackItem(T obj)
        {
            if (obj == null) return;

            // 先通知上层“要入池了”（例如关显、清状态）
            m_actionOnTakeBackItem?.Invoke(obj);

            // 放回空闲区
            m_list.Add(obj);

            // —— 从在用区 O(1) 移除：优先走 IPoolIndexable 保存的下标 —— //
            if (obj is IPoolIndexable indexable)
            {
                int idx = indexable.PoolIndex;
                if ((uint)idx < (uint)m_useList.Count && ReferenceEquals(m_useList[idx], obj))
                {
                    int lastIdx = m_useList.Count - 1;
                    if (idx != lastIdx)
                    {
                        T lastItem = m_useList[lastIdx];
                        m_useList[idx] = lastItem;

                        // 被交换到 idx 的元素要更新自己的索引信息
                        if (lastItem is IPoolIndexable lastIndexable)
                            lastIndexable.PoolIndex = idx;
                        else
                            m_inUseIndex[lastItem] = idx;
                    }
                    m_useList.RemoveAt(lastIdx);
                    return;
                }
            }

            // —— 未实现 IPoolIndexable 或索引失效时，用字典定位下标再做 swap-remove —— //
            if (m_inUseIndex.TryGetValue(obj, out int dicIdx))
            {
                int lastIdx = m_useList.Count - 1;
                if (dicIdx != lastIdx)
                {
                    T lastItem = m_useList[lastIdx];
                    m_useList[dicIdx] = lastItem;
                    m_inUseIndex[lastItem] = dicIdx;
                }
                m_useList.RemoveAt(lastIdx);
                m_inUseIndex.Remove(obj);
            }
        }

        /// <summary>
        /// 把所有在用实例逐个归还（会多次触发归还/取出相关回调，最终在用区清空）。
        /// </summary>
        public void TakeBackAll()
        {
            for (int i = m_useList.Count - 1; i >= 0; i--)
            {
                TakeBackItem(m_useList[i]);
            }
            m_useList.Clear();
            m_inUseIndex.Clear();
        }

        /// <summary>
        /// 预热：先 <see cref="Clear"/> 掉空闲区（销毁旧空闲实例），再连续调用工厂创建 <paramref name="count"/> 个实例放入空闲区。
        /// 并记录 <see cref="m_initSize"/>，供后续缩容时保留“最低库存”。
        /// </summary>
        public void Prepare(int count)
        {
            Clear();
            for (int i = 0; i < count; i++)
            {
                T inst = m_createFunc();
                m_list.Add(inst);
            }
            poolSize = count;
            m_initSize = count;
        }

        /// <summary>
        /// 在现有池基础上再创建 <paramref name="amount"/> 个实例加入空闲区（用于缓加载逐帧扩容）。
        /// </summary>
        public void Expansion(int amount)
        {
            amount = Math.Max(1, amount);
            for (int i = 0; i < amount; i++)
            {
                T inst = m_createFunc();
                m_list.Add(inst);
            }
            poolSize += amount;
        }

        /// <summary>
        /// 缩容：从空闲列表尾部每次销毁一个实例，最多销毁 <see cref="m_autoExpansionAmount"/> 个；
        /// 且空闲数量必须大于 <see cref="m_initSize"/>（不把预热出来的底线拆掉）。
        /// </summary>
        public void Reduction()
        {
            int amount = m_autoExpansionAmount;
            int removed = 0;
            while (removed < amount && m_list.Count > m_initSize)
            {
                int idx = m_list.Count - 1;
                T obj = m_list[idx];
                m_list.RemoveAt(idx);
                m_actionOnDestroy?.Invoke(obj);
                removed++;
                poolSize = Math.Max(0, poolSize - 1);
            }
        }

        /// <summary>
        /// 清空空闲区：对每个空闲实例调用销毁回调；不主动归还“在用”实例（它们仍由调用方持有）。
        /// <see cref="poolSize"/> 会调整为当前在用数量。
        /// </summary>
        public void Clear()
        {
            if (m_actionOnDestroy != null)
            {
                for (int i = 0; i < m_list.Count; i++)
                    m_actionOnDestroy(m_list[i]);
            }
            m_list.Clear();
            m_inUseIndex.Clear();
            poolSize = m_useList.Count;
        }

        /// <summary>
        /// 与 <see cref="Clear"/> 相同，符合 IDisposable 习惯用法（using 结束时释放空闲对象）。
        /// </summary>
        public void Dispose() => Clear();

        /// <summary>
        /// 将实例加入在用列表尾部，并写入索引（IPoolIndexable 或字典）。
        /// </summary>
        private void AddToInUse(T instance)
        {
            int idx = m_useList.Count;
            m_useList.Add(instance);

            if (instance is IPoolIndexable indexable)
            {
                indexable.PoolIndex = idx;
            }
            else
            {
                m_inUseIndex[instance] = idx;
            }
        }
    }
}
