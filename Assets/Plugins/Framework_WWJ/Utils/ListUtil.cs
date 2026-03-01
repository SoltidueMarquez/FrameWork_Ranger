using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ActFramework_ByHZR.BasicUtil
{
    public static class ListUtil
    {
        public static List<T> GetMatch<T>(this List<T> list, Func<T, bool> match)
        {
            var result = new List<T>();
            if (list.IsEmpty() || match == null) return result;
            foreach (var item in list)
            {
                if (match(item))
                {
                    result.Add(item);
                }
            }

            return result;
        }

        public static void AnySafeAdd<T>(this List<T> list, object element)
        {
            if (list == null || element == null) return;
            if (element is T t) list.Add(t);
        }

        public static void SafeInsert<T>(this List<T> list, int index, T element)
        {
            if (list == null || element == null) return;
            if (index >= 0 && index < list.Count)
                list.Insert(index, element);
            else
                list.Add(element);
        }

        public static void SafeAdd<T>(this List<T> list, T element, int count)
        {
            if (list == null || element == null || count <= 0) return;
            for (int i = 0; i < count; i++) list.Add(element);
        }

        public static bool SafeContains<T>(this List<T> list, T element)
        {
            if (list.IsEmpty()) return false;
            return list.Contains(element);
        }

        public static void SafeAdd<T>(this List<T> list, T element)
        {
            if (list == null || element == null) return;
            list.Add(element);
        }

        public static void SafeAddRange<T>(this List<T> list, List<T> element)
        {
            if (list == null || element.IsEmpty()) return;
            list.AddRange(element);
        }

        public static void Operation<T>(this List<T> list, Action<T> action)
        {
            if (list == null || action == null) return;
            foreach (var item in list)
            {
                if (item != null) action(item);
            }
        }

        public static void RemoveClassItem<T>(this List<T> list, T item) where T : class
        {
            if (list.IsEmpty() || item == null) return;
            int index = list.FindIndex(i => i == item);
            if (index != -1) list.RemoveAt(index);
        }

        public static void RemoveLast<T>(this List<T> list)
        {
            if (list.IsEmpty()) return;
            list.RemoveAt(list.Count - 1);
        }

        public static List<T> SubList<T>(this List<T> list, int startIndex, int length)
        {
            var resultList = new List<T>();
            if (list.IsEmpty() || length <= 0 || startIndex >= list.Count) return resultList;

            int end = Mathf.Min(list.Count, startIndex + length);
            for (int i = Mathf.Max(0, startIndex); i < end; i++)
                resultList.Add(list[i]);

            return resultList;
        }

        // 并集
        public static List<T> Union<T>(this List<T> list1, List<T> list2)
        {
            list1 ??= new List<T>();
            list2 ??= new List<T>();
            return list1.Concat(list2).Distinct().ToList();
        }

        // 差集
        public static List<T> Difference<T>(this List<T> list1, List<T> list2)
        {
            list1 ??= new List<T>();
            list2 ??= new List<T>();
            return list1.Except(list2).ToList();
        }

        public static void SafeClear<T>(this List<T> list)
        {
            if (list.IsEmpty()) return;
            list.Clear();
        }

        public static void SafeInit<T>(ref List<T> list)
        {
            if (list == null) list = new List<T>();
        }

        public static void SafeReset<T>(ref List<T> list)
        {
            if (list != null) list.Clear();
            else list = new List<T>();
        }

        public static int GetCount<T>(this List<T> list)
        {
            return list?.Count ?? 0;
        }

        /// <summary>
        /// 向 List&lt;T&gt; 中添加元素（反射版），保持原接口：仅在类型精确可赋时添加
        /// </summary>
        public static void AddToList(object list, object value)
        {
            if (list == null || value == null) return;

            var type = list.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type itemType = type.GetGenericArguments()[0];
                if (itemType.IsInstanceOfType(value))
                {
                    var addMethod = type.GetMethod("Add");
                    addMethod?.Invoke(list, new[] { value });
                }
            }
        }

        /// <summary>
        /// 根据传入类型创建空的 List 实例
        /// </summary>
        public static object CreateEmptyList(Type listValueType)
        {
            if (listValueType == null) return null;
            if (listValueType.IsGenericType && listValueType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type itemType = listValueType.GetGenericArguments()[0];
                var genericListType = typeof(List<>).MakeGenericType(itemType);
                return Activator.CreateInstance(genericListType);
            }

            return null;
        }

        public static void AddWithCheckExist<T>(this List<T> list, T t)
        {
            if (list == null) return;
            if (!list.Contains(t)) list.Add(t);
        }

        public static void RemoveWithCheckExist<T>(this List<T> list, T t)
        {
            if (list == null) return;
            if (list.Contains(t)) list.Remove(t);
        }

        public static void CallElementInvoke<T>(this List<T> list, Action<T> action)
        {
            if (list.IsEmpty() || action == null) return;
            foreach (var item in list)
            {
                if (item == null) continue;
                action(item);
            }
        }

        public static void FixedLength<T>(this List<T> list, int length) where T : new()
        {
            if (list == null) return;
            int target = Mathf.Max(0, length);
            int delta = target - list.Count;
            if (delta > 0)
            {
                for (int i = 0; i < delta; i++) list.Add(new T());
            }
            else
            {
                for (int i = 0; i < -delta; i++)
                {
                    if (list.Count == 0) break;
                    list.RemoveAt(list.Count - 1);
                }
            }
        }

        public static void MoveItem<T>(this List<T> list, int targetIndex, int moveTargetIndex)
        {
            if (list.IsEmpty()) return;
            if (!list.IndexIsValid(targetIndex) ||
                !list.IndexIsValid(Mathf.Clamp(moveTargetIndex, 0, list.Count - 1))) return;

            T value = list[targetIndex];
            list.RemoveAt(targetIndex);
            // 若移除位置在插入位置之前，插入索引需要-1
            int insertIndex = moveTargetIndex;
            if (targetIndex < moveTargetIndex) insertIndex = Mathf.Clamp(moveTargetIndex - 1, 0, list.Count);
            list.Insert(insertIndex, value);
        }

        public static List<T> ExpandList<T>(this List<T> originalList, int newCapacity, Func<T> defaultValue)
        {
            originalList ??= new List<T>(Mathf.Max(0, newCapacity));
            if (newCapacity <= originalList.Count || defaultValue == null) return originalList;

            for (int i = originalList.Count; i < newCapacity; i++)
                originalList.Add(defaultValue());

            return originalList;
        }

        public static T GetMax<T>(this List<T> list, IComparer<T> comparer)
        {
            if (list.IsEmpty()) return default;
            comparer ??= Comparer<T>.Default;
            T best = list[0];
            for (int i = 1; i < list.Count; i++)
                if (comparer.Compare(list[i], best) > 0)
                    best = list[i];
            return best;
        }

        public static T GetMin<T>(this List<T> list, IComparer<T> comparer)
        {
            if (list.IsEmpty()) return default;
            comparer ??= Comparer<T>.Default;
            T best = list[0];
            for (int i = 1; i < list.Count; i++)
                if (comparer.Compare(list[i], best) < 0)
                    best = list[i];
            return best;
        }

        public static T GetMax<T>(this IList<T> list) where T : IComparable<T>
        {
            if (list == null || list.Count == 0) return default;
            T best = list[0];
            for (int i = 1; i < list.Count; i++)
                if (list[i].CompareTo(best) > 0)
                    best = list[i];
            return best;
        }

        public static T GetMin<T>(this IList<T> list) where T : IComparable<T>
        {
            if (list == null || list.Count == 0) return default;
            T best = list[0];
            for (int i = 1; i < list.Count; i++)
                if (list[i].CompareTo(best) < 0)
                    best = list[i];
            return best;
        }

        public static bool ContainsList<T>(this List<T> list, List<T> other)
        {
            if (other.IsEmpty()) return true;
            if (list.IsEmpty()) return false;
            foreach (var item in other)
            {
                if (!list.Contains(item)) return false;
            }

            return true;
        }

        public static T SafeGet<T>(this T[] array, int index)
        {
            if (array == null || array.Length == 0) return default;
            if (index > array.Length - 1) index = array.Length - 1;
            else if (index < 0) index = 0;
            return array[index];
        }

        public static bool IndexIsValid<T>(this List<T> list, int index)
        {
            if (list == null || list.Count == 0) return false;
            if (index < 0 || index > list.Count - 1) return false;
            return true;
        }

        public static int SafeCount<T>(this List<T> list)
        {
            return list?.Count ?? 0;
        }

        public static void MoveItemsToTop<T>(this List<T> list, Func<T, bool> predicate)
        {
            if (list == null || predicate == null) return;
            list.Sort((a, b) =>
            {
                bool aMatch = predicate(a);
                bool bMatch = predicate(b);
                if (aMatch == bMatch) return 0;
                return aMatch ? -1 : 1;
            });
        }

        public static T SafeGetAllowNull<T>(this List<T> list, int index)
        {
            if (list.IsEmpty()) return default;
            if (index < 0 || index > list.Count - 1) return default;
            return list[index];
        }

        public static T SafeGet<T>(this List<T> list, int index)
        {
            if (list.IsEmpty()) return default;
            if (index > list.Count - 1) index = list.Count - 1;
            else if (index < 0) index = 0;
            return list[index];
        }

        public static void SafeSet<T>(ref List<T> list, List<T> value)
        {
            SafeReset(ref list);
            if (value.IsEmpty()) return;
            list.AddRange(value);
        }

        public static T SafeGetLast<T>(this List<T> list)
        {
            if (list.IsEmpty()) return default;
            return list[^1];
        }

        public static bool TrySafeGet<T>(this List<T> list, int index, out T value)
        {
            value = default;
            if (!list.IndexIsValid(index)) return false;
            value = list[index];
            return true;
        }

        public static bool IsEmpty<T>(this ICollection<T> list)
        {
            return list == null || list.Count == 0;
        }

        public static T GetTarget<T>(this List<T> list, Func<T, int> weightHandle)
        {
            if (list.IsEmpty() || weightHandle == null) return default;
            int weight = int.MinValue;
            T result = default;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                int v = weightHandle(item);
                if (i == 0 || v > weight)
                {
                    weight = v;
                    result = item;
                }
            }

            return result;
        }

        public static bool IsIndexOut<T>(this List<T> list, int index)
        {
            if (list.IsEmpty()) return true;
            return index < 0 || index > list.Count - 1;
        }

        /// <summary>
        /// 是否存在交集（任一元素相同即为 true）
        /// </summary>
        public static bool Intersection<T>(this List<T> list, List<T> target)
        {
            if (list.IsEmpty() || target.IsEmpty()) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (target.Contains(list[i])) return true;
            }

            return false;
        }

        public static List<T> GetIntersection<T>(this List<T> list, List<T> target)
        {
            if (list.IsEmpty() || target.IsEmpty()) return new List<T>();
            var result = list.Intersect(target);
            return new List<T>(result);
        }

        /// <summary>
        /// 计算 list 与 target 的交集（去重），结果写入 result。
        /// 使用外部提供的 scratchSet 作为临时 HashSet，避免内部分配。
        /// 要求：scratchSet.Capacity 预先扩到 >= max(list.Count, target.Count)，
        /// 且在调用间被复用，这样可做到 0GC。
        /// </summary>
        public static void GetIntersectionNoAlloc<T>(
            this List<T> list,
            List<T> target,
            List<T> result,
            HashSet<T> scratchSet
        )
        {
            result.Clear();

            if (list == null || target == null) return;
            if (list.Count == 0 || target.Count == 0) return;
            if (scratchSet == null)
                throw new ArgumentNullException(nameof(scratchSet));

            scratchSet.Clear();

            // 用较小的列表建 set，减少操作次数
            List<T> smaller = list.Count <= target.Count ? list : target;
            List<T> larger = ReferenceEquals(smaller, list) ? target : list;

            // 1. smaller 放进 HashSet
            for (int i = 0; i < smaller.Count; i++)
            {
                scratchSet.Add(smaller[i]); // 不分配，只要容量够
            }

            // 2. 遍历 larger，命中即加入结果
            // Remove 返回 true 只会发生一次，所以结果天然去重
            for (int i = 0; i < larger.Count; i++)
            {
                T item = larger[i];
                if (scratchSet.Remove(item))
                {
                    result.Add(item);
                }
            }
        }

        public static List<T> GetList<T>(this List<T> list, Func<T, bool> condition)
        {
            var r = new List<T>();
            if (list.IsEmpty() || condition == null) return r;
            foreach (var item in list)
            {
                if (condition(item)) r.Add(item);
            }

            return r;
        }

        public static bool AddWithoutRepetition<T>(this List<T> list, T target)
        {
            if (list == null) return false;
            if (!list.Contains(target))
            {
                list.Add(target);
                return true;
            }

            return false;
        }

        public static void AddListWithoutRepetition<T>(this List<T> list, IEnumerable<T> target)
        {
            if (list == null || target == null) return;
            foreach (var item in target)
            {
                if (!list.Contains(item)) list.Add(item);
            }
        }

        public static void GetRandomListWithoutRepetitionNoAlloc<T>(this List<T> list, int amount)
        {
            if (list.IsEmpty()) return;
            int n = list.Count;
            if (n == 0 || amount <= 0) return;

            if (amount > n) amount = n; // 超出则取全部

            // 部分 Fisher–Yates：只洗前 amount 段
            for (int i = 0; i < amount; i++)
            {
                // 在 [i, n) 中选一个下标，与 i 交换
                int r = UnityEngine.Random.Range(i, n);
                if (r != i)
                {
                    (list[i], list[r]) = (list[r], list[i]);
                }
            }

            list.RemoveRange(amount, list.Count - amount);
        }

        public static List<T> RemoveWithCondition<T>(this List<T> list, Func<T, bool> removeCondition)
        {
            var instance = new List<T>(list ?? new List<T>());
            if (removeCondition == null) return instance;
            for (int i = instance.Count - 1; i >= 0; i--)
            {
                if (removeCondition(instance[i])) instance.RemoveAt(i);
            }

            return instance;
        }

        public static void RemoveList<T>(this List<T> list, List<T> removeList)
        {
            if (list.IsEmpty() || removeList.IsEmpty()) return;
            foreach (var i in removeList) list.Remove(i);
        }

        public static void RemoveItem<T>(this List<T> list, List<T> removeList)
        {
            if (list.IsEmpty() || removeList.IsEmpty()) return;
            foreach (var i in removeList)
            {
                int index = list.FindIndex(item => Equals(item, i));
                if (index != -1) list.RemoveAt(index);
            }
        }

        /// <summary>
        /// 修复：之前当两元素“相等”时直接返回 false；现在正确比较。
        /// 可选比较器用于排序比较；若提供，会先对两个列表做拷贝并排序（不修改原列表）。
        /// </summary>
        public static bool SimpleIsSame<T>(this List<T> list, List<T> otherList, IComparer<T> comparer) where T : class
        {
            if (list == null && otherList == null) return true;
            if (list == null || otherList == null) return false;
            if (list.Count != otherList.Count) return false;

            IReadOnlyList<T> a = list, b = otherList;
            if (comparer != null)
            {
                var la = new List<T>(list);
                var lb = new List<T>(otherList);
                la.Sort(comparer);
                lb.Sort(comparer);
                a = la;
                b = lb;
            }

            var eq = EqualityComparer<T>.Default;
            for (int i = 0; i < a.Count; i++)
            {
                if (!eq.Equals(a[i], b[i])) return false;
            }

            return true;
        }

        public static bool IsSameWithCompareFunc<T>(this List<T> list, List<T> otherList, Comparison<T> comparison)
        {
            if (list == null && otherList == null) return true;
            if (list == null || otherList == null) return false;
            if (list.Count != otherList.Count) return false;

            IReadOnlyList<T> a = list, b = otherList;
            if (comparison != null)
            {
                var la = new List<T>(list);
                var lb = new List<T>(otherList);
                la.Sort(comparison);
                lb.Sort(comparison);
                a = la;
                b = lb;
            }

            var eq = EqualityComparer<T>.Default;
            for (int i = 0; i < a.Count; i++)
            {
                if (!eq.Equals(a[i], b[i])) return false;
            }

            return true;
        }

        public static bool IsSame<T>(this List<T> list, List<T> otherList, IComparer<T> comparer)
        {
            if (list == null && otherList == null) return true;
            if (list == null || otherList == null) return false;
            if (list.Count != otherList.Count) return false;

            IReadOnlyList<T> a = list, b = otherList;
            if (comparer != null)
            {
                var la = new List<T>(list);
                var lb = new List<T>(otherList);
                la.Sort(comparer);
                lb.Sort(comparer);
                a = la;
                b = lb;
            }

            var eq = EqualityComparer<T>.Default;
            for (int i = 0; i < a.Count; i++)
            {
                if (!eq.Equals(a[i], b[i])) return false;
            }

            return true;
        }


        public static void SafeTranslate<T>(this List<T> list, Action<T> handle)
        {
            if (list.IsEmpty() || handle == null) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                handle(list[i]);
            }
        }

        public static List<(string, int)> GetRandomKeyCounts(List<string> keyList, int randomCount)
        {
            if (keyList == null || keyList.Count == 0 || randomCount <= 0)
                return new List<(string, int)>();

            var rand = new System.Random();
            int keyUsedCount = rand.Next(1, Math.Min(keyList.Count, randomCount) + 1);

            var availableKeys = new List<string>(keyList);
            var selectedKeys = new List<string>();
            for (int i = 0; i < keyUsedCount; i++)
            {
                int index = rand.Next(availableKeys.Count);
                selectedKeys.Add(availableKeys[index]);
                availableKeys.RemoveAt(index);
            }

            List<int> counts = RandomIntPartition(randomCount, keyUsedCount, rand);

            var result = new List<(string, int)>();
            for (int i = 0; i < keyUsedCount; i++)
                result.Add((selectedKeys[i], counts[i]));

            return result;
        }

        private static List<int> RandomIntPartition(int total, int parts, System.Random rand)
        {
            var values = new List<int>();
            int remaining = total;

            for (int i = 0; i < parts - 1; i++)
            {
                int value = rand.Next(1, remaining - (parts - i - 1) + 1);
                values.Add(value);
                remaining -= value;
            }

            values.Add(remaining);

            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }

            return values;
        }

        public static void HForeach<T>(this List<T> list, Action<T> action)
        {
            if (list == null || action == null) return;
            for (int i = 0; i < list.Count; i++) action(list[i]);
        }

        public static void HForeach<T>(this List<T> list, Action<int, T> action)
        {
            if (list == null || action == null) return;
            for (int i = 0; i < list.Count; i++) action(i, list[i]);
        }

        public static void SafeHForeach<T>(this List<T> list, Action<T> action)
        {
            if (list == null || action == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                if (i < list.Count) action(list[i]);
            }
        }

        public static void SafeHForeach<T>(this List<T> list, Action<int, T> action)
        {
            if (list == null || action == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                if (i < list.Count) action(i, list[i]);
            }
        }

        /// <summary>
        /// 按权重生成给定长度的列表，并尽量避免相邻相同、整体更均匀。
        /// </summary>
        public static List<T> RandomGenerateEven<T>(int listCount, Dictionary<T, int> nodeWeight, int? seed = null)
        {
            var result = new List<T>(Mathf.Max(0, listCount));
            if (nodeWeight == null || listCount <= 0) return result;

            // 过滤无效权重
            var weights = new List<KeyValuePair<T, int>>();
            foreach (var kv in nodeWeight)
                if (kv.Value > 0)
                    weights.Add(kv);
            if (weights.Count == 0) return result;

            // 随机源（用于平手打散/洗牌），保证可复现
            System.Random rnd = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

            // --------- 第一步：最大余数法计算目标份额 ----------
            float totalWeight = 0f;
            foreach (var kv in weights) totalWeight += kv.Value;

            var baseCounts = new Dictionary<T, int>();
            var remainders = new List<(T key, float remainder)>();
            int baseSum = 0;

            foreach (var kv in weights)
            {
                float exact = listCount * (kv.Value / totalWeight); // 精确份额
                int baseCnt = Mathf.FloorToInt(exact); // 基础份额
                float rem = exact - baseCnt; // 余数
                baseCounts[kv.Key] = baseCnt;
                baseSum += baseCnt;
                remainders.Add((kv.Key, rem));
            }

            int left = listCount - baseSum; // 还需补多少
            if (left > 0)
            {
                // 余数大的先分；余数相等用随机打散避免偏置
                remainders.Sort((a, b) =>
                {
                    int cmp = b.remainder.CompareTo(a.remainder);
                    if (cmp != 0) return cmp;
                    return rnd.Next(-1, 2); // 打散平手
                });

                for (int i = 0; i < left; i++)
                {
                    var k = remainders[i % remainders.Count].key;
                    baseCounts[k] = baseCounts.TryGetValue(k, out var c) ? c + 1 : 1;
                }
            }

            // 如果所有份额都为 0（极端情况），直接返回空
            int totalAlloc = 0;
            foreach (var v in baseCounts.Values) totalAlloc += v;
            if (totalAlloc == 0) return result;

            // --------- 第二步：优先队列式调度，尽量不相邻 ----------
            // 用 List 充当“堆”：每次排序挑选（元素种类通常很少，开销可接受）
            var entries = new List<(T key, int remaining)>();
            foreach (var kv in baseCounts)
                if (kv.Value > 0)
                    entries.Add((kv.Key, kv.Value));

            // 辅助：把同剩余量的项随机打散，避免图案化
            Comparison<(T key, int remaining)> sorter = (x, y) =>
            {
                int cmp = y.remaining.CompareTo(x.remaining); // desc
                if (cmp != 0) return cmp;
                return rnd.Next(-1, 2); // 同剩余量时随机
            };

            T last = default;
            bool hasLast = false;

            for (int i = 0; i < listCount; i++)
            {
                // 移除掉耗尽的项
                entries.RemoveAll(e => e.remaining <= 0);
                if (entries.Count == 0) break;

                entries.Sort(sorter);

                int pickIdx = 0;

                // 避免和上一个相同：如果第一名和 last 相同，尽量用第二名
                if (hasLast && EqualityComparer<T>.Default.Equals(entries[0].key, last))
                {
                    // 找到第一个 != last 的候选
                    int alt = -1;
                    for (int k = 1; k < entries.Count; k++)
                    {
                        if (!EqualityComparer<T>.Default.Equals(entries[k].key, last))
                        {
                            alt = k;
                            break;
                        }
                    }

                    if (alt != -1) pickIdx = alt;
                    // alt == -1 表示只剩同一元素，无法避免相邻相同 —— 但仍应放入，保证可行性
                }

                var chosen = entries[pickIdx];
                result.Add(chosen.key);
                chosen.remaining--;

                // 写回
                entries[pickIdx] = chosen;
                hasLast = true;
                last = chosen.key;
            }

            return result;
        }

        public static List<T> RandomGenerate<T>(int listCount, Dictionary<T, int> nodeWeight, System.Random rng = null)
        {
            var result = new List<T>(Mathf.Max(0, listCount));
            if (listCount <= 0 || nodeWeight == null || nodeWeight.Count == 0) return result;

            var valid = new List<(T key, double w)>();
            double totalW = 0;
            foreach (var kv in nodeWeight)
            {
                if (kv.Value > 0)
                {
                    valid.Add((kv.Key, kv.Value));
                    totalW += kv.Value;
                }
            }

            if (valid.Count == 0) return result;

            rng ??= new System.Random();
            var quota = new Dictionary<T, double>(valid.Count);
            var cnt = new Dictionary<T, int>(valid.Count);
            int allocated = 0;

            foreach (var (k, w) in valid)
            {
                double q = listCount * (w / totalW);
                quota[k] = q;
                int baseCount = (int)Math.Floor(q);
                cnt[k] = baseCount;
                allocated += baseCount;
            }

            int remain = listCount - allocated;
            if (remain > 0)
            {
                var remainders = new List<(T key, double frac, double r)>(valid.Count);
                foreach (var (k, _) in valid)
                {
                    double frac = quota[k] - Math.Floor(quota[k]);
                    double rkey = rng.NextDouble();
                    remainders.Add((k, frac, rkey));
                }

                remainders.Sort((a, b) =>
                {
                    int cmp = b.frac.CompareTo(a.frac);
                    if (cmp != 0) return cmp;
                    return a.r.CompareTo(b.r);
                });

                for (int i = 0; i < remain && i < remainders.Count; i++)
                    cnt[remainders[i].key]++;
            }

            foreach (var (k, _) in valid)
                for (int i = 0; i < cnt[k]; i++)
                    result.Add(k);

            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            return result;
        }


        /// <summary>
        /// 去重，返回新列表
        /// </summary>
        public static List<T> RemoveDuplicates<T>(this List<T> list)
        {
            if (list == null) return null;
            return list.Distinct().ToList();
        }

    }

    public interface IListItemWeight
    {
        public int Weight { get; }
    }
}
