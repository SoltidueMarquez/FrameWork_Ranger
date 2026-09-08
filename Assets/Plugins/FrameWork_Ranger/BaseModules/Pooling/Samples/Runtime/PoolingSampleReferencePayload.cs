using FrameWork_Ranger.Pooling.Reference;

namespace FrameWork_Ranger.Pooling.Samples
{
    /// <summary>
    /// 展示未来 EventCenter 可借用、归还并清空状态的引用载荷。
    /// </summary>
    public sealed class PoolingSampleReferencePayload : IReferencePoolItem
    {
        public int Sequence { get; set; }

        public string Message { get; set; }

        public int RentCount { get; private set; }

        public PoolingSampleReferencePayload()
        {
        }

        public void OnRent()
        {
            RentCount++;
        }

        public void OnReturn()
        {
            Sequence = 0;
            Message = null;
        }
    }
}
