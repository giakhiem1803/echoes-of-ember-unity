using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class EchoesWindowsBuilder
{
    // Keep the submission player in a clean, standalone directory. Reusing an
    // older player folder can leave stale split data archives (level0/level1/
    // level2) beside the newly generated executable.
    private const string OutputPath = "C:/UnityBuilds/EchoesOfEmber_Final/EchoesOfEmber.exe";

    [MenuItem("Echoes of Ember/Build Windows DX11", priority = 900)]
    public static void BuildWindows()
    {
        ConfigureScenes();

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && File.Exists(scene.path))
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new BuildFailedException("No enabled scenes were found in Build Settings.");

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
            new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneWindows64,
            // Recreate all player data archives. The previous incremental player
            // cache emitted a corrupt level2 archive even when the output folder
            // itself was new.
            options = BuildOptions.CleanBuildCache
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"Windows build failed: {summary.result} ({summary.totalErrors} errors)");

        Debug.Log($"ECHOES WINDOWS BUILD SUCCESS: {OutputPath}, {summary.totalSize} bytes, {summary.totalWarnings} warnings");
    }

    private static void ConfigureScenes()
    {
        string[] required =
        {
            "Assets/Scenes/CampaignHub.unity",
            "Assets/Scenes/KaelMovementTest.unity",
            "Assets/Scenes/Level01_EmberRuins.unity",
            "Assets/Scenes/Level02_CrystalDepths.unity",
            "Assets/Scenes/Level03_AshenForge.unity",
            "Assets/Scenes/Level04_ShadowCitadel.unity",
            "Assets/Scenes/Level05_EmberThrone.unity"
        };

        List<EditorBuildSettingsScene> scenes = required
            .Where(path => File.Exists(path))
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToList();
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
