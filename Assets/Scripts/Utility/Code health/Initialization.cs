using UnityEngine;
using Unity.Collections;

public class Initialization
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnBeforeSceneLoadRuntimeMethod()
    {
        NativeLeakDetection.Mode = NativeLeakDetectionMode.Enabled;
        Debug.Log("Native leak detection enabled.");
    }
}
