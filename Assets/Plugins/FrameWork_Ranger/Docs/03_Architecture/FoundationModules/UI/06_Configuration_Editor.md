# UI 配置编辑器改版

2026-09-09。用户反馈项目中心配置选择为空、域参数英文且难以理解；已批准下拉选择、统一中文 Inspector 和明确的项目使用按钮。运行时首版的 API、字段名、枚举数值和资产 GUID 保持不变。

## 配置入口

UIConfigurationAssets 通过 AssetDatabase 查找全部 GlobalUIModule 资产，按路径排序，名称和路径共同显示。页面标注中央 GlobalConfig 中唯一已启用的 UI 条目；下拉只切换编辑对象，以 SessionState 保存资产 GUID。资产移动后可恢复，删除后回到当前项目配置；无项目配置时，唯一候选直接展示，多个候选保持待选。

“设为项目使用”先验证面板目录，再在临时 GlobalConfig 上用现有 ModuleGraphResolver 检查默认场景及覆盖场景的依赖。成功后原位替换唯一 UI 条目或追加新条目并启用，其他模块及顺序保持不动。支持 Undo 和资产保存；中央配置缺失、多个 UI 条目或校验失败均不写入。运行期间禁止安装切换和模板修改。

Prefab 选择移入可选辅助工具；通过资产选择按钮操作，无需拖拽。此选项仅用于组件/引用/Layout 检查，运行时资源仍由面板目录的后端与地址确定。

## 共用 Inspector

GlobalUIModuleInspector 同时供直接选中资产、UI 页面和中央模块列表内嵌编辑调用。主区域为“全局 UI／场景 UI”页签，分别解释存活范围。UI 域和面板目录分开，折叠条目显示标识、模式/所属域和排序/关闭策略。

域参数全部中文显示；屏幕叠加显示参考分辨率与适配滑杆，相机空间追加距相机距离，世界空间显示画布尺寸和世界缩放。显隐不重置其他模式的值。面板按身份与所属域、资源与逻辑、显示与交互、关闭行为分组；所属域从同目录下拉选择，失效引用提示。业务 Key、资源地址、Logic 类型名保持原值。Odin 属性树只承接多态 Logic 与继承的高级加载优先级，其余界面使用中文字段布局。

宽度不足 480 时标签与输入纵向排列，尺寸明确区分宽/高；屏幕适配滑杆显示按宽度/按高度两端含义。设置编辑使用 Undo，运行模式只读。业务 View 引用的手动绑定流程不受此次入口改版影响。

## 验证

验证完成。用户 GUI Unity 保持打开，CLI 使用 `Logs/UIEditorValidation/Project` 隔离副本（Assets、Packages、ProjectSettings 来自当前工作区，复制包缓存）。不将隔离副本中的临时测试资产或项目配置写回主工程。

聚焦用例覆盖资产发现/移动/删除与默认选择、独立安装的追加/替换/启用/撤销/重做、依赖及配置错误不写入、跨模式保存与 Logic 类型保留，以及 360/800 宽度下展开域和面板的真实 Inspector GUI 绘制。最终 EditMode 10/10 通过（6 项编辑器用例和 4 项配置/布局既有回归），包含真实 GUI 绘制与窄窗口分支断言；加载优先级/Odin 面板属性树可访问，查看不会改写 null 文本字段。


CLI 证据（相对于主工程）：

- Doctor：隔离工程路径与 Unity 6000.5.9f1 校验通过。
- Import：退出码 0，Logs/UIEditorValidation/Project/Logs/UnityCli/20260909-231011-import-16584/import.log。
- 最终 EditMode：10/10，Logs/UIEditorValidation/Project/Logs/UnityCli/20260909-231827-testeditmode-37892/editmode-results.xml；最终代码随该测试命令编译。
- 主工程未执行测试安装操作，也未关闭用户编辑器。实际 GUI 绘制通过 EditMode 窗口验证，不将已有运行时样例预览当作新版编辑器截图。
