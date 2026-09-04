using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class EchoesFinalAudit
{
    private static readonly string[] GameplayScenes =
    {
        "Assets/Scenes/Level01_EmberRuins.unity",
        "Assets/Scenes/Level02_CrystalDepths.unity",
        "Assets/Scenes/Level03_AshenForge.unity",
        "Assets/Scenes/Level04_ShadowCitadel.unity",
        "Assets/Scenes/Level05_EmberThrone.unity"
    };

    private static readonly string[] RequiredScenes =
    {
        "Assets/Scenes/CampaignHub.unity",
        "Assets/Scenes/KaelMovementTest.unity",
        "Assets/Scenes/Level01_EmberRuins.unity",
        "Assets/Scenes/Level02_CrystalDepths.unity",
        "Assets/Scenes/Level03_AshenForge.unity",
        "Assets/Scenes/Level04_ShadowCitadel.unity",
        "Assets/Scenes/Level05_EmberThrone.unity"
    };

    [MenuItem("Echoes of Ember/FINAL AUDIT - Five Levels", priority = -46)]
    public static void Run()
    {
        string originalScene = SceneManager.GetActiveScene().path;
        var failures = new List<string>();

        RepairGameplayMenuCommands();
        ValidateBuildSettings(failures);
        foreach (string path in GameplayScenes)
            ValidateGameplayScene(path, failures);

        if (!string.IsNullOrEmpty(originalScene) && File.Exists(originalScene))
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

        if (failures.Count > 0)
            throw new InvalidOperationException("ECHOES FINAL AUDIT FAILED:\n - " + string.Join("\n - ", failures));

        Debug.Log("ECHOES FINAL AUDIT SUCCESS: 5/5 gameplay scenes, build order, UI buttons, EventSystem, gate, routes, and missing scripts verified.");
    }

    [MenuItem("Echoes of Ember/REPAIR - Gameplay Menu Commands", priority = -47)]
    public static void RepairGameplayMenuCommands()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before repairing gameplay menus.");

        string originalScene = SceneManager.GetActiveScene().path;
        foreach (string path in GameplayScenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            GameManager manager = FindInRoots<GameManager>(scene.GetRootGameObjects());
            if (manager != null)
            {
                SerializedObject serialized = new SerializedObject(manager);
                RepairPanel(ReadObject(serialized, "pausePanel"), "RESUME", RpgMenuCommand.Resume);
                RepairPanel(ReadObject(serialized, "pausePanel"), "RESTART", RpgMenuCommand.Restart);
                RepairPanel(ReadObject(serialized, "gameOverPanel"), "RESTART", RpgMenuCommand.Restart);
                RepairPanel(ReadObject(serialized, "gameOverPanel"), "CAMPAIGN", RpgMenuCommand.CampaignHub);
                RepairPanel(ReadObject(serialized, "victoryPanel"), "NEXT LEVEL", RpgMenuCommand.NextLevel);
                RepairPanel(ReadObject(serialized, "victoryPanel"), "CAMPAIGN", RpgMenuCommand.CampaignHub);
            }

            // Generated scenes can retain an inactive legacy panel which is no
            // longer referenced by GameManager. Repair every visible/inactive
            // scene button too, so any panel selected at runtime is functional.
            RepairAllSceneButtons(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(originalScene) && AssetDatabase.LoadAssetAtPath<SceneAsset>(originalScene) != null)
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
        Debug.Log("Echoes of Ember: gameplay menu commands repaired in all five levels.");
    }

    private static void RepairAllSceneButtons(Scene scene)
    {
        IEnumerable<Button> buttons = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Button>(true));

        foreach (Button button in buttons)
        {
            string label = (button.name + " " + string.Join(" ", button.GetComponentsInChildren<TMP_Text>(true)
                .Select(item => item.text))).ToUpperInvariant();

            RpgMenuCommand? desired = null;
            if (label.Contains("NEXT LEVEL")) desired = RpgMenuCommand.NextLevel;
            else if (label.Contains("CAMPAIGN")) desired = RpgMenuCommand.CampaignHub;
            else if (label.Contains("RESUME")) desired = RpgMenuCommand.Resume;
            else if (label.Contains("RESTART")) desired = RpgMenuCommand.Restart;

            if (!desired.HasValue) continue;
            ConfigureEveryCommand(button, desired.Value);
        }
    }

    private static void RepairPanel(GameObject panel, string text, RpgMenuCommand command)
    {
        if (panel == null) return;
        Button[] matches = panel.GetComponentsInChildren<Button>(true).Where(button =>
        {
            string label = string.Join(" ", button.GetComponentsInChildren<TMP_Text>(true).Select(item => item.text));
            return button.name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 || label.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }).ToArray();

        // Some older generated scenes contain both the legacy panel and the
        // polished replacement. Repair every matching button so whichever
        // panel is displayed always remains clickable.
        foreach (Button match in matches)
        {
            ConfigureEveryCommand(match, command);
        }
    }

    private static void ConfigureEveryCommand(Button button, RpgMenuCommand command)
    {
        RpgMenuCommandButton[] targets = button.GetComponents<RpgMenuCommandButton>();
        if (targets.Length == 0)
            targets = new[] { button.gameObject.AddComponent<RpgMenuCommandButton>() };

        foreach (RpgMenuCommandButton target in targets)
        {
            target.Configure(command);
            EditorUtility.SetDirty(target);
        }

        EditorUtility.SetDirty(button.gameObject);
    }

    private static void ValidateBuildSettings(List<string> failures)
    {
        HashSet<string> enabled = new HashSet<string>(EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path), StringComparer.OrdinalIgnoreCase);

        foreach (string path in RequiredScenes)
        {
            if (!File.Exists(path)) failures.Add($"Scene file missing: {path}");
            else if (!enabled.Contains(path)) failures.Add($"Scene not enabled in Build Settings: {path}");
        }
    }

    private static void ValidateGameplayScene(string path, List<string> failures)
    {
        if (!File.Exists(path))
        {
            failures.Add($"Cannot audit missing scene: {path}");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        string label = scene.name;
        GameObject[] roots = scene.GetRootGameObjects();

        int missing = roots.Sum(root => CountMissingRecursive(root.transform));
        if (missing != 0) failures.Add($"{label}: {missing} missing script(s)");

        GameManager manager = FindInRoots<GameManager>(roots);
        PlayerController player = FindInRoots<PlayerController>(roots);
        EventSystem eventSystem = FindInRoots<EventSystem>(roots);
        LevelGoal goal = FindInRoots<LevelGoal>(roots);

        if (manager == null) failures.Add($"{label}: GameManager missing");
        if (player == null) failures.Add($"{label}: PlayerController missing");
        if (goal == null) failures.Add($"{label}: LevelGoal/Ember Gate missing");
        if (eventSystem == null) failures.Add($"{label}: EventSystem missing");
        else if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            failures.Add($"{label}: InputSystemUIInputModule missing");

        Canvas canvas = FindInRoots<Canvas>(roots);
        if (canvas == null) failures.Add($"{label}: Canvas missing");
        else
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                failures.Add($"{label}: Canvas Scaler is not Scale With Screen Size");
        }

        if (manager != null)
        {
            SerializedObject serialized = new SerializedObject(manager);
            GameObject pause = ReadObject(serialized, "pausePanel");
            GameObject over = ReadObject(serialized, "gameOverPanel");
            GameObject victory = ReadObject(serialized, "victoryPanel");
            if (pause == null) failures.Add($"{label}: pausePanel not assigned");
            if (over == null) failures.Add($"{label}: gameOverPanel not assigned");
            if (victory == null) failures.Add($"{label}: victoryPanel not assigned");
            ValidateCommandButton(label, over, "RESTART", RpgMenuCommand.Restart, failures);
            ValidateCommandButton(label, over, "CAMPAIGN", RpgMenuCommand.CampaignHub, failures);
            ValidateCommandButton(label, victory, "NEXT LEVEL", RpgMenuCommand.NextLevel, failures);
            ValidateCommandButton(label, victory, "CAMPAIGN", RpgMenuCommand.CampaignHub, failures);
        }

        ValidateRoute(scene, failures);
    }

    private static void ValidateRoute(Scene scene, List<string> failures)
    {
        Transform routeRoot = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(item => item.name == "ULTIMATE CRAFTPIX EXPANSION");

        if (routeRoot == null)
        {
            failures.Add($"{scene.name}: expansion route root missing");
            return;
        }

        List<Bounds> route = routeRoot.GetComponentsInChildren<BoxCollider2D>(true)
            .Where(collider => collider.gameObject.name.StartsWith("Route Platform", StringComparison.Ordinal))
            // Width 3.4 platforms are the optional upper route. Only audit the
            // mandatory lower route (width 10+), otherwise interleaving both
            // elevations creates false horizontal-gap reports.
            .Where(collider => Mathf.Abs(collider.size.x * collider.transform.lossyScale.x) >= 5f)
            .Select(collider => collider.bounds)
            .OrderBy(bounds => bounds.min.x)
            .ToList();

        if (route.Count < 7)
        {
            failures.Add($"{scene.name}: expected at least 7 route platforms, found {route.Count}");
            return;
        }

        if (route[0].min.x > 40.1f || route[route.Count - 1].max.x < 123.9f)
            failures.Add($"{scene.name}: route does not cover x=40..124");

        for (int i = 1; i < route.Count; i++)
        {
            float gap = route[i].min.x - route[i - 1].max.x;
            if (gap > .15f)
                failures.Add($"{scene.name}: impassable route gap {gap:0.00} between {route[i - 1].center.x:0.0} and {route[i].center.x:0.0}");
        }
    }

    private static GameObject ReadObject(SerializedObject serialized, string property)
    {
        SerializedProperty value = serialized.FindProperty(property);
        return value == null ? null : value.objectReferenceValue as GameObject;
    }

    private static void ValidateCommandButton(string scene, GameObject panel, string text, RpgMenuCommand expected, List<string> failures)
    {
        if (panel == null) return;
        Button[] matches = panel.GetComponentsInChildren<Button>(true).Where(button =>
        {
            string label = string.Join(" ", button.GetComponentsInChildren<TMP_Text>(true).Select(item => item.text));
            return button.name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 || label.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }).ToArray();

        if (matches.Length == 0)
        {
            failures.Add($"{scene}: button '{text}' missing on {panel.name}");
            return;
        }

        bool hasExpectedCommand = matches.Any(match =>
        {
            RpgMenuCommandButton command = match.GetComponent<RpgMenuCommandButton>();
            if (command == null) return false;
            return command.Command == expected;
        });

        if (!hasExpectedCommand)
        {
            string actual = string.Join("; ", matches.Select(match =>
            {
                string commands = string.Join(",", match.GetComponents<RpgMenuCommandButton>()
                    .Select(item => item.Command.ToString()));
                return $"{HierarchyPath(match.transform)}=[{commands}]";
            }));
            failures.Add($"{scene}: no '{text}' button has the expected {expected} command. Actual: {actual}");
        }
    }

    private static string HierarchyPath(Transform node)
    {
        var parts = new Stack<string>();
        while (node != null)
        {
            parts.Push(node.name);
            node = node.parent;
        }
        return string.Join("/", parts);
    }

    private static T FindInRoots<T>(IEnumerable<GameObject> roots) where T : Component
    {
        foreach (GameObject root in roots)
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    private static int CountMissingRecursive(Transform node)
    {
        int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject);
        for (int i = 0; i < node.childCount; i++) count += CountMissingRecursive(node.GetChild(i));
        return count;
    }
}
