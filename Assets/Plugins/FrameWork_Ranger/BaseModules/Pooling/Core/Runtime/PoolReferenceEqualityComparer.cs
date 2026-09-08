using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FrameWork_Ranger.Pooling
{
    /// <summary>
    /// 为池所有权集合提供不受 UnityEngine.Object 运算符影响的严格引用相等语义。
    /// </summary>
    [FrameworkArchitecture(
        "池引用相等比较器",
        "用对象身份而非值相等判断池内、借出、外来和重复归还对象。",
        FrameworkArchitectureLayer.Contracts,
        111)]
    internal sealed class PoolReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        internal static readonly PoolReferenceEqualityComparer<T> Instance =
            new PoolReferenceEqualityComparer<T>();

        private PoolReferenceEqualityComparer()
        {
        }

        public bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
