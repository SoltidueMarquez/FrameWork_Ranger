namespace FrameWork_Ranger.UI
{
    /// <summary>受管 UI 的存活范围，不代表另一个 Module。</summary>
    [FrameworkArchitecture("UI 所有者类别", "区分全局与场景存活范围。", FrameworkArchitectureLayer.Contracts, 135)]
    public enum UIOwner { Global, Scene }
    [FrameworkArchitecture("UI 关闭策略", "选择隐藏保留或销毁释放。", FrameworkArchitectureLayer.Configuration, 135)]
    public enum UIClosePolicy { KeepAlive, DestroyOnClose }
    [FrameworkArchitecture("UI 模态范围", "指定受管 UI 输入的限制范围。", FrameworkArchitectureLayer.Configuration, 135)]
    public enum UIModalScope { None, Domain, Global }
}
