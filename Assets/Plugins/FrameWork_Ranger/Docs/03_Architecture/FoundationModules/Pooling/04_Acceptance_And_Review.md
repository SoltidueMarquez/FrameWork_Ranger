# Pooling 验收与复盘

> 日期：2026-08-31  
> 当前结论：真实 Unity Import、首轮 Pooling 聚焦测试和 Sample 资产生成已通过；完成代码审计后的最新改动与全回归、Addressables、Player、双 Smoke 等待当前 GUI Editor 关闭后复验。

## 已完成检查

- 三个生产 Runtime asmdef、Editor、两套 Tests 与两套 Samples asmdef 已建立。
- Framework/Resource 既有程序集只追加受控 `InternalsVisibleTo`，没有修改生命周期公共契约。
- Unity 6000.5.9f1 真实 Import 与新 asmdef 编译通过；日志位于 `Logs/UnityCli/20260831-150705-import-8116/import.log`。
- Sample Builder 首次运行暴露场景切换卸载未保存 SO 引用的问题；修正为先提交模块资产、再重载 SceneConfig 后，第二次运行退出码为 0，日志位于 `Logs/UnityCli/20260831-151220-buildpoolingsample-34120/pooling-sample.log`。
- Builder 已追加 Global ReferencePoolModule、Pooling SceneConfig、双后端 Prefab、独立场景、Addressables Group、场景绑定和 Build Settings，没有移除 Resource Sample。
- Builder 完成后的 Pooling 聚焦 EditMode 为 21/21、PlayMode 为 5/5；随后审计又加入 Reference 热路径零分配与 Pool 加载失败回滚用例，最新总数必须在最终门禁中重新记录。
- LyingBottle 与 YokiFrame 保持只读。

## 待执行门禁

| 门禁 | 状态 | 说明 |
| --- | --- | --- |
| Unity Import / 新 asmdef 编译 | 已通过，最新改动待复验 | 首轮真实 Import 退出码 0；GUI 已重新打开工程 |
| Pooling EditMode / PlayMode | 首轮通过，最新改动待复验 | 生成资产后 21/21 与 5/5；新增两项验收用例后待重跑 |
| Sample 资产生成 | 已通过，最新 Builder 待重跑 | 追加式 Builder 退出码 0；新增 Camera/Light 后待刷新场景 |
| 全框架回归 | 待复验 | 首轮发现并修正 Pooling 架构路径与既有测试的枚举顺序耦合 |
| Addressables / Player / 双 Smoke | 待执行 | 需要当前 GUI Editor 关闭后执行 |

## 复盘关注点

- `TrackedPool<T>` 把失败恢复集中在一处，Reference 与 GameObject 不再分别复制所有权算法。
- GameObject 的 Spawn 前姿态设置通过 `beforeRentCallback` 位于激活和池回调之前；回调失败后当前实例会退出借出集合并销毁。
- 被 Unity 外部销毁的实例使用引用相等集合仍可定位，并在下一次借还、Tick 或诊断时剔除。
- 审计发现通用池原先会在 Reference 每次 Rent 时执行 Unity Object 风格失效扫描；现已只对显式提供有效性探针的 GameObject 池启用，并复用扫描缓冲，Reference 热路径不再因此分配。
- 架构目录要求稳定路径、中文路径逐段对应，且共享父组排序一致；Pooling 元数据已按该规则修正。
- 当前 GUI 占用不是代码失败；验收保持“部分通过”，直到最新结果 XML、构建退出码和 Smoke PASS 日志真实产生。
