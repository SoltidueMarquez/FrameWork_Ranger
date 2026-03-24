using System;
using System.Collections.Generic;
using Plugins.Framework_WWJ.Main.Base;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 【对象池配置文件】基于 ScriptableObject，用于在 Inspector 中统一配置所有池子。
    /// 继承自 GeneralSO，支持 Odin 序列化。
    /// </summary>
    [CreateAssetMenu(fileName = "ObjectPoolCfg", menuName = "Framework/Pool/ObjectPoolCfg")]
    public class ObjectPoolCfg : GeneralSO
    {
        /// <summary>所有配置的对象池子项。</summary>
        [LabelText("对象池配置列表"), Searchable]
        public List<ObjectPoolItemData> objectPoolItemDatas = new List<ObjectPoolItemData>();

        /// <summary>
        /// 清理无效数据：
        /// 1) 移除 template 为空的项。
        /// 2) 移除名字重复的项（保留第一项）。
        /// </summary>
        [Button("清理无效/重复数据")]
        public void ClearUnValidData()
        {
            if (objectPoolItemDatas == null) return;

            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = objectPoolItemDatas.Count - 1; i >= 0; i--)
            {
                var item = objectPoolItemDatas[i];
                
                // 1) 检查模板
                if (item.template == null)
                {
                    objectPoolItemDatas.RemoveAt(i);
                    continue;
                }

                // 2) 检查重复名（忽略大小写）
                if (string.IsNullOrWhiteSpace(item.name) || seenNames.Contains(item.name))
                {
                    objectPoolItemDatas.RemoveAt(i);
                }
                else
                {
                    seenNames.Add(item.name);
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
