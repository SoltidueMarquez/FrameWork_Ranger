using System;

namespace Plugins.Framework_WWJ
{
    [System.Serializable]
    public abstract class MainLoaderBase : IMainLoader, IMainLoading
    {
        public IModule module { get; set; }
        public void Born()
        {
            throw new NotImplementedException();
        }

        public void Die()
        {
            throw new NotImplementedException();
        }

        public void Init()
        {
            throw new NotImplementedException();
        }

        public void UnInit()
        {
            throw new NotImplementedException();
        }

        public void Update()
        {
            throw new NotImplementedException();
        }

        public void FixedUpdate()
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Run()
        {
            throw new NotImplementedException();
        }

        public void LateUpdate()
        {
            throw new NotImplementedException();
        }

        public event Action onStartInitLife;
        public event Action onEndInitLife;
        public event Action onUpdateInitLife;
        public float Progress { get; }
        public string CurrentContent { get; }
    }
}
