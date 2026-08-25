# Resource Management 架构与公共契约

## 数据流

```text
业务 / 后续 Pooling
  -> ResourceKey（明确 Backend + Location）
  -> ResourceModule
  -> ResourceHandler
  -> ResourceStore（路由、缓存、single-flight、Lease、Shutdown）
  -> UnityResourcesProvider | AddressablesResourceProvider
```

## 公共 API

```csharp
var key = ResourceKey.FromAddressables(
    "framework-wwj/samples/resource-management/addressables-prefab");
using var lease = await Framework.GetModule<ResourceModule>()
    .AcquireAsync<GameObject>(key, cancellationToken);
var instance = Object.Instantiate(lease.Value);
```

- `ResourceBackendKind` 只有 `UnityResources` 与 `Addressables`。
- `ResourceKey` 的相等性使用 Backend 与 Ordinal Location；Resources 路径相对于任意 `Resources` 目录，不带扩展名和前缀。
- `ResourceLease<T>` 是引用类型、`Dispose` 幂等；释放或 Shutdown 后访问 `Value` 抛 `ObjectDisposedException`。
- `ResourceLoadException` 保存 Key、请求类型、Provider 名称和原始异常。
- `ResourceProviderBase` 只向 Resource Runtime 开放内部包装方法；派生类实现受保护的初始化、加载和关闭方法。

## 缓存与取消

缓存键为 `(ResourceKey, exact Type)`。首次请求创建 Pending；相同键和类型的后续请求加入等待。每个等待者的 CancellationToken 只取消自己的等待；全部等待者取消时 Pending 被废弃并取消 Provider 等待。后端迟到结果会立即释放，不进入缓存。失败不缓存。

加载成功后，每个等待者创建独立 LeaseState。最后一份 Lease 归还时立即移除缓存并释放后端 Handle；因此本模块没有空闲轮询、TTL、LRU 或稳态 GC。

## 初始化与关闭

`ResourceHandler` 先校验空项、重复后端和双 Provider 完整性，再按 Resources、Addressables 初始化。失败时逆序关闭已完成 Provider，并把异常交给 GlobalScope 回滚。

Shutdown 先拒绝 Acquire，再废弃并等待 Pending，随后按 Key/后端/类型记录泄漏 Lease，令现存 Lease 失效，释放全部后端句柄，最后逆序关闭 Provider。SO、Handler 和 Provider 模板只保存配置，运行状态都在克隆内。

## 程序集边界

- Resource Runtime → Framework Runtime、UniTask。
- UnityResources Integration → Resource Runtime。
- Addressables Integration → Resource Runtime、Addressables、ResourceManager、UniTask。
- Editor → Framework Editor、Resource Runtime、两个 Integration。
- Tests/Samples 单向引用生产程序集；Core 与未来 Pooling 不引用具体 Integration。
