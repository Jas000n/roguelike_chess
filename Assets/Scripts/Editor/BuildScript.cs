using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class BuildScript
{
    public static void BuildMac()
    {
        Build(
            BuildTarget.StandaloneOSX,
            Path.Combine("Builds", "Mac"),
            "DragonChessLegends.app"
        );
    }

    public static void BuildWin64()
    {
        Build(
            BuildTarget.StandaloneWindows64,
            Path.Combine("Builds", "Windows"),
            "DragonChessLegends.exe"
        );
    }

    private static void Build(BuildTarget target, string relativeBuildDir, string outputName)
    {
        var projectRoot = Directory.GetCurrentDirectory();
        var buildDir = Path.Combine(projectRoot, relativeBuildDir);
        Directory.CreateDirectory(buildDir);

        string scenePath = EnsureMainScene(projectRoot);

        var options = new BuildPlayerOptions
        {
            scenes = new[] { scenePath },
            locationPathName = Path.Combine(buildDir, outputName),
            target = target,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Build failed: " + report.summary.result);
        UnityEngine.Debug.Log("Build succeeded: " + options.locationPathName);
    }

    private static string EnsureMainScene(string projectRoot)
    {
        const string scenePath = "Assets/Scenes/Main.unity";
        if (File.Exists(scenePath)) return scenePath;

        Directory.CreateDirectory(Path.Combine(projectRoot, "Assets", "Scenes"));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, scenePath);
        return scenePath;
    }
}
