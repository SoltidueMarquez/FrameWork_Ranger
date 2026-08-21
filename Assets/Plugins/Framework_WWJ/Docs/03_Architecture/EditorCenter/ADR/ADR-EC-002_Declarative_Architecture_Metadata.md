# ADR-EC-002：声明式代码架构元数据

- 状态：已接受
- 日期：2026-08-07
- 所属阶段：Phase 1.1——中央启动、统一编辑器中心与架构类图

> Phase 1.4 通过 [ADR-EC-005](./ADR-EC-005_OptIn_Production_Assemblies_And_Hierarchical_Navigation.md)
> 扩展了本决定：目标范围改为显式接入的生产程序集，类型覆盖增加结构体与枚举，显示方式改为分组目录 + 叶级类型图。

## 背景与约束

Framework_WWJ 需要在 Unity 内展示可理解、可点击并能定位源码的类图。仅靠反射可以推导继承和接口实现，却无法可靠表达中文名称、职责、逻辑层级及少量关键协作关系；手工维护独立清单又容易与代码漂移。

## 候选方案

1. 完全通过反射生成所有类型和调用关系。
2. 在每个目标类型上维护轻量 Attribute，反射补充结构关系。
3. 在 JSON 或 ScriptableObject 中维护独立架构目录。

## 决定

采用方案 2。Runtime 与 Editor 顶层类及关键接口使用 `FrameworkArchitectureAttribute` 声明显示名称、中文职责、固定层级、排序和显式关键协作类型。直接继承和直接接口实现由目录构建器自动生成；不会扫描方法体或生成传递关系。

Attribute 使用 `Conditional("UNITY_EDITOR")`，使玩家构建不保留职责字符串和协作元数据。Editor 使用 `TypeCache` 建立不可变目录，通过 `MonoScript.GetClass()` 建立 Type 到源码索引；图使用 IMGUI + Handles 固定分层布局。

## 影响与明确非目标

- 新增 Runtime/Editor 顶层目标类型时必须同步维护 Attribute，EditMode 测试负责防漂移。
- 代码中的 Attribute 是架构图元数据事实源，详细设计与决策仍以 Docs 为事实源。
- 图只表达继承、接口实现与少量显式协作，不代表完整运行时调用图。
- Samples、Tests、第三方代码、委托和私有嵌套辅助类型不进入图；生产结构体与枚举已由 Phase 1.4 纳入。
- 本阶段不使用 GraphView，也不支持节点自由拖动、缩放或自动写回代码。

## 验证方式

- EditMode 验证 Attribute 发现、分层排序、三类关系和目标类型覆盖。
- EditMode 验证 Type 到 `MonoScript` 的源码定位。
- 人工验证选择、搜索高亮、Ping 和由 Rider 打开源码。
