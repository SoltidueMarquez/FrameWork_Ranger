# ADR-RM-002：Lease、single-flight 与即时缓存移除

- 状态：已接受
- 日期：2026-08-19
- 所属阶段：Resource Management

## 背景

未来对象池需要共享同一 Prefab 加载，又必须明确每个调用者的释放责任，避免裸资源引用无法追踪。

## 决定

相同 `(Backend, Location, exact T)` 的并发请求共享一次底层加载；每个等待者获得独立引用型 LeaseState。最后一份 Lease 释放后立即移除框架缓存并释放后端 Handle。失败不缓存，取消的等待者不影响其他等待者。

## 影响与非目标

调用者必须持有并归还 Lease。缓存不跨零引用驻留，因此没有 TTL/LRU、预加载或空闲 Tick；如果未来真实项目需要驻留缓存，应新增 ADR。

## 验证

EditMode 覆盖 single-flight、独立 Lease、精确类型、取消、迟到结果、失败重试、幂等释放和 Shutdown。
