# Pooling 参考研究

> 参考工程默认只读：`D:/unityhub/UnityProjects/LyingBottle` 与 `D:/unityhub/UnityProjects/YokiFrame`。

## HTY / LyingBottle

HTY 的对象池设计提供 SO 配置、初始数量、按批扩容与定时缩容；LyingBottle 展示了这些能力在真实游戏中的接线和诊断需求。正式实现保留配置与循环缩容问题定义，并修正为“完全无借出后重新计满一个周期、每次仅缩一个扩容批次、永不低于 InitialCount”。

没有复制的部分包括：常驻全局 GameObject 根、直接调用 Addressables、运行时动态注册、字符串静默失败、CSV 配置以及反射修补。Scene GameObject 池明确交给 SceneScope，资源身份和释放交给 ResourceModule。

## YokiFrame

YokiFrame 的纯 C# Pool/Kit 强调本地或共享实例、显式所有权、固定容量语义与对重复归还的严格处理。正式实现吸收这些边界：使用引用相等集合同时跟踪空闲和借出对象，并让外来、重复和错池归还成为可观察错误。

没有复制动态全局服务入口或与 FrameWork_Ranger Scope 不一致的生命周期。Reference 与 GameObject 也没有被捆绑成一个程序集，从而让未来 Event Center 保持纯 C# 依赖。

## FrameWork_Ranger 结论

- HTY 负责成熟的配置、批量和循环行为参考。
- YokiFrame 负责所有权、容量与错误语义参考。
- FrameWork_Ranger 由 Scope、Resource Lease、严格回滚和 Editor/Build 共用诊断收口最终设计。
