# ADR-RM-001：垂直模块胶囊与集成边界

- 状态：已接受
- 日期：2026-08-19
- 所属阶段：Resource Management

## 背景

模块需要可选分发，同时 Addressables 不应污染 Core 或未来 Pooling 的最小依赖。

## 决定

使用 `BaseModules/ResourceManagement` 垂直胶囊，内部划分无具体后端依赖的 Runtime、UnityResources Integration、Addressables Integration、Editor、Tests 与 Samples。Core Runtime 不引用 Resource；Resource Runtime 不引用具体 Integration；未来 Pooling 只引用 Resource Runtime。

## 影响

删除某个后端集成时依赖关系清楚；当前项目模板仍要求两个集成都存在。Addressables 包和 Odin Addressables 支持资产属于项目级安装内容，不进入 Resource Runtime asmdef。

## 验证

Unity 编译、asmdef 检查、Player 构建和全框架回归通过。
