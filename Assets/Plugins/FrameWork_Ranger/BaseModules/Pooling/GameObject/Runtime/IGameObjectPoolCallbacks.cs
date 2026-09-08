namespace FrameWork_Ranger.Pooling.GameObjects
{
    /// <summary>
    /// GameObject 池实例及其子节点可选实现的借出与归还回调。
    /// </summary>
    [FrameworkArchitecture(
        "GameObject 池回调",
        "让 Prefab 根与子节点按稳定正序 Spawn、逆序 Despawn 接收池生命周期。",
        FrameworkArchitectureLayer.Contracts,
        130)]
    public interface IGameObjectPoolCallbacks
    {
        void OnSpawned();

        void OnDespawned();
    }
}
