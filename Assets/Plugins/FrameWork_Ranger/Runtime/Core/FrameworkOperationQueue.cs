using System;
using Cysharp.Threading.Tasks;

namespace FrameWork_Ranger
{
    /// <summary>
    /// 将 Attach、Detach 与 Shutdown 串行化的主线程异步队列。
    /// 队列门本身永不失败，因此前一项异常不会永久阻塞后续清理或重试。
    /// </summary>
    [FrameworkArchitecture(
        "框架操作队列",
        "保证场景挂载、卸载与 Shutdown 按提交顺序串行执行。",
        FrameworkArchitectureLayer.RuntimeDriving,
        50)]
    internal sealed class FrameworkOperationQueue
    {
        private UniTask m_tail = UniTask.CompletedTask;

        internal UniTask Enqueue(Func<UniTask> operation)
        {
            var previousGate = m_tail;
            var nextGate = new UniTaskCompletionSource();
            var callerCompletion = new UniTaskCompletionSource();
            m_tail = nextGate.Task;

            RunOperationAsync(previousGate, nextGate, callerCompletion, operation).Forget();
            return callerCompletion.Task;
        }

        private static async UniTaskVoid RunOperationAsync(
            UniTask previousGate,
            UniTaskCompletionSource nextGate,
            UniTaskCompletionSource callerCompletion,
            Func<UniTask> operation)
        {
            await previousGate;
            try
            {
                await operation();
                callerCompletion.TrySetResult();
            }
            catch (OperationCanceledException exception)
            {
                callerCompletion.TrySetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                callerCompletion.TrySetException(exception);
            }
            finally
            {
                nextGate.TrySetResult();
            }
        }
    }
}
