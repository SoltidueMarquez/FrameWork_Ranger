using UnityEngine;
using Plugins.Framework_WWJ;

public class AudioModule : ModuleBase
{
    public int a;

    private string Tag => "[AudioModule]";

    protected override void OnVaildBorn()
    {
        Debug.Log($"{Tag} Born");
    }

    protected override void OnVaildBeginInit()
    {
        Debug.Log($"{Tag} BeginInit  state={currentInitState} isLoading={isLoading}");
    }

    protected override void OnVaildInit()
    {
        Debug.Log($"{Tag} Init");
    }

    protected override void OnVaildEndInit()
    {
        Debug.Log($"{Tag} EndInit  state={currentInitState} isRunning={isRunning}");
    }

    protected override void OnVaildBeginUnInit()
    {
        Debug.Log($"{Tag} BeginUnInit  state={currentInitState}");
    }

    protected override void OnVaildUnInit()
    {
        Debug.Log($"{Tag} UnInit");
    }

    protected override void OnVaildEndUnInit()
    {
        Debug.Log($"{Tag} EndUnInit  state={currentInitState} isRunning={isRunning}");
    }

    protected override void OnVaildDie()
    {
        Debug.Log($"{Tag} Die");
    }

    protected override void OnVaildUpdate()
    {
        Debug.Log($"{Tag} Update  isRunning={isRunning}");
    }

    protected override void OnVaildFixedUpdate()
    {
        Debug.Log($"{Tag} FixedUpdate  isRunning={isRunning}");
    }

    protected override void OnVaildLateUpdate()
    {
        Debug.Log($"{Tag} LateUpdate  isRunning={isRunning}");
    }

    protected override void OnVaildPause()
    {
        Debug.Log($"{Tag} Pause  isRunning={isRunning}");
    }

    protected override void OnVaildRun()
    {
        Debug.Log($"{Tag} Run  isRunning={isRunning}");
    }
}