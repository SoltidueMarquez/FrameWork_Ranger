# Phase 1.4 分层代码架构导航器验收与复盘

> 日期：2026-08-20  
> 结论：已通过。Resource Management 与 Framework_WWJ 核心生产代码已接入分层架构目录，完整 EditMode/PlayMode 回归通过。

> 后续说明：Phase 1.4 的生产程序集接入和类型元数据继续有效；钻入叶组的导航方式已在 [Phase 1.5](./11_Phase1_5_Expandable_Compound_Architecture_Graph_Acceptance_And_Review.md) 改为单画布可展开容器。

## 1. 问题根因

旧架构目录只扫描 `Framework_WWJ.Runtime` 与 `Framework_WWJ.Editor` 两个硬编码程序集，源码索引也只搜索核心 `Runtime/`、`Editor/` 目录。Resource Management 使用独立 Runtime、Editor 和两个 Integration 程序集，因此已有的类型 Attribute 无法让它进入目录。

本阶段将发现边界改为程序集显式接入：生产程序集用 `FrameworkArchitectureAssemblyAttribute` 声明稳定分组路径、中文显示路径、职责和顺序；目录构建器不再维护模块程序集白名单。

## 2. 实际交付

- 架构元数据支持顶层 Class、Interface、Struct 与 Enum。
- 核心 Runtime、核心 Editor、Resource Runtime、Resource Editor、Unity Resources 和 Addressables 共 6 个生产程序集显式接入。
- 构建出 11 个分组与 104 个正式类型节点，其中 Resource Management 为 25 个类型节点。
- 根目录按“框架核心 / 基础模块”展示；资源模块继续展开为 Runtime、适配、Editor，适配下再区分 Unity Resources 与 Addressables。
- 非叶分组使用类似 Animator 子状态机的大块节点；单击查看职责，双击或点击详情按钮进入内部，顶部面包屑可返回任意父级。
- 分组之间的类型依赖聚合显示为带数量的关系边；进入叶分组后显示原有的继承、接口实现和显式协作关系。
- 叶级类型图继续支持 35%–200% 缩放、中键或 `Alt + 左键`平移、适配视图、100% 重置、搜索高亮、节点详情、Ping 与 Rider 打开。
- 源码索引扩展到整个 `Assets/Plugins/Framework_WWJ`，并能定位与其他类型共用脚本文件的结构体和枚举。
- Resource Management 的契约、Module/Handler、缓存状态、Provider、两种后端 Handle、诊断与 Editor 工具均补充了中文职责和关键协作关系。
- AI 模块流水线与交付契约增加“程序集接入 + 所有生产顶层类型元数据 + 架构目录验收”门禁。

## 3. 目录快照

```text
Framework_WWJ
├─ 框架核心
│  ├─ Runtime
│  └─ Editor
└─ 基础模块
   └─ 资源管理
      ├─ Runtime
      ├─ 适配
      │  ├─ Unity Resources
      │  └─ Addressables
      └─ Editor
```

目录探针结果：

| 项目 | 结果 |
| --- | ---: |
| 架构目录诊断 | 0 |
| 分组 | 11 |
| 正式类型节点 | 104 |
| 核心类型节点 | 79 |
| Resource Management 类型节点 | 25 |
| Resource 源码定位失败 | 0 |

Tests、Samples、第三方程序集没有声明生产程序集 Attribute，因此不会混入正式架构目录。

## 4. 自动验证

为避免干扰用户当前打开的 Unity 编辑器，本次将 Assets、Packages 与 ProjectSettings 复制到一次性隔离工程，再使用同一 Unity 2022.3.62f3 执行官方 Test Runner。

| 门禁 | 结果 | 说明 |
| --- | --- | --- |
| Core + Resource EditMode | 57/57 Passed | 2.639 s；包含分组树、元数据覆盖、资源类型接入与源码映射测试 |
| Core + Resource PlayMode | 18/18 Passed | 0.658 s；Runtime 生命周期与资源双后端既有行为无回退 |
| Rider Core EditMode 工程编译 | Passed | 0 error |
| Rider Resource EditMode 工程编译 | Passed | 0 error |
| 目录运行探针 | Passed | 0 诊断，104 个正式节点，25 个资源节点 |

Rider 构建中仍会显示 Unity 自动生成项目引用的版本冲突 Warning；它不是本次源码诊断，也未产生编译错误。

## 5. 人工验收步骤

1. 打开 `Framework_WWJ → Framework Center → 代码架构`。
2. 根视图确认出现“框架核心”和“基础模块”两个大块节点。
3. 双击“基础模块”，再双击“资源管理”。
4. 确认资源管理内部显示 `Runtime`、`适配`、`Editor`；打开“适配”后显示 `Unity Resources` 与 `Addressables`。
5. 进入 Resource Runtime，选择 `ResourceModule`、`ResourceHandler`、`ResourceStore`、`ResourceProviderBase` 或 `ResourceKey`：
   - 右侧显示中文名称、职责、种类与关键协作。
   - `Ping 脚本`能在 Project 窗口定位源码。
   - `打开脚本`能在 Rider 打开对应文件。
6. 分别进入两个适配器，确认 Provider 与 BackendHandle 均可见。
7. 在分组图和类型图中验证滚轮缩放、中键/`Alt + 左键`平移、适配与 100% 重置。
8. 点击“刷新”，确认页面不显示架构元数据诊断。

## 6. 与计划的偏差

- 为满足“所有核心脚本”而不把测试和示例噪音混入正式目录，覆盖范围被精确定义为“所有显式接入生产程序集中的顶层类、接口、结构体和枚举”。
- 结构体和枚举常与主类共用脚本文件，因此源码索引增加了声明匹配回退，而不是强制每种类型独占一个文件。
- 分组节点不保存自由坐标，也不支持节点拖动；保持确定性自动布局，交互重点放在逐层进入与共享画布导航。

## 7. 后续规则

后续 Pooling、Event Center 及其他正式模块必须：

1. 在每个生产程序集声明 `FrameworkArchitectureAssemblyAttribute`。
2. 为所有纳入范围的顶层生产类型声明 `FrameworkArchitectureAttribute`。
3. 为跨类型关键协作维护 `RelatedTypes`，但不伪造逐方法调用图。
4. 在模块验收中检查目录诊断为零、源码可定位，并确认 Tests/Samples 未误接入。

本阶段没有修改 Framework Runtime 生命周期、中央设置资产、Resource 的加载/缓存/租约行为或公开调用契约。
