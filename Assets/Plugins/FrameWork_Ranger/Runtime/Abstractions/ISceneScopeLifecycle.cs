using System.Threading;
using Cysharp.Threading.Tasks;

namespace FrameWork_Ranger
{
    /// <summary>已加载 Global 模块可选实现的场景作用域能力。由 Runtime 配对调用，不替换 Driver。</summary>
    public interface ISceneScopeLifecycle
    {
        /// <summary>在 Scene 模块加载前建立本轮资源。令牌仅用于建立过程。</summary>
        UniTask OnSceneScopeStartingAsync(FrameworkSceneScopeInfo scope, CancellationToken cancellationToken);

        /// <summary>在 Scene 卸载或回滚前清理本轮资源；即使 Starting 失败也调用，且不可取消。</summary>
        UniTask OnSceneScopeEndingAsync(FrameworkSceneScopeInfo scope);
    }
}
