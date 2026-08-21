# Framework_WWJ 模块交付契约与模板

> 状态：流水线交付规范。垂直胶囊已由 Resource 阶段 ADR-RM-001 验证，后续模块仍需按自身依赖确认程序集。

## 1. 模块完整交付物

| 层次 | 必需内容 |
| --- | --- |
| 需求 | 模块目标、调用方、范围、非目标、验收标准 |
| 架构 | 逻辑图、数据流、生命周期、所有权、失败与依赖；生产程序集/类型在 Framework Center 分层架构目录中可见 |
| Runtime | Module SO、可选 Handler/Provider、公开契约、内部实现 |
| Editor | 必要 Inspector、配置诊断、Framework Center 接入；没有真实需求时可为空 |
| Assets | 模块模板 SO、配置、示例资产与 `.meta` |
| Tests | 纯算法/EditMode、生命周期/PlayMode、回归验证 |
| Samples | 最小调用示例和人工验收路径 |
| Docs | README、ADR、实施计划、验收复盘和索引 |
| Distribution | 模块身份、程序集、直接依赖、资产边界和可选集成说明 |

## 2. 代码目录候选

为了未来可选引入和分发，优先讨论“垂直模块胶囊”：

```text
Assets/Plugins/Framework_WWJ/
├─ Runtime/                         # 已有核心骨架
├─ Editor/                          # 已有统一编辑器基础设施
├─ Modules/
│  └─ <ModuleName>/
│     ├─ Runtime/
│     ├─ Editor/
│     ├─ Tests/EditMode/
│     ├─ Tests/PlayMode/
│     └─ Samples/
└─ Docs/03_Architecture/FoundationModules/<ModuleName>/
```

**候选理由：** 单模块目录可清楚识别源码、程序集、测试和示例边界，未来分发工具也更容易构建模块清单。

**已验证实例：** Resource Management 使用 `BaseModules/ResourceManagement` 垂直胶囊，内部自带 Runtime、Integrations、Editor、Tests、Samples 与配置资产；详见 [ADR-RM-001](./ResourceManagement/ADR/ADR-RM-001_Vertical_Capsule_And_Integration_Boundaries.md)。后续模块可复用边界原则，但不能机械复制程序集数量。

## 3. 程序集依赖规则

- Core Runtime 不反向引用任何基础模块。
- 模块 Runtime 只引用 Core Runtime 和已批准的直接依赖模块。
- 模块 Editor 只引用本模块 Runtime、Framework Editor 基础设施和必要的 UnityEditor/Odin Editor 程序集。
- Tests 与 Samples 单向依赖 Runtime；生产 Runtime 不引用 Tests/Samples。
- 可选后端进入 Adapter/Integration 程序集，不把 Addressables、YooAsset 等依赖写进模块最小核心。
- EventCenter 若需要池化，只依赖批准的最小 Pooling 契约，避免引入 GameObject/资源后端。
- 每个生产程序集通过 `FrameworkArchitectureAssemblyAttribute` 声明稳定分组与职责；Tests、Samples、第三方程序集不接入生产架构目录。
- 已接入程序集的顶层类、接口、结构体和枚举必须维护 `FrameworkArchitectureAttribute`，目录诊断必须为零。

## 4. Module SO 与运行状态规则

- SO 资产是模板；运行时由 Scope 克隆，原资产不保存订阅、句柄、缓存、池实例或加载状态。
- 模块必须声明 Global/Scene 适用范围和依赖类型。
- Handler 只在确有策略替换、后端多态或复杂逻辑隔离价值时使用。
- 公开返回借用对象、订阅令牌或资源句柄时，API 必须说明所有者、释放方法和 Scope 卸载后的行为。
- Scope 卸载必须回收模块仍持有的内部资源；不能依赖调用方在完美时机全部手动清理。

## 5. 模块需求输入模板

```markdown
# <模块名>需求简报

## 目标与使用场景
- 要解决的问题：
- 首批调用方：
- 玩家/开发者可观察结果：

## 范围
- 必须实现：
- 明确不实现：
- 可延后：

## 生命周期与所有权
- Global / Scene：
- 创建者 / 持有者 / 销毁者：
- 获得和释放方式：
- Shutdown 行为：

## API 与数据流
- 期望调用示例：
- 同步 / 异步：
- 取消 / 失败 / 重试：

## 依赖与适配
- 必需模块：
- 可选后端：
- 第三方包：

## 验收
- EditMode：
- PlayMode：
- 示例：
- 性能 / 内存：
```

## 6. 实施计划模板

```markdown
# <模块名>实施计划

## 目标、范围与决定
## 逻辑层次图
## 生命周期 / 数据时序图
## 目录与程序集树
## 逐脚本设计
## SO、中央配置与示例接线
## 错误、取消、回滚与清理
## Editor / Framework Center
## EditMode / PlayMode / 人工验收
## 实施门禁
## 文档与迁移
```

逐脚本设计必须细化到字段、属性、公开/受保护/内部方法、关键算法、协作类型和保存目录，不能只列类名。

## 7. 验收复盘模板

```markdown
# <模块名>验收与复盘

## 实际交付
## 自动测试结果
## 人工验收结果
## 原资产与运行克隆检查
## 性能 / GC / 泄漏观察
## 与计划的偏差
## 已知限制
## 后续候选（不自动进入实现）
```

## 8. 完成检查

- [ ] 用户批准的公共契约已实现，没有静默扩张。
- [ ] 模板资产在运行前后序列化内容不变。
- [ ] 依赖缺失和配置错误在运行时克隆前给出中文诊断。
- [ ] Load 失败回滚、Unload 和 Shutdown 完成资源清理。
- [ ] EditMode、PlayMode、全框架回归与示例验收通过。
- [ ] Runtime、Editor、Tests、Samples 依赖方向正确。
- [ ] 新 Unity 资产都有稳定 `.meta`。
- [ ] 生产程序集分组、全部生产顶层类型职责、关键关系和源码定位均进入分层代码架构图。
- [ ] Docs、ADR、架构图元数据和 Skill 路由已回写。
