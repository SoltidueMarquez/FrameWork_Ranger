namespace FrameWork_Ranger
{
    /// <summary>
    /// 声明对象需要接收 Unity LateUpdate 时序。
    /// </summary>
    [FrameworkArchitecture(
        "LateUpdate 能力",
        "声明模块或 Handler 需要接收帧末更新。",
        FrameworkArchitectureLayer.Contracts,
        30)]
    public interface IModuleLateUpdate
    {
        /// <summary>
        /// 在 Framework Host 的 LateUpdate 中调用。
        /// </summary>
        /// <param name="deltaTime">当前帧的缩放时间间隔。</param>
        void OnModuleLateUpdate(float deltaTime);
    }
}
