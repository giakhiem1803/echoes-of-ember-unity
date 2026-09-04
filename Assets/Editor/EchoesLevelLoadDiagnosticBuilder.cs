using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Produces isolated clean Windows builds used to verify that Level 01 can be
/// deserialized by a standalone player. These builds never overwrite the
/// submission build in Builds/Windows.
/// </summary>
public static class EchoesLevelLoadDiagnosticBuilder
{
    private const string Level01 = "Assets/Scenes/Level01_EmberRuins.unity";

    public static void BuildLevel01Diagnostic()
    {
        Build(
            new[] { Level01 },
            "Builds/Windows_Level01_Diagnostic/EchoesLevel01Diagnostic.exe");
    }

    // Isolates the player archive named level2: CampaignHub = level0,
    // KaelMovementTest = level1 and Level01_EmberRuins = level2.
    public static void BuildFirstThreeScenesDiagnostic()
    {
        Build(
            new[]
            {
                "Assets/Scenes/CampaignHub.unity",
                "Assets/Scenes/KaelMovementTest.unity",
                Level01
            },
            "C:/UnityBuilds/Echoes_Diagnostic_First3/EchoesFirstThree.exe");
    }

    public static void BuildFirstFourScenesDiagnostic()
    {
        Build(
            new[]
            {
                "Assets/Scenes/CampaignHub.unity",
                "Assets/Scenes/KaelMovementTest.unity",
                Level01,
                "Assets/Scenes/Level02_CrystalDepths.unity"
            },
            "C:/UnityBuilds/Echoes_Diagnostic_First4/EchoesFirstFour.exe");
    }

    public static void BuildCampaignToLevel01TransitionDiagnostic()
    {
        const string bootstrapScene = "Assets/Diagnostics/CampaignToLevel01Smoke.unity";
        Directory.CreateDirectory("Assets/Diagnostics");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "CampaignToLevel01Smoke";
        new GameObject("Scene Transition Smoke Bootstrap").AddComponent<SceneTransitionSmokeBootstrap>();
        EditorSceneManager.SaveScene(scene, bootstrapScene);

        Build(
            new[] { bootstrapScene, Level01 },
            "Builds/Windows_Transition_Diagnostic/EchoesTransitionDiagnostic.exe");
    }

    public static void BuildRepairedFull()
    {
        string[] scenes =
        {
            "Assets/Scenes/CampaignHub.unity",
            "Assets/Scenes/KaelMovementTest.unity",
            Level01,
            "Assets/Scenes/Level02_CrystalDepths.unity",
            "Assets/Scenes/Level03_AshenForge.unity",
            "Assets/Scenes/Level04_ShadowCitadel.unity",
            "Assets/Scenes/Level05_EmberThrone.unity"
        };

        Build(scenes, "Builds/Windows_Repaired/EchoesOfEmber.exe");
    }

    private static void Build(string[] scenes, string outputPath)
    {
        foreach (string scene in scenes)
        {
            if (!File.Exists(scene))
                throw new BuildFailedException("Missing required scene: " + scene);
        }

        string outputDirectory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrEmpty(outputDirectory))
            throw new BuildFailedException("Invalid output path: " + outputPath);

        Directory.CreateDirectory(outputDirectory);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(
            BuildTarget.StandaloneWindows64,
            new[] { GraphicsDeviceType.Direct3D11 });

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.CleanBuildCache
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException(
                $"Diagnostic build failed: {summary.result}; " +
                $"errors={summary.totalErrors}; warnings={summary.totalWarnings}");
        }

        Debug.Log(
            $"ECHOES DIAGNOSTIC BUILD SUCCESS | output={outputPath} | " +
            $"scenes={scenes.Length} | bytes={summary.totalSize} | " +
            $"warnings={summary.totalWarnings} | utc={DateTime.UtcNow:O}");
    }
}
