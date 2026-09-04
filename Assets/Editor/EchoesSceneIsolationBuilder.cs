#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class EchoesSceneIsolationBuilder
{
    private const string PlayerPrefab = "Assets/Prefabs/Characters/Player_Kael.prefab";
    private const string Level01 = "Assets/Scenes/Level01_EmberRuins.unity";
    private const string DiagnosticFolder = "Assets/Diagnostics";

    public static void BuildFullClean()
    {
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
            new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });

        string output = Path.GetFullPath("Builds/Isolation/22_FullClean/22_FullClean.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { Level01 },
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.CleanBuildCache
        });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new InvalidOperationException($"Full clean build failed: {report.summary.result}");
        Debug.Log($"FULL CLEAN BUILD OK -> {output}");
    }

    public static void BuildCampaignClean()
    {
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
            new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });

        var scenes = new List<string>();
        foreach (var entry in EditorBuildSettings.scenes)
        {
            if (entry.enabled && File.Exists(Path.GetFullPath(entry.path))) scenes.Add(entry.path);
        }
        if (scenes.Count == 0) throw new InvalidOperationException("No enabled scenes in Build Settings.");

        string output = Path.GetFullPath("Builds/Windows/EchoesOfEmber.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.CleanBuildCache
        });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new InvalidOperationException($"Campaign clean build failed: {report.summary.result}");
        Debug.Log($"CAMPAIGN CLEAN BUILD OK ({scenes.Count} scenes) -> {output}");
    }

    public static void BuildAll()
    {
        Directory.CreateDirectory(DiagnosticFolder);
        var builds = new List<(string name, Action create)>
        {
            ("06_EnvironmentOnly", CreateEnvironmentOnly),
            ("07_NoDecor", CreateNoDecor),
            ("08_NoEnemies", CreateNoEnemies),
            ("09_NoPickupsGoal", CreateNoPickupsGoal)
        };

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
            new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });

        foreach (var item in builds)
        {
            item.create();
            string scenePath = $"{DiagnosticFolder}/{item.name}.unity";
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
            string output = Path.GetFullPath($"Builds/Isolation/{item.name}/{item.name}.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CleanBuildCache
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException($"Isolation build {item.name} failed: {report.summary.result}");
            Debug.Log($"ISOLATION BUILD OK: {item.name} -> {output}");
        }
    }

    public static void BuildGranular()
    {
        Directory.CreateDirectory(DiagnosticFolder);
        var builds = new List<(string name, Action create)>
        {
            ("10_FullNoUi", CreateFullNoUi),
            ("11_NoBackdropLayers", CreateNoBackdropLayers),
            ("12_NoSmallDecor", CreateNoSmallDecor),
            ("13_NoPlayer", CreateNoPlayer),
            ("14_NoPickups", CreateNoPickups),
            ("15_NoGoalHazards", CreateNoGoalHazards)
        };

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
            new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });

        foreach (var item in builds)
        {
            item.create();
            string scenePath = $"{DiagnosticFolder}/{item.name}.unity";
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
            string output = Path.GetFullPath($"Builds/Isolation/{item.name}/{item.name}.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException($"Isolation build {item.name} failed: {report.summary.result}");
            Debug.Log($"GRANULAR ISOLATION BUILD OK: {item.name} -> {output}");
        }
    }

    public static void BuildUiIsolation()
    {
        Directory.CreateDirectory(DiagnosticFolder);
        var builds = new List<(string name, Action create)>
        {
            ("16_NoCanvas", () => OpenLevelAndRemove("Canvas")),
            ("17_NoGameManager", () => OpenLevelAndRemove("GameManager")),
            ("18_NoEventSystem", () => OpenLevelAndRemove("EventSystem")),
            ("19_NoCanvasNoGameManager", () => OpenLevelAndRemove("Canvas", "GameManager")),
            ("20_NoCanvasNoEventSystem", () => OpenLevelAndRemove("Canvas", "EventSystem")),
            ("21_NoGameManagerNoEventSystem", () => OpenLevelAndRemove("GameManager", "EventSystem"))
        };

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
            new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });

        foreach (var item in builds)
        {
            item.create();
            string scenePath = $"{DiagnosticFolder}/{item.name}.unity";
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
            string output = Path.GetFullPath($"Builds/Isolation/{item.name}/{item.name}.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException($"UI isolation build {item.name} failed: {report.summary.result}");
            Debug.Log($"UI ISOLATION BUILD OK: {item.name} -> {output}");
        }
    }

    private static Camera AddCamera()
    {
        var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0, 0, -10);
        var camera = go.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5;
        camera.backgroundColor = new Color(.03f, .04f, .08f);
        return camera;
    }

    private static void NewEmpty() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    private static void CreateCameraOnly()
    {
        NewEmpty();
        AddCamera();
    }

    private static void CreateBackground()
    {
        NewEmpty();
        AddCamera();
        string path = "Assets/Art/Tilesets/Dungeon/PNG/Background/Pale/Background.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        var go = new GameObject("CraftPix Background", typeof(SpriteRenderer));
        go.GetComponent<SpriteRenderer>().sprite = sprite;
        go.transform.localScale = Vector3.one * 2f;
    }

    private static void CreatePlayer()
    {
        NewEmpty();
        AddCamera();
        var ground = new GameObject("Ground", typeof(BoxCollider2D), typeof(SpriteRenderer));
        ground.transform.position = new Vector3(0, -2.5f, 0);
        ground.transform.localScale = new Vector3(20, 1, 1);
        var sr = ground.GetComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        sr.color = new Color(.25f, .12f, .08f);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        if (prefab == null) throw new FileNotFoundException(PlayerPrefab);
        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.transform.position = Vector3.zero;
    }

    private static void CreateUi()
    {
        NewEmpty();
        AddCamera();
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var textGo = new GameObject("Diagnostic Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(canvasGo.transform, false);
        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = "UI DIAGNOSTIC";
        text.fontSize = 48;
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.anchorMin = new Vector2(.25f, .4f);
        text.rectTransform.anchorMax = new Vector2(.75f, .6f);
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static void CreateWorldWithoutUi()
    {
        if (!File.Exists(Path.GetFullPath(Level01))) throw new FileNotFoundException(Level01);
        EditorSceneManager.OpenScene(Level01, OpenSceneMode.Single);
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "Canvas" || root.name == "EventSystem" || root.name == "GameManager")
                UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void OpenLevelAndRemove(params string[] prefixes)
    {
        EditorSceneManager.OpenScene(Level01, OpenSceneMode.Single);
        var scene = SceneManager.GetActiveScene();
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go == null || go.scene != scene) continue;
            foreach (string prefix in prefixes)
            {
                if (go.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                    break;
                }
            }
        }
    }

    private static void CreateEnvironmentOnly()
    {
        OpenLevelAndRemove("Canvas", "EventSystem", "GameManager", "Kael", "Player", "Ember Shard",
            "Heart Pickup", "Skeleton", "Fire Spirit", "Ruins Guardian", "Lava Death Zone",
            "Ember Gate", "Ember Checkpoint", "RPG Chests", "Fireball");
    }

    private static void CreateNoDecor()
    {
        OpenLevelAndRemove("Canvas", "EventSystem", "GameManager", "Dungeon Backdrop", "Dungeon Depth",
            "Dungeon Mist", "Torch", "Stalactite", "Stalagmite", "Ancient Statue", "Bridge Detail",
            "Gate Torch");
    }

    private static void CreateNoEnemies()
    {
        OpenLevelAndRemove("Canvas", "EventSystem", "GameManager", "Skeleton", "Fire Spirit", "Ruins Guardian");
    }

    private static void CreateNoPickupsGoal()
    {
        OpenLevelAndRemove("Canvas", "EventSystem", "GameManager", "Ember Shard", "Heart Pickup",
            "Lava Death Zone", "Ember Gate", "Ember Checkpoint", "RPG Chests", "Fireball");
    }

    private static void CreateFullNoUi()
    {
        OpenLevelAndRemove("Canvas", "EventSystem", "GameManager");
    }

    private static void CreateNoBackdropLayers()
    {
        OpenLevelAndRemove("Canvas", "EventSystem", "GameManager", "Dungeon Backdrop", "Dungeon Depth",
            "Dungeon Mist");
    }

    private static void CreateNoSmallDecor()
    {
        OpenLevelAndRemove("Canvas", "EventSystem", "GameManager", "Torch", "Stalactite", "Stalagmite",
            "Ancient Statue", "Bridge Detail", "Gate Torch");
    }

    private static void CreateNoPlayer()
    {
        OpenLevelAndRemove("Canvas", "EventSystem", "GameManager", "Kael", "Player");
    }

    private static void CreateNoPickups()
    {
        OpenLevelAndRemove("Canvas", "EventSystem", "GameManager", "Ember Shard", "Heart Pickup",
            "RPG Chests", "Fireball");
    }

    private static void CreateNoGoalHazards()
    {
        OpenLevelAndRemove("Canvas", "EventSystem", "GameManager", "Lava Death Zone", "Ember Gate",
            "Ember Checkpoint");
    }
}
#endif
