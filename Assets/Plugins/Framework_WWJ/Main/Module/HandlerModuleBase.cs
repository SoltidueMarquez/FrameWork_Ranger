using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 支持 Handler 模式的模块基类。
    /// 将具体的业务逻辑委派给泛型参数 T (IModuleHandler) 的实现类。
    /// </summary>
    /// <typeparam name="T">处理器接口类型</typeparam>
    public abstract class HandlerModuleBase<T> : ModuleBase where T : IModuleHandler
    {
        /// <summary>
        /// 处理器实例
        /// </summary>
        [OdinSerialize, LabelText("处理器实例")] public T handler { get; protected set; }

        /// <summary>
        /// 具体的处理器实现类型，子类需提供以便通过反射实例化。
        /// </summary>
        protected abstract Type handleType { get; }

        public bool enableDebug;
        
        protected override void OnVaildBorn()
        {
            if (handler == null && handleType != null)
            {
                try
                {
                    handler = (T)Activator.CreateInstance(handleType);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[HandlerModuleBase] 实例化 Handler {handleType.Name} 失败: {e}");
                }
            }

            if (handler != null)
            {
                handler.module = this;
                handler.Born();
            }
            else if (handleType == null)
            {
                Debug.LogWarning($"[HandlerModuleBase] 模块 {GetType().Name} 未指定 handleType 且 handler 为空。");
            }
        }

        protected override void OnVaildDie()
        {
            if (handler != null)
            {
                handler.Die();
                // 注意：由于 handler 是序列化的，我们不再这里将其设为 default，
                // 否则会清空 Inspector 中的配置。
            }
        }

        protected override void OnVaildInit()
        {
            handler?.Init();
        }

        protected override void OnVaildUnInit()
        {
            handler?.UnInit();
        }

        protected override void OnVaildUpdate()
        {
            handler?.Update();
        }

        protected override void OnVaildFixedUpdate()
        {
            handler?.FixedUpdate();
        }

        protected override void OnVaildLateUpdate()
        {
            if (handler is IModuleHandlerLateUpdateSupport lateSupport)
            {
                lateSupport.LateUpdate();
            }
        }

        protected override void OnVaildPause()
        {
            handler?.Pause();
        }

        protected override void OnVaildRun()
        {
            handler?.Run();
        }
    }
}
