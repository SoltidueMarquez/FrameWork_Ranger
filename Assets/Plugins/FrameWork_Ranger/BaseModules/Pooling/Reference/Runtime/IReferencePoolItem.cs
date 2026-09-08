namespace FrameWork_Ranger.Pooling.Reference
{
    /// <summary>
    /// 引用池对象必须实现的借出与归还生命周期契约。
    /// </summary>
    [FrameworkArchitecture(
        "引用池对象契约",
        "让引用对象在每次借出和归还时显式重置自己的业务状态。",
        FrameworkArchitectureLayer.Contracts,
        120)]
    public interface IReferencePoolItem
    {
        void OnRent();

        void OnReturn();
    }
}
