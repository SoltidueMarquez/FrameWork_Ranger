using UnityEngine;
using Plugins.Framework_WWJ;

public class AudioModule : ModuleBase
{
    public int a;
    protected override void OnVaildBorn()
    {
        Debug.Log("音频模块：Born");
    }

    protected override void OnVaildInit()
    {
        Run();
        Debug.Log("音频模块：Init 完成");
    }
}