using Sirenix.OdinInspector;

namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 模块基础模板：统一维护通用状态，具体模块只重载 OnVaildXxx。
    /// </summary>
    public abstract class ModuleBase : IModule
    {
        public bool isLoading { get; private set; }
        public InitState currentInitState { get; private set; } = InitState.Sleep;
        public bool isRunning { get; private set; }

        [LabelText("初始化后直接运行")] public bool autoRun;
        
        public void Born()
        {
            OnVaildBorn();
        }

        public void Die()
        {
            OnVaildDie();
            isLoading = false;
            isRunning = false;
            currentInitState = InitState.Sleep;
        }

        public void BeginInit()
        {
            if (currentInitState == InitState.Success) return;
            isLoading = true;
            currentInitState = InitState.Loading;
            OnVaildBeginInit();
        }

        public void Init()
        {
            if (currentInitState == InitState.Success) return;
            OnVaildInit();
        }

        public void EndInit()
        {
            currentInitState = InitState.Success;
            isLoading = false;
            OnVaildEndInit();
            if(autoRun) Run();
        }

        public void BeginUnInit()
        {
            if (currentInitState == InitState.Sleep) return;
            isLoading = true;
            OnVaildBeginUnInit();
        }

        public void UnInit()
        {
            if (currentInitState == InitState.Sleep) return;
            OnVaildUnInit();
        }

        public void EndUnInit()
        {
            currentInitState = InitState.Sleep;
            isLoading = false;
            isRunning = false;
            OnVaildEndUnInit();
        }

        public void UpdateHandle()
        {
            if (currentInitState == InitState.Sleep || !isRunning) return;
            OnVaildUpdate();
        }

        public void FixedUpdateHandle()
        {
            if (currentInitState == InitState.Sleep || !isRunning) return;
            OnVaildFixedUpdate();
        }

        public void LateUpdateHandle()
        {
            if (currentInitState == InitState.Sleep || !isRunning) return;
            OnVaildLateUpdate();
        }

        public void Pause()
        {
            if (!isRunning) return;
            isRunning = false;
            OnVaildPause();
        }

        public void Run()
        {
            if (isRunning) return;
            isRunning = true;
            OnVaildRun();
        }

        protected virtual void OnVaildBorn() { }
        protected virtual void OnVaildDie() { }
        protected virtual void OnVaildBeginInit() { }
        protected virtual void OnVaildInit() { }
        protected virtual void OnVaildEndInit() { }
        protected virtual void OnVaildBeginUnInit() { }
        protected virtual void OnVaildUnInit() { }
        protected virtual void OnVaildEndUnInit() { }
        protected virtual void OnVaildUpdate() { }
        protected virtual void OnVaildFixedUpdate() { }
        protected virtual void OnVaildLateUpdate() { }
        protected virtual void OnVaildPause() { }
        protected virtual void OnVaildRun() { }
    }
}
