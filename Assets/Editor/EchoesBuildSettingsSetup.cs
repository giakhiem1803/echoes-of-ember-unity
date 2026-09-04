#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/// <summary>Keeps the authored gameplay scenes playable from UI buttons and ready for PC builds.</summary>
[InitializeOnLoad]
public static class EchoesBuildSettingsSetup
{
    static EchoesBuildSettingsSetup() => EditorApplication.delayCall += EnsureScenes;

    private static void EnsureScenes()
    {
        string[] required = { "Assets/Scenes/KaelMovementTest.unity", "Assets/Scenes/Level01_EmberRuins.unity" };
        var scenes = EditorBuildSettings.scenes.ToList();
        bool changed = false;
        foreach (string path in required)
        {
            if (scenes.Any(scene => scene.path == path)) continue;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            changed = true;
        }
        if (changed) EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
