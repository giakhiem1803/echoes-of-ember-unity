using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Assigns the real orange CraftPix projectile frames (4_1 sheet) to Kael in
/// the character prefab and every campaign scene.  The previous installer was
/// loading only one arbitrary sub-sprite from each sheet.
/// </summary>
public static class CraftpixFireballInstaller
{
    private const string Stamp = "Echoes.CraftpixFireball.Rev2";
    private const string Sheet = "Assets/Art/Effects/Magic/1 Magic/4_1.png";
    private static readonly string[] Scenes =
    {
        "Assets/Scenes/Level01_EmberRuins.unity", "Assets/Scenes/Level02_CrystalDepths.unity",
        "Assets/Scenes/Level03_AshenForge.unity", "Assets/Scenes/Level04_ShadowCitadel.unity",
        "Assets/Scenes/Level05_EmberThrone.unity"
    };

    [MenuItem("Echoes of Ember/Install CraftPix Fireball Projectile")]
    public static void InstallAfterCompile()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (frames.Length == 0) { Debug.LogWarning("Echoes: CraftPix fireball sheet is not ready yet."); return; }

        string active = SceneManager.GetActiveScene().path;
        ConfigurePrefab(frames);
        foreach (string path in Scenes)
        {
            if (!System.IO.File.Exists(path)) continue;
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            PlayerMagic[] casters = Object.FindObjectsByType<PlayerMagic>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (PlayerMagic caster in casters) { caster.Configure(frames); EditorUtility.SetDirty(caster); }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(active) && System.IO.File.Exists(active)) EditorSceneManager.OpenScene(active, OpenSceneMode.Single);
        Debug.Log("Echoes: installed real CraftPix fireball frames (4_1) for all campaign levels.");
    }

    private static void ConfigurePrefab(Sprite[] frames)
    {
        const string prefabPath = "Assets/Prefabs/Characters/Player_Kael.prefab";
        if (!System.IO.File.Exists(prefabPath)) return;
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            PlayerMagic magic = root.GetComponentInChildren<PlayerMagic>(true);
            if (magic == null) magic = root.AddComponent<PlayerMagic>();
            magic.Configure(frames);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
