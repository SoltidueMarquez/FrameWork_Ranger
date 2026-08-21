# ADR-EC-009：HTY 式主从配置工作台

> 状态：Accepted<br>
> 日期：2026-08-22<br>
> 阶段：Editor Center Phase 1.8

## 背景与约束

Phase 1.7 已让项目设置引用和 Module 引用能够按眼睛按钮展开真实 Inspector，但项目设置仍把中央引用、场景覆盖、诊断、依赖图和多个内联 Inspector 纵向堆叠。模块数量增加后，用户很难保持当前作用域和错误上下文。

HTY 新版“全局模块”页面验证了两层降噪方式：先用左侧导航区分当前配置，再用右侧页签和 32px 紧凑行组织编辑；同一类列表只展开一个详情。参考实现同时包含硬编码暗色、CustomPropertyTree 和 Library JSON 状态，这些不适合作为 Framework_WWJ 的基础设施。

## 考虑方案

1. 只调整颜色和边框：变更最小，但纵向层次和多项展开问题仍存在。
2. 仅重绘 Module 列表：能降低单个 Config 的密度，但中央设置与场景组合仍分散。
3. 整页主从工作台：左侧选择作用域，右侧编辑或查看组合依赖图，并让 Global/Scene Config 共用紧凑模块视图。

## 决策

采用整页主从工作台：

- 项目配置页自管滚动，左侧固定作用域导航，右侧使用“配置编辑 / 组合依赖图”页签。
- 新会话默认选择有效 Global Config；缺失时回到项目入口。
- 配置引用眼睛改为右侧目标导航；不再在项目页纵向嵌套多个完整 Config Inspector。
- ModuleConfig 的模块列表使用紧凑行、搜索、状态筛选、拖动排序和行级诊断。
- 同一份 Config 只允许一个 Module 真实 Inspector 展开；状态仍由 `SessionState` 持有，子 Editor 继续缓存并明确释放。
- 搜索或筛选生效时禁止重排；结构修改统一经过 Undo、`SetModules` 和 Dirty 标记，Resolver 保持唯一校验来源。
- 样式使用 Framework Center 深浅主题色板，不复制 HTY 的硬编码暗色或 PropertyTree 管理方式。

## 后果与非目标

- `FrameworkCenterPage` 增加默认开启的 `UseHostContentScroll` Editor 扩展属性；现有页面无需修改。
- ADR-EC-008 关于 SessionState、真实 Editor 缓存、异常隔离和释放生命周期的决策继续有效；其中“多个配置或模块可以同时原位展开”的展示策略由本 ADR 取代。
- Runtime 公共 API、模块生命周期、配置解析和序列化格式不变。
- 不增加 HTY 的静态/动态配置、模块包、预览模块或 Odin 单例配置页签。

## 验证

- EditMode 覆盖工作台状态修复、页面滚动契约、模块增删改排、筛选映射、Undo、Odin 保存往返和互斥 Editor 释放。
- Unity 6000.5.9f1 完成隔离 Import、完整 EditMode/PlayMode 回归和架构目录零诊断检查。
- 真实窗口需人工检查深浅主题、最小 Center 宽度、窄 Inspector、长名称、搜索筛选、场景覆盖和真实模块 Inspector。
