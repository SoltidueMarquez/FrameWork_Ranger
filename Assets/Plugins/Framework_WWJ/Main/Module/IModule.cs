
namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 初始化状态
    /// </summary>
    public enum InitState
    {
        Sleep,
        Success,
        Loading,
    }
    
    /// <summary>
    /// IModule 是模块对外的统一定义。
    /// 在框架中，它通常由一个继承自 MonoBehaviour 的基类（如 GlobalModuleBase）来实现。
    /// 它是框架底层管理系统（MainLoop）能够识别和调度的基本单位。
    /// 它持有一个 IModuleHandler，并负责管理这个处理器的创建和销毁。
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool isLoading { get; }
        
        /// <summary>
        /// 目前初始化状态
        /// </summary>
        public InitState currentInitState { get; }
        
        /// <summary>
        /// 初始化优先级
        /// </summary>
        public int initPriority { get; }
        
        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool isRunning { get; }
        
        public void Born();
        public void Die();
        
        public void BeginInit();
        public void Init();
        public void EndInit();
        
        public void BeginUnInit();
        public void UnInit();
        public void EndUnInit();
        
        public void UpdateHandle();
        public void FixedUpdateHandle();
        
        public void Pause();
        public void Run();
    }
}
