using System;

namespace Plugins.Framework_WWJ
{
    public interface IMainLoading
    {
        public event Action onStartInitLife;
        public event Action onEndInitLife;
        public event Action onUpdateInitLife;

        public float Progress { get; }
        public string CurrentContent { get;}
    }
}