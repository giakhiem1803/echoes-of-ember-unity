using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class BuildReadinessRepair
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/CampaignHub.unity",
        "Assets/Scenes/KaelMovementTest.unity",
        "Assets/Scenes/Level01_EmberRuins.unity",
        "Assets/Scenes/Level02_CrystalDepths.unity",
        "Assets/Scenes/Level03_AshenForge.unity",
        "Assets/Scenes/Level04_ShadowCitadel.unity",
        "Assets/Scenes/Level05_EmberThrone.unity"
    };

    [MenuItem("Echoes of Ember/FINAL Repair Missing Scripts and Input", priority = -47)]
    public static void RepairAll()
    {
        int removedTotal = 0;
        foreach (string scenePath in ScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int removed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                removed += CleanRecursive(root.transform);

            EnsureEventSystem(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            removedTotal += removed;
            Debug.Log($"Build readiness: {scene.name}, removed missing scripts = {removed}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Build readiness complete. Total missing scripts removed = {removedTotal}");
    }

    public static void RunFinalPipeline()
    {
        UltimateEchoesUpgradeInstaller.Install();
        CompleteRpgMenuInstaller.Install();
        CampaignSceneBuilder.RebuildCampaignHubOnly();
        RepairAll();
        Debug.Log("FINAL SCENE PIPELINE SUCCESS");
    }

    private static int CleanRecursive(Transform node)
    {
        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(node.gameObject);
        for (int i = 0; i < node.childCount; i++)
            removed += CleanRecursive(node.GetChild(i));
        return removed;
    }

    private static void EnsureEventSystem(Scene scene)
    {
        EventSystem eventSystem = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            eventSystem = root.GetComponentInChildren<EventSystem>(true);
            if (eventSystem != null) break;
        }

        if (eventSystem == null)
        {
            GameObject go = new GameObject("EventSystem");
            SceneManager.MoveGameObjectToScene(go, scene);
            eventSystem = go.AddComponent<EventSystem>();
        }

        StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy);
        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
    }
}
