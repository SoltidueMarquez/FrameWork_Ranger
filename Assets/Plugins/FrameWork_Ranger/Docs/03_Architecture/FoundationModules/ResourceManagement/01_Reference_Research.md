# Resource Management 参考研究

> 参考项目只读；本文区分参考事实与 Framework_WWJ 决策，不复制参考 API。

## HTY / LyingBottle

事实：HTY 的资源域较宽，`IResourceHandler` 同时组合 LoaderConfig、AssetLoader、Unloader 与 BundleManager；资源 README、`ResourceManager`、`DefaultResourceHelper` 还覆盖 Resources、AssetBundle、Addressables 与业务资源描述。真实项目通过全局模块和 SO 配置接入，但不存在一个可直接轻量复制的单一 Handler。源码路由见 `Docs/02_References/HTY/08_HTY_Reference_Source_Map.md` 的“资源”章节。

采纳：资源能力属于跨场景生命周期，初始化失败必须进入框架已有回滚；Provider 实现与业务门面分离。

舍弃：不采用宽接口、静态资源总入口、AssetBundle/场景/下载聚合，也不复制 HTY API。

## YokiFrame

事实：ResKit 使用 `IResourceProvider` 隔离门面与后端；每次获取返回独立 `ResHandle<T>`，底层同一资源共享缓存与引用计数；Unity Resources 和 YooAsset 位于不同适配层。来源索引：`Core/Runtime/ResKit/Contracts/IResourceProvider.cs`、`Handles/ResHandle.cs`、`Facade/ResKit.Provider.cs` 与 `Core/Adapters/Unity/Runtime/ResKit/Resources/UnityResourceProvider.cs`。

采纳：显式所有权 Handle/Lease、Provider 边界、后端集成程序集和缓存引用计数。

调整：本项目的 Key 必须携带后端，不允许 Provider 顺序产生隐式回退；Provider 不允许业务直接取得；后端替换与代次管理不进入首版。

## 形成的本项目决策

- 采用 `ResourceModule → ResourceHandler → ResourceStore → Provider` 的窄数据流。
- Core Runtime 不引用 Resource；Resource Runtime 不引用 Addressables。
- Addressables 和 Unity Resources 是当前模板的两个必需集成，但其第三方/引擎句柄不越过 Provider 边界。
- 所有权采用独立 Lease；缓存只服务当前活动 Lease，不做跨请求驻留优化。
