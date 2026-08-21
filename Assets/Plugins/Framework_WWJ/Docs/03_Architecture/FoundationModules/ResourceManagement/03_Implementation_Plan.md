# Resource Management 实施计划

> 状态：用户批准的双后端计划，已按本文件完成实现与验收。

## 实施切片

1. 建立 `BaseModules/ResourceManagement` 垂直目录与独立 asmdef，保留用户的 `BaseModules.meta`。
2. 实现 Key、Lease、异常、Provider 最小契约和无第三方依赖的 Resource Runtime。
3. 实现 ResourceStore 的显式路由、single-flight、独立取消、即时缓存移除和 Shutdown。
4. 接入 `ResourceModule + ResourceHandler`、Global 生命周期、初始化回滚与只读诊断。
5. 分别实现 Unity Resources 和 Addressables Integration，并锁定 Addressables 1.22.3。
6. 增加配置/构建验证、Framework Center 页面和 Sample 页面。
7. 创建双 Provider Module SO、两个 Prefab、空 SceneConfig、示例场景、Addressables 本地 Group/Entry，并以追加方式接入中央设置和 Build Settings。
8. 完成模块 EditMode/PlayMode、全框架回归、CoreSkeleton 重建保留、内容构建、Player 构建与 Standalone 冒烟。
9. 回写需求、参考、契约、ADR、验收、项目状态与索引；不进入 Pooling/Event。

## 关键资产

- Module：`Samples/Configs/ResourceModule.asset`
- Resources Prefab：`Samples/Runtime/Resources/Framework_WWJ/ResourceManagement/ResourcesSamplePrefab.prefab`
- Addressables Prefab：`Samples/Prefabs/AddressablesSamplePrefab.prefab`
- Address：`framework-wwj/samples/resource-management/addressables-prefab`
- Sample Scene：`Samples/Scenes/ResourceManagementSample.unity`

## 门禁

- Runtime 不反向引用具体 Integration 或 Addressables。
- SceneConfig 不安装 ResourceModule；当前 Global 模板同时配置两个 Provider。
- CoreSkeleton 重建保留非 CoreSkeleton Global 模块与非 A/B 场景绑定。
- 不改写 A/B 场景内容、不恢复旧代码、不修改 LyingBottle/YokiFrame。
- 全部门禁关闭后才允许讨论 Pooling。
