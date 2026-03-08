using UnityEngine;

/// <summary>
/// 自动启动：进入Play或打包运行时自动创建游戏。
/// 用户不需要手动拖脚本。
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (Object.FindFirstObjectByType<RoguelikeFramework>() != null) return;

        var go = new GameObject("RoguelikeFramework");
        go.AddComponent<RoguelikeFramework>();
    }
}
