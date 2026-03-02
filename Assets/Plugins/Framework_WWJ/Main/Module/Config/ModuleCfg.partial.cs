#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// ModuleCfg 的编辑器扩展部分。
    /// 仅在 Unity 编辑器下编译，用于：
    /// - 监听 PlayMode 状态变化；
    /// - 当脚本化资源被修改或切换 Play/编辑模式时，自动让内部缓存（静态配置缓存）失效。
    ///
    /// 这样可以保证：
    /// - 你在 Inspector 中改了 cfgDatas 等字段后，下次运行时不会继续使用旧缓存；
    /// - 退出 PlayMode 后，ModuleCfg 不会残留运行时状态。
    /// </summary>
    public partial class ModuleCfg
    {
#if UNITY_EDITOR
        /// <summary>
        /// ScriptableObject 被加载或 Domain Reload 后 Unity 会调用 OnEnable。
        /// 这里安全地（先移除再添加）注册 PlayMode 状态切换事件，避免重复订阅。
        /// </summary>
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        /// <summary>
        /// ScriptableObject 被卸载时调用，解除事件订阅，避免内存泄漏。
        /// </summary>
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        }

        /// <summary>
        /// 当 SO 在 Inspector 中被修改时调用。
        /// - 任何配置字段的修改，都意味着 GetStaticCfgData 需要重新组合结果；
        /// - 因此这里直接让缓存失效。
        /// </summary>
        private void OnValidate()
        {
            InvalidateCfgCache();
            // 非序列化字段，无需 SetDirty；如果你希望立刻刷新依赖界面，可按需：EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// 监听 Unity 播放模式切换事件：
        /// - ExitingPlayMode：即将退出播放；
        /// - EnteredEditMode：真正回到编辑模式。
        /// 在这两个时机都清理 ModuleCfg 的内部缓存，避免跨模式“带状态”。
        /// </summary>
        private void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            // 两个时机都清：刚退出 Play（ExitingPlayMode）以及真正进入编辑模式（EnteredEditMode）
            if (state == PlayModeStateChange.ExitingPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
            {
                InvalidateCfgCache();
            }
        }
#endif
    }
}