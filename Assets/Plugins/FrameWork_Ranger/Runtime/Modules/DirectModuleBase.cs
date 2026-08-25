using System.Threading;
using Cysharp.Threading.Tasks;

namespace FrameWork_Ranger
{
    /// <summary>
    /// 允许 Module 自身直接实现逻辑的轻量基类，适用于无需 Handler 多态的模块。
    /// </summary>
    [FrameworkArchitecture(
        "直接模块基类",
        "让轻量模块直接实现异步加载与卸载逻辑。",
        FrameworkArchitectureLayer.ModuleModel,
        20)]
    public abstract class DirectModuleBase : ModuleBase
    {
        /// <summary>
        /// 加载模块运行资源。调用失败或取消时应在抛出前清理尚未正式接管的临时资源。
        /// </summary>
        protected virtual UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 释放已经由本模块成功接管的资源。卸载流程不接受取消。
        /// </summary>
        protected virtual UniTask OnUnloadAsync()
        {
            return UniTask.CompletedTask;
        }

        protected sealed override UniTask ExecuteLoadAsync(CancellationToken cancellationToken)
        {
            return OnLoadAsync(cancellationToken);
        }

        protected sealed override UniTask ExecuteUnloadAsync()
        {
            return OnUnloadAsync();
        }
    }
}
