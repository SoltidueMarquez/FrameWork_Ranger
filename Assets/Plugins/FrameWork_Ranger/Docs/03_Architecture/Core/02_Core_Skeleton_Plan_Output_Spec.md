# Framework_WWJ 骨架计划交付规范

> 本规范约束下一步“设计计划”的内容。它不是实现方案本身。

## 1. 计划必须决策完整

计划交付后，实现者不应再决定公共 API、脚本职责、所有权、生命周期顺序、目录位置或错误行为。仍需用户选择的内容必须在输出计划前问清楚。

## 2. 必须包含的逻辑结构图

使用 Mermaid 绘制至少一张代码逻辑层次图，明确：

- Unity 启动/场景入口；
- Global 与 Scene 配置；
- Framework Runtime、可选 Framework Handler；
- Module SO 模板、运行时克隆体和 Module Handler；
- 模块注册表、排序、生命周期和 Tick 驱动；
- 创建、持有、调用和销毁方向；
- Global/Scene 作用域边界。

复杂时补充启动、场景切换和卸载时序图。图中节点名称必须与脚本清单一致。

## 3. 必须包含的目录树

目录树必须给出 `Assets/Plugins/Framework_WWJ` 下的实际目标路径，并至少区分：

- Runtime 核心契约；
- Runtime 配置；
- Runtime 驱动与作用域；
- 模块/Handler 基类；
- Editor 工具；
- Tests/EditMode；
- Tests/PlayMode；
- Samples 或最小验证场景；
- Docs 与 ADR。

每个 asmdef 的位置和依赖方向也要列出。

## 4. 逐脚本规格

对计划新增或修改的每个脚本，按以下模板描述：

```markdown
### ScriptName.cs

- 路径：
- 层次/职责：
- 类型：class / abstract class / interface / enum / ScriptableObject / MonoBehaviour
- 继承与接口：
- 创建者、持有者、销毁者：

#### 序列化字段
- `fieldName : Type`：用途、默认值、Inspector/Odin 展示方式。

#### 运行时字段与属性
- `propertyName : Type`：读写权限、状态来源、不变量。

#### 公开方法
- `Method(args) -> result`：调用者、前置条件、完整行为、失败方式。

#### 内部方法
- `Method(args)`：算法、调用顺序、与其他脚本的交互。

#### 生命周期
- 在何时被调用，成功/失败后进入什么状态，如何清理。

#### 注释与 region
- 需要的中文 XML 注释和 `#region` 分块。

#### 验证
- 对应 EditMode / PlayMode / 示例验收。
```

不允许只列文件名和一句职责；属性、方法和实现方式必须达到可以直接编码的精度。

## 5. 必须描述的数据与时序

- 配置资产如何被 Entry/Runtime 获取；
- SO 模板如何克隆、注册和销毁；
- Module 与 Handler 如何绑定；
- 排序和依赖如何计算；
- Global 模块何时创建，跨场景如何保留；
- Scene 模块何时装载和卸载；
- Update/FixedUpdate/LateUpdate 如何只分发给需要的模块；
- 初始化失败、场景切换中断和应用退出如何清理。

## 6. 必须包含的测试计划

至少覆盖：

- 原始 SO 资产在 Play Mode 后没有运行状态变化；
- 克隆体只创建一次并在作用域结束销毁；
- 加载与卸载排序正确且稳定；
- Global 模块跨场景保持，Scene 模块按作用域销毁；
- 重复模块、缺失引用和初始化失败符合设计；
- Module/Handler 绑定与多态选择正确；
- Domain Reload 开启和关闭时均无静态残留；
- 最小示例能够从 Entry 完整跑通加载、Tick 和卸载。

## 7. 代码表达要求

计划中的伪代码和未来实现都必须遵循[代码规范](../../04_Standards/Code_Style_And_Comments.md)：清晰命名、详细中文注释、适度 `#region`、职责单一，以及只在真实边界提供必要保护。
