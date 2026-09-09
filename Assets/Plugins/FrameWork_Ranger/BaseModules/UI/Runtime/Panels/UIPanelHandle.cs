namespace FrameWork_Ranger.UI
{
    /// <summary>标识具体所有者、实例及打开轮次；旧句柄不会指向重开的面板。</summary>
    [FrameworkArchitecture("面板句柄", "固定所有者、实例与打开轮次。", FrameworkArchitectureLayer.Contracts, 130)]
    public readonly struct UIPanelHandle
    {
        internal readonly UIPanelRecord Record;
        internal readonly long Generation;
        internal UIPanelHandle(UIPanelRecord record) { Record = record; Generation = record.Generation; }
        public bool IsOpen => Record != null && Record.Context.IsAlive && Record.Open &&
            Record.Generation == Generation && Record.View != null;
        public bool IsPresented => IsOpen && Record.Domain.IsPresented;
        public string Key => Record?.Definition.Key;
        public UIView View => IsOpen ? Record.View : null;
    }
}
