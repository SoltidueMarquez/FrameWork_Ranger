namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 模块处理器
    /// IModuleHandler 是模块具体业务逻辑的实现载体。
    /// 逻辑实现：IModule 将具体的逻辑执行通过“委托”的方式交给 IModuleHandler。例如，当 IModule.UpdateHandle() 被触发时，它内部会调用 m_handler.Update()。
    /// 纯逻辑层：Handler 通常是一个纯 C# 类（非 MonoBehaviour），这使得逻辑与 Unity 场景对象解耦，更易于单元测试和复用。
    /// 反向引用：IModuleHandler 拥有一个 module 属性指向它所属的 IModule，这让 Handler 能够访问模块的状态（如优先级、所有者等）。
    /// </summary>
    public interface IModuleHandler
    {
        /// <summary>
        /// 所属的模块
        /// </summary>
        public IModule module { get; set; }
        
        public abstract void Born();
        
        public abstract void Die();
        
        public abstract void Init();
        
        public abstract void UnInit();
        
        public abstract void Update();
        
        public abstract void FixedUpdate();
        
        public abstract void Pause();
        
        public abstract void Run();
    }
    
    /// <summary>
    /// 接口隔离原则：只有涉及 相机跟随、物理同步后处理、UI 布局刷新 等特定逻辑的模块才需要 LateUpdate。
    /// 如果写在 IModuleHandler 里：
    /// 框架的 MainLoader 在每帧遍历所有模块时，必须调用每一个模块的 LateUpdate。
    /// 即使这个模块不需要 LateUpdate（大多数逻辑模块其实都不需要），也会产生函数调用开销。
    /// </summary>
    public interface IModuleHandlerLateUpdateSupport
    {
        public abstract void LateUpdate();
    }
    
    /// <summary>
    /// 在默认情况下，模块的 isLoading 状态是由基类（IModule）通过内部变量 m_isLoading 维护的。通常 Init() 执行完，m_isLoading 就会变回 false。
    /// 但是，如果一个模块的初始化是异步的（比如需要从网络下载资源、加载大型 Prefab），基类的 Init() 方法瞬间就执行完了，此时资源还没到位。
    /// 这时候就需要 IModuleHandlerLoading 来定义加载状态。
    /// 由于 IModuleHandler 最初可能被设计为同步执行，后来发现需要处理异步情况，为了不破坏已有的、大量不涉及异步的 Handler，才单独拆出了这个接口。
    /// </summary>
    public interface IModuleHandlerIInitStatusProvider
    {
        public bool IsLoading { get; }
    }
}