using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FrameWork_Ranger.Pooling.Reference;
using UnityEngine;

namespace FrameWork_Ranger.Events
{
    /// <summary>Global 唯一事件中心；SO 保存模板身份，监听和借还状态属于运行克隆。</summary>
    [CreateAssetMenu(fileName = "EventModule", menuName = "FrameWork_Ranger/Modules/Event Center")]
    [FrameworkArchitecture("事件中心模块", "提供主线程同步订阅、注销和池化发送入口。",
        FrameworkArchitectureLayer.PublicFacade, 40, typeof(EventRuntime), typeof(ReferencePoolModule))]
    public sealed class EventModule : DirectModuleBase
    {
        private static readonly IReadOnlyList<Type> Dependencies = Array.AsReadOnly(new[] { typeof(ReferencePoolModule) });
        [NonSerialized] private EventRuntime m_runtime;

        /// <summary>由 Core 保证引用池先加载、后卸载。</summary>
        protected override IReadOnlyList<Type> RequiredModuleTypes => Dependencies;

        #region 框架生命周期

        protected override UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Context.ScopeKind != ModuleScopeKind.Global)
                throw new InvalidOperationException("EventModule 只能安装在 GlobalScope。");
            m_runtime = new EventRuntime(Context.GetModule<ReferencePoolModule>());
            return UniTask.CompletedTask;
        }

        protected override async UniTask OnUnloadAsync()
        {
            await m_runtime.ShutdownAsync();
            m_runtime = null;
        }

        #endregion

        #region 公开 API

        /// <summary>按精确类型订阅并去重；仅限已加载中心的主线程，空回调报参数错误。</summary>
        public void Subscribe<T>(Action<T> callback) where T : EventBase
        {
            GetRuntime().Subscribe(callback);
        }

        /// <summary>主线程幂等注销；空回调、未注册回调和已卸载中心均允许清理。</summary>
        public void Unsubscribe<T>(Action<T> callback) where T : EventBase
        {
            m_runtime?.Unsubscribe(callback);
        }

        /// <summary>
        /// 独立借出并初始化载荷，同步通知监听后归还；初始化/清理错误传播，监听错误记录后继续。
        /// 不接受外部载荷，回调不得持有池化实例用于延后处理。
        /// </summary>
        public void Publish<T>(Action<T> initialize = null) where T : EventBase, new()
        {
            GetRuntime().Publish(initialize);
        }

        #endregion

        private EventRuntime GetRuntime()
        {
            return m_runtime ?? throw new InvalidOperationException("EventModule 尚未加载或已卸载，请等待模块就绪。");
        }
    }
}
