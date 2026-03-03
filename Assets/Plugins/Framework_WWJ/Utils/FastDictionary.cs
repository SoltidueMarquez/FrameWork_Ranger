using System;
using System.Collections.Generic;
using ActFramework_ByHZR.BasicUtil;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 快速字典：用「键→下标」+ 双列表实现字典结构。
    /// 保证按 Key 查找 O(1)，同时支持按下标顺序、零 GC 的 ForEach 遍历；
    /// Remove 时用「与末尾元素交换」再删末尾，避免整段移动。
    /// 仅在编辑器中维护 Preview 字典，用于 Inspector 里以标准 Dictionary 形式展示内容。
    /// </summary>
    public class FastDictionary<TK, TV>
    {
#if UNITY_EDITOR
        /// <summary>
        /// 编辑器专用：在 Inspector 中展示当前所有键值对的「预览字典」。
        /// 内部真实结构是 m_dict + m_keys + m_values，Odin 对 Dictionary 的展示更友好，
        /// 所以每次修改后把当前内容同步到 Preview，便于调试；发布包中不包含此字段。
        /// </summary>
        [HideLabel, HideReferenceObjectPicker, OdinSerialize, ShowInInspector, HideInEditorMode]
        public Dictionary<TK, TV> Preview
        {
            get;
            private set;
        }
#endif
        /// <summary>
        /// 键 → 在 m_keys/m_values 中的下标，用于 O(1) 按 Key 查找。
        /// </summary>
        [HideReferenceObjectPicker, OdinSerialize,HideInEditorMode, LabelText("索引映射"),FoldoutGroup("真值")]
        private readonly Dictionary<TK, int> m_dict = new Dictionary<TK, int>(); // K -> index
        /// <summary>
        /// 键列表，与 m_values 一一对应；遍历时用下标访问，无枚举器分配。
        /// </summary>
        [HideReferenceObjectPicker, OdinSerialize,HideInEditorMode, ShowInInspector,LabelText("Key"),FoldoutGroup("真值")]
        private readonly List<TK> m_keys = new List<TK>();
        /// <summary>
        /// 值列表，与 m_keys 一一对应。
        /// </summary>
        [HideReferenceObjectPicker, OdinSerialize,HideInEditorMode, LabelText("Value"),FoldoutGroup("真值")]
        private readonly List<TV> m_values = new List<TV>();

        #region 构造函数
        
        public FastDictionary()
        {
            m_dict = new Dictionary<TK, int>();
            m_keys = new List<TK>();
            m_values = new List<TV>();
#if UNITY_EDITOR
            Preview = new Dictionary<TK, TV>();
#endif
        }

        public FastDictionary(int capacity)
        {
            m_dict = new Dictionary<TK, int>(capacity);
            m_keys = new List<TK>(capacity);
            m_values = new List<TV>(capacity);
#if UNITY_EDITOR
            Preview = new Dictionary<TK, TV>(capacity);
#endif
        }

        /// <summary>
        /// 拷贝构造：深拷贝键、值列表及索引字典；编辑器下同时拷贝 Preview 供 Inspector 展示。
        /// </summary>
        public FastDictionary(FastDictionary<TK, TV> dic)
        {
            if (dic == null)
                throw new ArgumentNullException(nameof(dic));

            m_keys = new List<TK>(dic.m_keys);
            m_values = new List<TV>(dic.m_values);
            m_dict = new Dictionary<TK, int>(dic.m_dict);

#if UNITY_EDITOR
            if (dic.Preview != null)
                Preview = new Dictionary<TK, TV>(dic.Preview);
#endif
        }

        #endregion

        public int Count => m_keys.Count;

        /// <summary>
        /// 添加或更新：若 key 已存在则只更新对应 value，否则追加到末尾并维护 m_dict 索引。
        /// </summary>
        public void Add(TK key, TV value)
        {
            if (m_dict.ContainsKey(key))
            {
                int idx = m_dict[key];
                m_values[idx] = value;
            }
            else
            {
                m_dict[key] = m_keys.Count;
                m_keys.Add(key);
                m_values.Add(value);
            }
            OnModify();
        }

        /// <summary>
        /// 按 key 移除：用 m_dict 查到下标；若该位置不是末尾，则用末尾元素覆盖该位置再删末尾，
        /// 保证 O(1) 删除且不破坏 m_dict 与 m_keys/m_values 的一致性。
        /// </summary>
        public bool Remove(TK key)
        {
#if UNITY_EDITOR
            if (m_dict == null)
            {
                Debug.LogError($"生命周期错误，非法移除操作");
                return false;
            } 
#endif
            if (!m_dict.TryGetValue(key, out int index)) return false;

            int lastIndex = m_keys.Count - 1;
            // 若删除点不是末尾：用末尾元素覆盖删除位置，并更新该 key 在 m_dict 中的下标，再删末尾，避免整段移动
            if (index != lastIndex)
            {
                TK lastKey = m_keys[lastIndex];
                TV lastValue = m_values[lastIndex];

                m_keys[index] = lastKey;
                m_values[index] = lastValue;

                m_dict[lastKey] = index;
            }
            m_keys.RemoveAt(lastIndex);
            m_values.RemoveAt(lastIndex);
            m_dict.Remove(key);
            OnModify();
            return true;
        }

        public bool TryGetValue(TK key, out TV value)
        {
            value = default;
            if (m_dict.IsEmpty()) return false;
            if (m_dict.TryGetValue(key, out int index))
            {
                value = m_values[index];
                return true;
            }

            return false;
        }

        public bool ContainsKey(TK key) => m_dict.ContainsKey(key);

        public TV this[TK key]
        {
            get
            {
                if (m_dict.TryGetValue(key, out int index))
                    return m_values[index];
                throw new KeyNotFoundException();
            }
            set => Add(key, value);
        }

        /// <summary>
        /// 清空所有键值对，并同步更新 Preview（仅编辑器）。
        /// </summary>
        public void Clear()
        {
            m_dict.Clear();
            m_keys.Clear();
            m_values.Clear();
            OnModify();
        }

        /// <summary>
        /// 按存储顺序遍历所有键值对，使用 for 循环无枚举器分配，适合性能敏感路径。
        /// </summary>
        public void ForEach(Action<TK, TV> action)
        {
            var keys = m_keys;
            var values = m_values;
            for (int i = 0; i < keys.Count; i++)
            {
                action(keys[i], values[i]);
            }
        }

        /// <summary>
        /// 仅遍历值，零 GC。
        /// </summary>
        public void ForEachValue(Action<TV> action)
        {
            var values = m_values;
            for (int i = 0; i < values.Count; i++)
            {
                action(values[i]);
            }
        }
        
        public IReadOnlyList<TK> GetKeyList()
        {
            return m_keys;
        }

        public IReadOnlyList<TV> GetValueList()
        {
            return m_values;
        }

        /// <summary>
        /// 仅遍历键。
        /// </summary>
        public void ForEachKey(Action<TK> action)
        {
            var keys = m_keys;
            for (int i = 0; i < keys.Count; i++)
            {
                action(keys[i]);
            }
        }
        
        /// <summary>
        /// 根据当前 m_keys 重建 m_dict 索引（例如反序列化后或顺序被外部打乱时）。
        /// </summary>
        public void RebuildDictionary()
        {
            m_dict.Clear();
            for (int i = 0; i < m_keys.Count; i++)
                m_dict[m_keys[i]] = i;
            OnModify();
        }

        /// <summary>
        /// 在任意会改变键值对内容的操作之后调用（Add / Remove / Clear / RebuildDictionary）。
        /// 仅在 UNITY_EDITOR 下执行：把当前 FastDictionary 里的所有 K-V 同步到 Preview 字典，
        /// 以便在 Inspector 里以标准 Dictionary 的形式展示，方便调试；发布构建中此段不编译，无运行开销。
        /// </summary>
        private void OnModify()
        {
#if UNITY_EDITOR
            {
                // 若尚未创建预览字典则 new 一个，否则清空后复用，避免每次修改都 new Dictionary 产生 GC
                if (Preview == null)
                    Preview = new Dictionary<TK, TV>();
                else
                    Preview.Clear();
                // 用 ForEach 按当前顺序把全部 (key, value) 填入 Preview，使 Inspector 显示与真实数据一致
                ForEach(PreviewAdd);
            }
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// 供 OnModify 使用：将一对 (k,v) 加入 Preview 字典。
        /// </summary>
        private void PreviewAdd(TK k, TV v)
        {
            Preview.Add(k, v);
        }
#endif
    }
}