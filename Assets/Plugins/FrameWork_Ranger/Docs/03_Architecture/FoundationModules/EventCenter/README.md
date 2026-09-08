# Event Center 事件中心

2026-09-05 重新启动需求与设计。主要公共行为问答已完成：独立事件类静态入口、Global 唯一中心、成对手动注销、主线程同步池化派发、去重和注册顺序、监听异常隔离，以及明确的派发中增删、嵌套和未加载调用规则。完整契约与最小实施计划已整理；Runtime、配置接线与 Unity 验证尚未实施。

- [HTY / YokiFrame 参考研究与设计起点](./01_Reference_Research_And_Design_Starting_Point.md)
- [事件类静态接口与 HTY 真实使用场景](./02_Event_Class_API_And_HTY_Usage.md)
- [完整架构与公共契约](./03_Architecture_And_Public_Contracts.md)
- [最小实施计划与验证范围](./04_Implementation_Plan.md)
- [当前要求与问答](../../../../../../../.workflow/event-center/requirements.json)
- [进度与恢复入口](../../../../../../../.workflow/event-center/progress.json)

延续项目既有的事件中心依赖引用池方向，具体池化对象与发送所有权在本轮设计中明确。历史 Pooling 验收状态见 [Pooling 入口](../Pooling/README.md)，不作为开始事件需求讨论的前置条件。

本轮未生成 Event Runtime、SO 配置或示例资产。实现后的公共契约和验证结果将在本目录补充。
