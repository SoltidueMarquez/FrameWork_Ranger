using UnityEngine;

namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 框架入口：挂在场景中，持有 Loader 与配置，驱动 Born → Init → Update → UnInit → Die 生命周期；
    /// 协程由本脚本托管，不依赖 Global。
    /// </summary>
    public class FrameworkEntry : MonoBehaviour
    {
        [SerializeField] private MainLoaderBase m_loader;
        [SerializeField] private ModuleCfg m_moduleCfg;

        private void Start()
        {
            if (m_loader == null)
            {
                Debug.LogError("[框架入口] Loader 没有 assigned.");
                return;
            }
            m_loader.SetCfg(m_moduleCfg);
            m_loader.SetCoroutineHost(this);
            m_loader.Born();
            m_loader.Init(this);
        }

        private void Update() => m_loader?.LoaderUpdate();
        private void FixedUpdate() => m_loader?.LoaderFixedUpdate();
        private void LateUpdate() => m_loader?.LoaderLateUpdate();

        private void OnDestroy()
        {
            if (m_loader != null)
            {
                m_loader.UnInit();
                m_loader.Die();
            }
        }
    }
}
