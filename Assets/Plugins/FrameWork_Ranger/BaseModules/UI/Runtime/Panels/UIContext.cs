using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FrameWork_Ranger.UI
{
    /// <summary>一个 Global/Scene 轮次的面板所有者。公开操作必须在 Unity 主线程调用。</summary>
    [FrameworkArchitecture("UI 所有者上下文", "按 owner/key 管理唯一实例、异步打开和清理。",
        FrameworkArchitectureLayer.Contracts, 130, typeof(UIPanelHandle), typeof(UIView))]
    public sealed class UIContext
    {
        internal readonly GlobalUIModule Module;
        internal readonly GameObject Root;
        private readonly Dictionary<string, UIDomain> m_domains = new Dictionary<string, UIDomain>(StringComparer.Ordinal);
        private readonly Dictionary<string, UIPanelDefinition> m_definitions = new Dictionary<string, UIPanelDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, UIPanelRecord> m_records = new Dictionary<string, UIPanelRecord>(StringComparer.Ordinal);
        private readonly Dictionary<string, OpenOperation> m_operations = new Dictionary<string, OpenOperation>(StringComparer.Ordinal);
        private int m_pendingLoads, m_pendingReleases;
        private readonly List<Exception> m_cleanupErrors = new List<Exception>();
        public UIOwner Owner { get; }
        public bool IsAlive { get; private set; } = true;

        private sealed class Request
        {
            internal readonly UniTaskCompletionSource<UIPanelHandle> Completion = new UniTaskCompletionSource<UIPanelHandle>();
            internal object Data;
            internal UIPanelHandle Parent;
            internal CancellationToken Token;
            internal CancellationTokenRegistration Registration;
        }
        private sealed class OpenOperation
        {
            internal readonly CancellationTokenSource Cancellation = new CancellationTokenSource();
            internal readonly List<Request> Requests = new List<Request>();
            internal UIPanelRecord Record;
            internal bool Done;
        }
        internal UIContext(GlobalUIModule module, UIOwner owner, UIConfiguration configuration, Transform parent)
        {
            Module = module; Owner = owner;
            Root = new GameObject(owner + " UI");
            Root.transform.SetParent(parent, false);
            foreach (var domain in configuration.Domains) m_domains.Add(domain.Key, new UIDomain(this, domain));
            foreach (var panel in configuration.Panels) m_definitions.Add(panel.Key, panel);
        }
        private void RequireAlive()
        {
            if (!PlayerLoopHelper.IsMainThread) throw new InvalidOperationException("UI 操作必须在 Unity 主线程执行。");
            if (!IsAlive) throw new InvalidOperationException("此 UI 所有者已经结束，不能操作新场景。");
        }
        public UniTask<UIPanelHandle> OpenAsync(string panelKey, object data = null, CancellationToken cancellationToken = default)
            => Enqueue(panelKey, data, default, cancellationToken);
        public UniTask<UIPanelHandle> OpenChildAsync(UIPanelHandle parent, string panelKey,
            object data = null, CancellationToken cancellationToken = default)
        {
            RequireHandle(parent);
            return Enqueue(panelKey, data, parent, cancellationToken);
        }
        private UniTask<UIPanelHandle> Enqueue(string key, object data, UIPanelHandle parent, CancellationToken token)
        {
            RequireAlive(); token.ThrowIfCancellationRequested();
            if (!m_definitions.TryGetValue(key, out var definition)) throw new KeyNotFoundException("未配置 UI 面板：" + key);
            if (m_records.TryGetValue(key, out var transitioning) && transitioning.Closing)
                return ReopenAfterCloseAsync(transitioning, key, data, parent, token);
            if (parent.Record != null && (parent.Record.Domain.Definition.Key != definition.DomainKey || parent.Key == key))
                throw new InvalidOperationException("依附面板必须同 owner/域，且不能依附自身。");
            if (parent.Record != null && definition.Layer < parent.Record.Definition.Layer)
                throw new InvalidOperationException("依附弹窗的显示层不能低于父面板。");
            if (m_records.TryGetValue(key, out var record) && record.Open && !record.Entering)
            {
                ValidateParent(record, parent);
                Refresh(record.Handle, data); BringToFront(record.Handle);
                return UniTask.FromResult(record.Handle);
            }
            m_domains[definition.DomainKey].RequireBinding();
            if (!m_operations.TryGetValue(key, out var operation))
            {
                operation = new OpenOperation();
                m_operations.Add(key, operation);
            }
            var request = new Request { Data = data, Parent = parent, Token = token };
            operation.Requests.Add(request);
            if (token.CanBeCanceled)
                request.Registration = token.Register(() =>
                {
                    if (PlayerLoopHelper.IsMainThread) CancelIfUnobserved(key, operation);
                    else UniTask.Post(() => CancelIfUnobserved(key, operation));
                });
            if (operation.Requests.Count == 1) ProcessOpenAsync(key, definition, operation).Forget();
            return token.CanBeCanceled ? request.Completion.Task.AttachExternalCancellation(token) : request.Completion.Task;
        }

        private async UniTask ProcessOpenAsync(string key, UIPanelDefinition definition, OpenOperation operation)
        {
            m_pendingLoads++;
            UIPanelRecord record = null;
            UIPrefabLease lease = null;
            GameObject host = null;
            long ownedGeneration = 0;
            try
            {
                if (!m_records.TryGetValue(key, out record))
                {
                    lease = await Module.LoadPrefabAsync(definition, operation.Cancellation.Token);
                    operation.Cancellation.Token.ThrowIfCancellationRequested();
                    RequireAlive();
                    if (!HasWaiter(operation)) throw new OperationCanceledException();
                    var domain = m_domains[definition.DomainKey];
                    domain.RequireBinding();
                    if (!lease.Prefab) throw new InvalidOperationException("UI Prefab 为空：" + key);
                    host = new GameObject(key, typeof(RectTransform), typeof(CanvasGroup));
                    host.SetActive(false);
                    host.transform.SetParent(domain.GetLayer(definition.Layer), false);
                    var rect = (RectTransform)host.transform;
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                    var presentation = new GameObject("Presentation", typeof(RectTransform), typeof(CanvasGroup));
                    presentation.transform.SetParent(host.transform, false);
                    var visual = (RectTransform)presentation.transform;
                    visual.anchorMin = Vector2.zero; visual.anchorMax = Vector2.one; visual.offsetMin = visual.offsetMax = Vector2.zero;
                    var instance = UnityEngine.Object.Instantiate(lease.Prefab, visual, false);
                    instance.SetActive(true);
                    var view = instance.GetComponent<UIView>();
                    var logic = (UILogic)SerializationUtility.CreateCopy(definition.Logic ?? new UILogic());
                    if (!view || !logic.ViewType.IsInstanceOfType(view))
                        throw new InvalidOperationException(key + ": Prefab 根缺少匹配 Logic 的 UIView。");
                    ValidateView(view);
                    record = new UIPanelRecord { Context = this, Domain = domain, Definition = definition,
                        Host = host, Presentation = visual, View = view, Logic = logic, Gate = host.GetComponent<CanvasGroup>(), Lease = lease };
                    lease = null; host = null;
                    m_records.Add(key, record);
                    operation.Record = record; record.OpeningOperation = operation;
                    logic.Bind(view, this);
                    logic.OnCreate();
                    if (record.Disposed || !IsAlive) throw new OperationCanceledException();
                }
                else { operation.Record = record; record.OpeningOperation = operation; }
                for (int i = 0; i < operation.Requests.Count; i++)
                {
                    var request = operation.Requests[i];
                    if (request.Token.IsCancellationRequested)
                    { request.Completion.TrySetCanceled(request.Token); request.Data = null; continue; }
                    operation.Cancellation.Token.ThrowIfCancellationRequested();
                    RequireAlive();
                    ValidateParent(record, request.Parent);
                    ownedGeneration = record.Open ? record.Generation : record.Generation + 1;
                    if (record.Open) { Refresh(record.Handle, request.Data); BringToFront(record.Handle); }
                    else await OpenRecordAsync(record, request.Parent, request.Data, operation.Cancellation.Token);
                    if (!record.Open || record.Closing || record.Disposed || !IsAlive || record.Generation != ownedGeneration)
                        throw new OperationCanceledException();
                    request.Completion.TrySetResult(record.Handle);
                    request.Data = null;
                }
                if (!record.Open) DestroyRecord(record);
            }
            catch (Exception ex)
            {
                if (record != null && record.OpeningOperation == operation && !record.Closing && (!record.Open || record.Entering)) CloseRecord(record, true);
                foreach (var request in operation.Requests)
                {
                    if (ex is OperationCanceledException || request.Token.IsCancellationRequested) request.Completion.TrySetCanceled(request.Token);
                    else request.Completion.TrySetException(ex);
                    request.Data = null;
                }
            }
            finally
            {
                operation.Done = true;
                if (record != null && record.OpeningOperation == operation) record.OpeningOperation = null;
                foreach (var request in operation.Requests) request.Registration.Dispose();
                if (host || lease != null) Retire(host, lease);
                if (m_operations.TryGetValue(key, out var current) && current == operation) m_operations.Remove(key);
                operation.Cancellation.Dispose(); m_pendingLoads--;
            }
        }
        private static bool HasWaiter(OpenOperation operation)
        {
            foreach (var request in operation.Requests) if (!request.Token.IsCancellationRequested) return true;
            return false;
        }
        private void CancelIfUnobserved(string key, OpenOperation operation)
        {
            if (operation.Done || HasWaiter(operation)) return;
            if (operation.Record != null && operation.Record.OpeningOperation == operation)
                CloseRecord(operation.Record, true);
            if (m_operations.TryGetValue(key, out var current) && current == operation) m_operations.Remove(key);
            operation.Cancellation.Cancel();
        }
        private void ValidateParent(UIPanelRecord record, UIPanelHandle parent)
        {
            if (parent.Record != null) RequireHandle(parent);
            if (record.Open && record.Parent != parent.Record)
                throw new InvalidOperationException("不能抢占已打开面板的依附关系。");
        }
        private async UniTask OpenRecordAsync(UIPanelRecord record, UIPanelHandle parent, object data, CancellationToken token)
        {
            record.Domain.RequireBinding();
            if (parent.Record != null)
            {
                RequireHandle(parent);
                for (var ancestor = parent.Record; ancestor != null; ancestor = ancestor.Parent)
                    if (ancestor == record) throw new InvalidOperationException("面板依附不能成环。");
            }
            if (!string.IsNullOrEmpty(record.Definition.MutexGroup))
            {
                foreach (var other in new List<UIPanelRecord>(m_records.Values))
                {
                    if (other == record || !other.Open || other.Domain != record.Domain ||
                        other.Definition.MutexGroup != record.Definition.MutexGroup) continue;
                    for (var ancestor = parent.Record; ancestor != null; ancestor = ancestor.Parent)
                        if (ancestor == other) throw new InvalidOperationException("子面板不能互斥关闭自身祖先。");
                    await CloseRecordAsync(other).AttachExternalCancellation(token);
                }
            }
            token.ThrowIfCancellationRequested();
            RequireAlive();
            if (parent.Record != null) RequireHandle(parent);
            if (record.Disposed) throw new OperationCanceledException();
            record.Parent = parent.Record; record.Parent?.Children.Add(record);
            record.Generation++; record.Open = true; record.Entering = true;
            long generation = record.Generation;
            record.CloseFinished = false; record.ForceClose = false;
            record.AnimationCancellation?.Dispose();
            record.AnimationCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
            var enteringToken = record.AnimationCancellation.Token;
            record.Host.SetActive(true);
            try
            {
                record.Logic.OnOpen(data);
                if (!record.Open || record.Closing || record.Disposed || record.Generation != generation)
                    throw new OperationCanceledException();
                BringToFront(record.Handle);
                await UIPanelPresentation.PlayAsync(record, record.Definition.EnterAnimation, true, enteringToken);
                record.Entering = false;
                Module.Pulse();
            }
            catch
            {
                if (record.Generation == generation && !record.Closing) CloseRecord(record, true);
                throw;
            }
        }
        private async UniTask<UIPanelHandle> ReopenAfterCloseAsync(UIPanelRecord record, string key, object data, UIPanelHandle parent, CancellationToken token)
        {
            await record.CloseCompletion.Task.AttachExternalCancellation(token);
            RequireAlive();
            if (parent.Record != null) RequireHandle(parent);
            return await Enqueue(key, data, parent, token);
        }
        public bool TryGet(string key, out UIPanelHandle panel)
        {
            panel = default;
            if (!IsAlive || !m_records.TryGetValue(key, out var record) || !record.Open || !record.View) return false;
            panel = record.Handle; return true;
        }
        private UIPanelRecord RequireHandle(UIPanelHandle panel)
        {
            RequireAlive();
            if (!panel.IsOpen || panel.Record.Context != this || panel.Record.Closing)
                throw new InvalidOperationException("UI 句柄不属于此所有者或已失效。");
            return panel.Record;
        }
        public void Refresh(UIPanelHandle panel, object data) { RequireHandle(panel).Logic.OnRefresh(data); }
        public void BringToFront(UIPanelHandle panel)
        {
            var record = RequireHandle(panel);
            record.Sequence = Module.NextSequence();
            record.Host.transform.SetAsLastSibling();
            foreach (var child in new List<UIPanelRecord>(record.Children))
                if (child.Open && !child.Closing) BringToFront(child.Handle);
            Module.Pulse();
        }
        public void Close(string key) => CloseAsync(key).Forget();
        public void Close(UIPanelHandle panel) => CloseAsync(panel).Forget();
        public UniTask CloseAsync(string key, CancellationToken cancellationToken = default)
        {
            if (m_operations.TryGetValue(key, out var operation))
            {
                m_operations.Remove(key);
                // 先进入关闭状态，再取消打开，避免入场取消路径将保留策略误判为销毁。
                var closing = m_records.TryGetValue(key, out var existing) ? CloseRecordAsync(existing) : UniTask.CompletedTask;
                operation.Cancellation.Cancel();
                return closing.AttachExternalCancellation(cancellationToken);
            }
            return (m_records.TryGetValue(key, out var record) ? CloseRecordAsync(record) : UniTask.CompletedTask)
                .AttachExternalCancellation(cancellationToken);
        }
        public UniTask CloseAsync(UIPanelHandle panel, CancellationToken cancellationToken = default)
        {
            if (panel.Record == null) return UniTask.CompletedTask;
            if (panel.Record.Context != this) throw new InvalidOperationException("UI 句柄属于另一所有者。");
            if (panel.Generation != panel.Record.Generation) return UniTask.CompletedTask;
            if (panel.Record.Closing) return panel.Record.CloseCompletion.Task.AttachExternalCancellation(cancellationToken);
            return panel.IsOpen ? CloseAsync(panel.Key, cancellationToken) : UniTask.CompletedTask;
        }
        private UniTask CloseRecordAsync(UIPanelRecord record)
        {
            if (record.Disposed || (!record.Open && !record.Closing)) return record.Retirement;
            if (record.Closing) return record.CloseCompletion.Task;
            BeginClose(record);
            RunCloseAsync(record, record.CloseVersion).Forget();
            return record.CloseCompletion.Task;
        }
        private void BeginClose(UIPanelRecord record)
        {
            record.CloseCompletion = new UniTaskCompletionSource();
            record.CloseFinished = false; record.Closing = true;
            record.CloseVersion++;
            bool wasOpen = record.Open; record.Open = false; record.Entering = false;
            record.AnimationCancellation?.Cancel(); record.AnimationCancellation?.Dispose();
            record.AnimationCancellation = new CancellationTokenSource();
            if (record.Focused) { record.Focused = false; InvokeCleanup(record.Logic.OnBlur); }
            var events = EventSystem.current;
            if (events && events.currentSelectedGameObject && record.Host &&
                events.currentSelectedGameObject.transform.IsChildOf(record.Host.transform))
            {
                record.LastSelection = events.currentSelectedGameObject;
                events.SetSelectedGameObject(null);
            }
            if (record.Gate) record.Gate.interactable = false;
            if (wasOpen) InvokeCleanup(record.Logic.OnClose);
            Module.Pulse();
        }
        private async UniTask RunCloseAsync(UIPanelRecord record, int version)
        {
            try
            {
                var children = new List<UniTask>();
                foreach (var child in new List<UIPanelRecord>(record.Children)) children.Add(CloseRecordAsync(child));
                await UIPanelPresentation.PlayAsync(record, record.Definition.ExitAnimation, false, record.AnimationCancellation.Token);
                await UniTask.WhenAll(children);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { m_cleanupErrors.Add(ex); Debug.LogException(ex); }
            finally { if (record.CloseVersion == version) FinishClose(record); }
        }
        private void CloseRecord(UIPanelRecord record, bool destroy = false)
        {
            if (!destroy) { CloseRecordAsync(record).Forget(); return; }
            if (record.Disposed) return;
            record.ForceClose = true;
            if (!record.Closing) BeginClose(record);
            foreach (var child in new List<UIPanelRecord>(record.Children)) CloseRecord(child, true);
            record.AnimationCancellation?.Cancel();
            FinishClose(record);
        }
        private void FinishClose(UIPanelRecord record)
        {
            if (record.CloseFinished || !record.Closing) return;
            record.CloseFinished = true;
            var parent = record.Parent;
            parent?.Children.Remove(record); record.Parent = null; record.Children.Clear();
            if (record.Host) record.Host.SetActive(false);
            record.AnimationCancellation?.Dispose(); record.AnimationCancellation = null;
            if (record.ForceClose || record.Definition.ClosePolicy == UIClosePolicy.DestroyOnClose) DestroyRecord(record);
            if (parent != null && parent.Open && !parent.Closing) parent.Sequence = Module.NextSequence();
            CompleteCloseAsync(record).Forget();
        }
        private async UniTask CompleteCloseAsync(UIPanelRecord record)
        {
            await record.Retirement;
            record.Closing = false;
            if (record.Disposed && m_records.TryGetValue(record.Definition.Key, out var current) && current == record)
                m_records.Remove(record.Definition.Key);
            Module.Pulse();
            record.CloseCompletion.TrySetResult();
        }
        private void DestroyRecord(UIPanelRecord record)
        {
            if (record.Disposed) return;
            record.Disposed = true; record.Open = false;
            if (!record.Closing && m_records.TryGetValue(record.Definition.Key, out var current) && current == record)
                m_records.Remove(record.Definition.Key);
            InvokeCleanup(record.Logic.OnDispose);
            record.Logic.Unbind();
            var retired = new UniTaskCompletionSource();
            record.Retirement = retired.Task;
            RetireAndCompleteAsync(record.Host, record.Lease, retired).Forget();
            record.Host = null; record.View = null; record.Lease = null;
        }
        private void Retire(GameObject host, UIPrefabLease lease) { RetireAsync(host, lease).Forget(); }
        private async UniTask RetireAndCompleteAsync(GameObject host, UIPrefabLease lease, UniTaskCompletionSource completion)
        {
            try { await RetireAsync(host, lease); }
            finally { completion.TrySetResult(); }
        }
        private async UniTask RetireAsync(GameObject host, UIPrefabLease lease)
        {
            m_pendingReleases++;
            try
            {
                if (host) { host.SetActive(false); GlobalUIModule.DestroyOwned(host); }
                if (Application.isPlaying) await UniTask.NextFrame();
                lease?.Dispose();
            }
            catch (Exception ex) { m_cleanupErrors.Add(ex); Debug.LogException(ex); }
            finally { m_pendingReleases--; }
        }
        internal void InvokeCleanup(Action callback)
        {
            try { callback(); }
            catch (Exception ex) { m_cleanupErrors.Add(ex); Debug.LogException(ex); }
        }
        internal void Update()
        {
            if (!IsAlive) return;
            foreach (var domain in m_domains.Values) domain.UpdateBinding();
            foreach (var record in new List<UIPanelRecord>(m_records.Values))
                if (!record.View && !record.Disposed) CloseRecord(record, true);
        }
        internal void CollectOpen(List<UIPanelRecord> result)
        {
            if (!IsAlive) return;
            foreach (var record in m_records.Values) if ((record.Open || (record.Closing && !record.CloseFinished)) && !record.Disposed) result.Add(record);
        }
        internal async UniTask DisposeAsync()
        {
            if (!IsAlive) return;
            IsAlive = false;
            foreach (var operation in new List<OpenOperation>(m_operations.Values)) operation.Cancellation.Cancel();
            m_operations.Clear();
            foreach (var record in new List<UIPanelRecord>(m_records.Values)) CloseRecord(record, true);
            while (m_pendingLoads > 0 || m_pendingReleases > 0) await UniTask.Yield();
            UILayoutScheduler.ForgetSubtree(Root.transform);
            GlobalUIModule.DestroyOwned(Root);
            m_domains.Clear(); m_definitions.Clear();
            if (m_cleanupErrors.Count > 0) throw new AggregateException("UI 所有者已清理，但部分回调/释放失败。", m_cleanupErrors);
        }
        public static void ValidateView(UIView view)
        {
            if (!view) throw new ArgumentNullException(nameof(view));
            foreach (var group in view.GetComponentsInChildren<CanvasGroup>(true))
                if (group.ignoreParentGroups) throw new InvalidOperationException("UIView 内禁止 ignoreParentGroups 绕过输入门禁。");
            foreach (var canvas in view.GetComponentsInChildren<Canvas>(true))
                if (canvas.overrideSorting) throw new InvalidOperationException("UIView 内禁止 overrideSorting 绕过域层顺序。");
        }
    }
}
