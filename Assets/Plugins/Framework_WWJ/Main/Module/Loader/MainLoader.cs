namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 可挂到场景中的 Loader 具体实现类。因 MainLoaderBase 为抽象类，Inspector 中需引用具体类型，
    /// 故使用本类：将 MainLoader 挂到同一场景的 GameObject 上，在 FrameworkEntry 的 _loader 字段中引用即可。
    /// </summary>
    public class MainLoader : MainLoaderBase { }
}