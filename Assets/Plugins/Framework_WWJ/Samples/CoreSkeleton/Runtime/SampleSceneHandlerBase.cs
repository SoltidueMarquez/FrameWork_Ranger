using System;
using Cysharp.Threading.Tasks;

namespace Framework_WWJ.Samples
{
    /// <summary>
    /// 示例 Scene Module 的可替换 Handler 基类。它读取全局时钟，并把结果写回稳定的 Module 门面。
    /// </summary>
    [Serializable]
    public abstract class SampleSceneHandlerBase : ModuleHandlerBase, IModuleUpdate
    {
        /// <summary>
        /// 获取在示例面板中显示的 Handler 标签。
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// 获取每次 Tick 应累计的示例数值。
        /// </summary>
        protected abstract float TickAmount { get; }

        protected override UniTask OnLoadAsync(System.Threading.CancellationToken cancellationToken)
        {
            GetOwner<SampleSceneModule>().ResetRuntime(DisplayName);
            return UniTask.CompletedTask;
        }

        protected override UniTask OnUnloadAsync()
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public void OnModuleUpdate(float deltaTime)
        {
            var globalClock = Context.GetModule<SampleGlobalClockModule>();
            GetOwner<SampleSceneModule>().RecordTick(
                TickAmount * deltaTime,
                globalClock.ElapsedSeconds);
        }
    }
}
