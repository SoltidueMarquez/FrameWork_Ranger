# Framework_WWJ 旧实现清理记录

> 完成日期：2026-08-06  
> 清理目标：为全量重建设立空白代码基线。

## 已删除

已完整删除以下两个目录：

- `Assets/Plugins/Framework_WWJ/Main`
- `Assets/Plugins/Framework_WWJ/Utils`

清理内容包括：

- 31 个旧 C# 文件，约 4,783 行；
- 旧 Module/Loader/Config/Handler 核心；
- Pool、Resource、Audio 遗留结构；
- SO 与 Editor 工具；
- List/Dictionary/FastDictionary 工具；
- 旧 `Main.prefab`；
- 已失效的 `ObjectPoolCfg.asset`；
- 对应 Unity `.meta`；
- 原对象池技术文档（其设计信息已归档为 `Docs/Legacy_Object_Pool_Design.md`）。

在本次清理前已经被用户删除、因此没有重复处理的旧内容包括：

- `Main/Audio/AudioModule.cs`；
- `Main/Module/Module SO` 下两份运行配置资产；
- `Assets/Resources/Cube.prefab` 测试模板。

## 已保留

- `Assets/Plugins/Framework_WWJ/Docs` 及本次整理出的文档；
- Framework_WWJ 之外的第三方插件；
- `Packages`、`ProjectSettings` 与 DOTween 设置等既有用户改动；
- `Assets/Scenes/SampleScene.unity` 的其他内容。

## 已知待办

`SampleScene.unity` 仍保存对旧 `Main.prefab` GUID `ad177c2509f9490499392744a649bf59` 的引用。旧 Prefab 已删除，因此在 Phase 1 创建新框架入口时需要：

1. 清理场景中的 Missing Prefab 实例；
2. 放入新版 Framework Entry；
3. 保存场景并做 PlayMode 验收。

本轮没有擅自编辑场景，避免误碰用户的其他场景内容。

## 恢复来源

旧实现仍可从 Git 历史读取：

- 最新旧实现基线：提交 `b24566c`；
- 对象池首次加入：`b6d270f`；
- 旧 Loader 跑通：`2df9f03`；
- Loader 计划：`d3e0798`；
- 更早的最小脚本评估：`f63590f`。

清理前未跟踪的 `Main/ModuleBase` 与 `HEAD` 的 `Main/Module` 内容完全一致，因此没有额外未归档的新核心实现丢失。

## 当前基线

Framework_WWJ 根目录只保留设计资料。下一步应先分析用户提供的参考项目、确定第一个游戏目标并完成架构决策，再创建新的 `Runtime/Editor/Tests` 或最终确认的目录结构。

