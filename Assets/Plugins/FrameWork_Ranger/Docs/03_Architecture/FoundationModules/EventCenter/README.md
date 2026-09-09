# Event Center 事件中心

2026-09-05 完成主要公共行为设计，2026-09-09 已实施首版：独立事件类静态入口、Global 唯一中心、成对手动注销、主线程同步池化派发、去重和注册顺序、监听异常隔离，以及明确的派发中增删、嵌套和未加载调用规则。Runtime、全局模板接线和最小样例已落盘，Unity Import、Event EditMode 7 项与 PlayMode 11 项测试通过。

- [HTY / YokiFrame 参考研究与设计起点](./01_Reference_Research_And_Design_Starting_Point.md)
- [事件类静态接口与 HTY 真实使用场景](./02_Event_Class_API_And_HTY_Usage.md)
- [完整架构与公共契约](./03_Architecture_And_Public_Contracts.md)
- [最小实施计划与验证范围](./04_Implementation_Plan.md)
- [实际使用与验收结果](./05_Usage_And_Acceptance.md)
- [当前要求与问答](../../../../../../../.workflow/event-center/requirements.json)
- [进度与恢复入口](../../../../../../../.workflow/event-center/progress.json)

延续项目既有的事件中心依赖引用池方向，具体池化对象与发送所有权在本轮设计中明确。历史 Pooling 验收状态见 [Pooling 入口](../Pooling/README.md)，不作为开始事件需求讨论的前置条件。

业务入口见 `FrameWork_Ranger.Events.EventModule`；独立事件类与启停监听组件示例位于 `BaseModules/EventCenter/Samples/Runtime`。事件载荷只能在同步回调期间借读，延后处理须复制数据。
