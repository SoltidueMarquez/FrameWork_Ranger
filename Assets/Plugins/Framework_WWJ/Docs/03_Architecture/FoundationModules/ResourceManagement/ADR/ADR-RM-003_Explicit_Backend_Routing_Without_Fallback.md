# ADR-RM-003：显式后端路由且禁止回退

- 状态：已接受
- 日期：2026-08-19
- 所属阶段：Resource Management

## 背景

同一文字位置可能同时存在于 Resources 与 Addressables。隐式优先级或失败回退会掩盖配置错误，并让缓存身份不稳定。

## 决定

`ResourceKey` 必须携带 `ResourceBackendKind`；不提供裸字符串重载。路由只查询指定 Provider，绝不尝试另一后端。Addressables 地址和 Key 相等使用 Ordinal；Resources 路径必须相对 Resources 目录、不带扩展名或前缀。

## 影响

调用代码更明确；迁移资源后必须主动修改 Key。相同 Location 的两个后端是完全独立资源。

## 验证

EditMode 与 PlayMode 均覆盖同名双后端、无效位置、缺少 Provider 和禁止回退。
