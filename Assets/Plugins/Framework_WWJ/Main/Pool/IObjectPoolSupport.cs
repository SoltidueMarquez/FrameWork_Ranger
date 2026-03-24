namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 【Prefab 生命周期回调】挂在对象池模板（Prefab）或其子节点上的组件可实现本接口，
    /// 在“从池里取出”和“放回池里”两个时机由 <see cref="ObjectPool"/> 统一调用。
    /// <para>典型用途：取出时重置血量/动画，放回时停粒子、取消订阅，避免逻辑残留在下次取出时出错。</para>
    /// <para>性能：若模板上没有任何实现该接口的组件，池会跳过整段回调查找逻辑。</para>
    /// </summary>
    public interface IObjectPoolSupport
    {
        /// <summary>
        /// 对象已从空闲转为外借状态之后调用（在 <see cref="ObjectPool"/> 里于 <c>SetActive(true)</c> 之后触发）。
        /// 适合写：播放动画、注册事件、重置随机种子等。
        /// </summary>
        void OnTakeItem();

        /// <summary>
        /// 对象即将从外借归还到空闲之前调用（在关显、改父节点之前触发）。
        /// 适合写：停止协程、退订事件、把引用清空等。
        /// </summary>
        void OnTakeBack();
    }
}
