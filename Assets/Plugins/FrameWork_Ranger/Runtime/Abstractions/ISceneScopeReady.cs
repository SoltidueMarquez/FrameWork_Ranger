using System.Threading;
using Cysharp.Threading.Tasks;
namespace FrameWork_Ranger
{
    /// <summary>场景模块及 Driver 加载后处理完成后、Scope 宣布就绪之前的可选通知。</summary>
    [FrameworkArchitecture("场景就绪参与者", "在场景模块加载完成后执行可回滚的就绪工作。", FrameworkArchitectureLayer.Contracts, 45)]
    public interface ISceneScopeReady
    {
        UniTask OnSceneScopeReadyAsync(FrameworkSceneScopeInfo scope, CancellationToken cancellationToken);
    }
}
