using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FrameWork_Ranger
{
    /// <summary>
    /// 一个 Global 或 Scene 作用域的运行所有者，负责克隆、生命周期、查询、Tick 缓存与最终销毁。
    /// </summary>
    [FrameworkArchitecture(
        "模块作用域运行时",
        "拥有模块克隆、串行加载、回滚、逆序卸载与 Tick 缓存。",
        FrameworkArchitectureLayer.GraphAndScope,
        60,
        typeof(ModuleRuntimeRecord),
        typeof(FrameworkDriverHandlerBase))]
    internal sealed class ModuleScopeRuntime
    {
        #region 运行时状态

        private readonly FrameworkRuntime m_runtime;
        private readonly List<ModuleRuntimeRecord> m_records = new List<ModuleRuntimeRecord>();
        private readonly List<ModuleRuntimeRecord> m_loadedRecords = new List<ModuleRuntimeRecord>();
        private readonly List<ModuleBase> m_modules = new List<ModuleBase>();
        private readonly List<IModuleUpdate> m_updateTargets = new List<IModuleUpdate>();
        private readonly List<IModuleFixedUpdate> m_fixedUpdateTargets = new List<IModuleFixedUpdate>();
        private readonly List<IModuleLateUpdate> m_lateUpdateTargets = new List<IModuleLateUpdate>();
        private bool m_canTick;
        private bool m_destroyed;

        #endregion

        #region 公开属性

        internal ModuleScopeKind ScopeKind { get; }

        internal IReadOnlyList<ModuleBase> Modules => m_modules;

        #endregion

        internal ModuleScopeRuntime(
            FrameworkRuntime runtime,
            ModuleScopeKind scopeKind,
            IReadOnlyList<ModuleGraphNode> orderedNodes)
        {
            m_runtime = runtime;
            ScopeKind = scopeKind;

            // 图已经完整验证后才进入此处，因此克隆阶段不再重复依赖和重复类型校验。
            for (var i = 0; i < orderedNodes.Count; i++)
            {
                var node = orderedNodes[i];
                var instance = RuntimeObjectUtility.CloneModule(node.Template);
                var context = new ModuleContext(runtime, this);
                instance.BindRuntime(node.Template, context, scopeKind);

                m_records.Add(new ModuleRuntimeRecord(node, instance));
                m_modules.Add(instance);
            }
        }

        #region 框架生命周期

        internal async UniTask LoadAsync(
            FrameworkDriverHandlerBase driverHandler,
            CancellationToken cancellationToken)
        {
            try
            {
                await driverHandler.BeforeScopeLoadAsync(ScopeKind, m_modules, cancellationToken);

                for (var i = 0; i < m_records.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var record = m_records[i];

                    await driverHandler.BeforeModuleLoadAsync(record.Instance, cancellationToken);
                    await record.Instance.LoadAsync(cancellationToken);

                    // AfterModuleLoad 若失败，该模块已经完成自身 Load，必须进入回滚集合。
                    record.IsLoaded = true;
                    m_loadedRecords.Add(record);
                    await driverHandler.AfterModuleLoadAsync(record.Instance, cancellationToken);
                }

                await driverHandler.AfterScopeLoadAsync(ScopeKind, m_modules, cancellationToken);
                BuildTickTargets();
                m_canTick = true;
            }
            catch
            {
                var rollbackErrors = await UnloadLoadedModulesAsync(driverHandler);
                LogCleanupErrors("加载失败回滚", rollbackErrors);
                DestroyAllInstances();
                throw;
            }
        }

        internal async UniTask<IReadOnlyList<Exception>> UnloadAndDestroyAsync(
            FrameworkDriverHandlerBase driverHandler)
        {
            if (m_destroyed)
            {
                return Array.Empty<Exception>();
            }

            var errors = await UnloadLoadedModulesAsync(driverHandler);
            DestroyAllInstances();
            return errors;
        }

        private async UniTask<List<Exception>> UnloadLoadedModulesAsync(
            FrameworkDriverHandlerBase driverHandler)
        {
            var errors = new List<Exception>();
            m_canTick = false;

            await CaptureUnloadErrorAsync(
                () => driverHandler.BeforeScopeUnloadAsync(ScopeKind, m_modules),
                errors);

            for (var i = m_loadedRecords.Count - 1; i >= 0; i--)
            {
                var record = m_loadedRecords[i];
                await CaptureUnloadErrorAsync(
                    () => driverHandler.BeforeModuleUnloadAsync(record.Instance),
                    errors);

                await CaptureUnloadErrorAsync(record.Instance.UnloadAsync, errors);

                await CaptureUnloadErrorAsync(
                    () => driverHandler.AfterModuleUnloadAsync(record.Instance),
                    errors);

                record.IsLoaded = false;
            }

            m_loadedRecords.Clear();
            await CaptureUnloadErrorAsync(
                () => driverHandler.AfterScopeUnloadAsync(ScopeKind, m_modules),
                errors);
            return errors;
        }

        private void DestroyAllInstances()
        {
            if (m_destroyed)
            {
                return;
            }

            m_destroyed = true;
            m_canTick = false;
            m_updateTargets.Clear();
            m_fixedUpdateTargets.Clear();
            m_lateUpdateTargets.Clear();

            for (var i = 0; i < m_records.Count; i++)
            {
                var instance = m_records[i].Instance;
                instance.ReleaseRuntime();
                RuntimeObjectUtility.Destroy(instance);
            }

            m_records.Clear();
            m_modules.Clear();
        }

        #endregion

        #region 模块查询

        internal bool TryGetLoadedModule(Type moduleType, out ModuleBase module)
        {
            for (var i = 0; i < m_records.Count; i++)
            {
                var record = m_records[i];
                if (record.Instance.GetType() == moduleType &&
                    record.Instance.State == ModuleLifecycleState.Loaded)
                {
                    module = record.Instance;
                    return true;
                }
            }

            module = null;
            return false;
        }

        #endregion

        #region Tick 驱动

        internal void TickUpdate(float deltaTime)
        {
            if (!m_canTick)
            {
                return;
            }

            for (var i = 0; i < m_updateTargets.Count; i++)
            {
                try
                {
                    m_updateTargets[i].OnModuleUpdate(deltaTime);
                }
                catch (Exception exception)
                {
                    LogTickException(m_updateTargets[i], "Update", exception);
                }
            }
        }

        internal void TickFixedUpdate(float fixedDeltaTime)
        {
            if (!m_canTick)
            {
                return;
            }

            for (var i = 0; i < m_fixedUpdateTargets.Count; i++)
            {
                try
                {
                    m_fixedUpdateTargets[i].OnModuleFixedUpdate(fixedDeltaTime);
                }
                catch (Exception exception)
                {
                    LogTickException(m_fixedUpdateTargets[i], "FixedUpdate", exception);
                }
            }
        }

        internal void TickLateUpdate(float deltaTime)
        {
            if (!m_canTick)
            {
                return;
            }

            for (var i = 0; i < m_lateUpdateTargets.Count; i++)
            {
                try
                {
                    m_lateUpdateTargets[i].OnModuleLateUpdate(deltaTime);
                }
                catch (Exception exception)
                {
                    LogTickException(m_lateUpdateTargets[i], "LateUpdate", exception);
                }
            }
        }

        private void BuildTickTargets()
        {
            for (var i = 0; i < m_records.Count; i++)
            {
                AppendTickTarget(m_records[i].Instance);
                AppendTickTarget(m_records[i].Instance.GetAdditionalTickTarget());
            }
        }

        private void AppendTickTarget(object target)
        {
            if (target is IModuleUpdate update)
            {
                m_updateTargets.Add(update);
            }

            if (target is IModuleFixedUpdate fixedUpdate)
            {
                m_fixedUpdateTargets.Add(fixedUpdate);
            }

            if (target is IModuleLateUpdate lateUpdate)
            {
                m_lateUpdateTargets.Add(lateUpdate);
            }
        }

        #endregion

        #region 内部实现

        private static async UniTask CaptureUnloadErrorAsync(
            Func<UniTask> operation,
            ICollection<Exception> errors)
        {
            try
            {
                await operation();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        private static void LogCleanupErrors(string phase, IReadOnlyList<Exception> errors)
        {
            for (var i = 0; i < errors.Count; i++)
            {
                Debug.LogError($"[FrameWork_Ranger] {phase}时发生异常，框架将继续清理其余模块。");
                Debug.LogException(errors[i]);
            }
        }

        private void LogTickException(object target, string phase, Exception exception)
        {
            Debug.LogError(
                $"[FrameWork_Ranger] {ScopeKind} Scope 的 {target.GetType().FullName} 在 {phase} 中抛出异常；其他 Tick 目标继续运行。");
            Debug.LogException(exception);
        }

        #endregion
    }
}
