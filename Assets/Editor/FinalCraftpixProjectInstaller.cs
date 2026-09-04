using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The single authoritative content pass for Echoes of Ember.  It never
/// rebuilds gameplay geometry or the Canvas; it enriches the five tested maps
/// with the complete production-ready CraftPix library and is safe to rerun.
/// </summary>
[InitializeOnLoad]
public static class FinalCraftpixProjectInstaller
{
    private const string RevisionKey = "Echoes.FinalCraftpixPass.2026.08.26.r4";
    private const string RootName = "CRAFTPIX FINAL CONTENT";
    private static bool forceInstallRequested;
    private static readonly string[] Scenes =
    {
        "Assets/Scenes/Level01_EmberRuins.unity",
        "Assets/Scenes/Level02_CrystalDepths.unity",
        "Assets/Scenes/Level03_AshenForge.unity",
        "Assets/Scenes/Level04_ShadowCitadel.unity",
        "Assets/Scenes/Level05_EmberThrone.unity"
    };

    private static readonly string[][] EnemySheets =
    {
        new[] { "Assets/Art/Enemies/FantasyEnemies/Skeleton/Walk.png", "Assets/Art/Enemies/Skeletons/Skeleton_Warrior/Walk.png" },
        new[] { "Assets/Art/Enemies/Ghosts/Onre/Walk.png", "Assets/Art/Enemies/Ghosts/Yurei/Walk.png" },
        new[] { "Assets/Art/Enemies/FantasyEnemies/Fire_Spirit/Walk.png", "Assets/Art/Enemies/FantasyEnemies/Plent/Walk.png" },
        new[] { "Assets/Art/Enemies/Skeletons/Skeleton_Archer/Walk.png", "Assets/Art/Enemies/Skeletons/Skeleton_Spearman/Walk.png", "Assets/Art/Enemies/Ghosts/Gotoku/Walk.png" },
        new[] { "Assets/Art/Enemies/FantasyEnemies/Skeleton/Run.png", "Assets/Art/Enemies/Ghosts/Yurei/Run.png", "Assets/Art/Enemies/FantasyEnemies/Fire_Spirit/Run.png" }
    };

    private static readonly string[] Backgrounds =
    {
        "Assets/Art/Backgrounds/Battlegrounds/Battleground1/Pale/Battleground1.png",
        "Assets/Art/Backgrounds/CrystalCave/background 2/background 2.png",
        "Assets/Art/Backgrounds/Battlegrounds/Battleground2/Pale/Battleground2.png",
        "Assets/Art/Backgrounds/Battlegrounds/Battleground3/Pale/Battleground3.png",
        "Assets/Art/Backgrounds/Battlegrounds/Battleground2/Bright/Battleground2.png"
    };

    private static readonly string[] DecorationSheets =
    {
        "Assets/Art/Props/DungeonObjects/PNG/supplies_objects.png",
        "Assets/Art/Props/DungeonObjects/PNG/pedestals.png",
        "Assets/Art/Props/DungeonObjects/PNG/Other_objects.png",
        "Assets/Art/Props/DungeonObjects/PNG/trap_plate.png",
        "Assets/Art/Props/DungeonObjects/PNG/trap_saw.png",
        "Assets/Art/Props/DungeonObjects/PNG/fire_trap.png"
    };

    static FinalCraftpixProjectInstaller()
    {
        // Keep checking until Unity is genuinely back in Edit Mode. A one-shot
        // delayCall can be consumed while Play Mode is still shutting down.
        EditorApplication.update -= AutoInstallWhenReady;
        EditorApplication.update += AutoInstallWhenReady;
    }

    [MenuItem("Echoes of Ember/FINAL INSTALL - Complete CraftPix Project", priority = 0)]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        string active = SceneManager.GetActiveScene().path;
        int completed = 0;
        try
        {
            for (int i = 0; i < Scenes.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Scenes[i]) == null) continue;
                EditorUtility.DisplayProgressBar("Echoes of Ember - Final CraftPix Pass", $"Polishing level {i + 1}/5", (i + 1) / 5f);
                Scene scene = EditorSceneManager.OpenScene(Scenes[i], OpenSceneMode.Single);
                RemoveMissingScripts(scene);
                InstallSceneContent(i + 1);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                completed++;
            }
            WriteUsageMatrix();
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorPrefs.SetBool(RevisionKey, true);
            Debug.Log($"Echoes of Ember: final CraftPix content pass completed for {completed}/5 levels.");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (!string.IsNullOrEmpty(active) && AssetDatabase.LoadAssetAtPath<SceneAsset>(active) != null)
                EditorSceneManager.OpenScene(active, OpenSceneMode.Single);
        }
    }

    [MenuItem("Echoes of Ember/FORCE FINAL INSTALL %#i", priority = 1)]
    public static void ForceInstall()
    {
        forceInstallRequested = true;
        EditorApplication.update -= ForceInstallWhenReady;
        EditorApplication.update += ForceInstallWhenReady;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.ExitPlaymode();
    }

    private static void ForceInstallWhenReady()
    {
        if (!forceInstallRequested || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        forceInstallRequested = false;
        EditorApplication.update -= ForceInstallWhenReady;
        Install();
    }

    private static void AutoInstallWhenReady()
    {
        if (EditorPrefs.GetBool(RevisionKey, false))
        {
            EditorApplication.update -= AutoInstallWhenReady;
            return;
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= AutoInstallWhenReady;
        Install();
    }

    private static void InstallSceneContent(int level)
    {
        GameObject old = GameObject.Find(RootName);
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        GameObject root = new GameObject(RootName);
        root.AddComponent<EchoesAudioManager>();
        root.transform.position = Vector3.zero;

        InstallBackdrop(root.transform, level);
        InstallDecorations(root.transform, level);
        InstallEnemyVariety(level);
        EnsureCameraAudioListener();
    }

    private static void InstallBackdrop(Transform parent, int level)
    {
        Sprite sprite = LoadFirstSprite(Backgrounds[level - 1]);
        if (sprite == null) return;
        GameObject go = new GameObject($"CraftPix Atmosphere L{level}", typeof(SpriteRenderer));
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(18f, 0f, 8f);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -100;
        float width = Mathf.Max(.01f, sr.bounds.size.x);
        float height = Mathf.Max(.01f, sr.bounds.size.y);
        go.transform.localScale = new Vector3(50f / width, 13f / height, 1f);
        sr.color = new Color(.7f, .7f, .76f, .34f);
    }

    private static void InstallDecorations(Transform parent, int level)
    {
        // Non-blocking scenery: every dungeon-object production sheet appears
        // across the campaign, but never creates another impossible jump.
        for (int i = 0; i < DecorationSheets.Length; i++)
        {
            Sprite[] sprites = LoadFrames(DecorationSheets[i]);
            if (sprites.Length == 0) continue;
            Sprite sprite = sprites[(level + i) % sprites.Length];
            GameObject go = new GameObject($"Prop {level}-{i + 1} {Path.GetFileNameWithoutExtension(DecorationSheets[i])}", typeof(SpriteRenderer));
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(3.5f + i * 6.1f, -2.52f + ((i + level) % 2) * .2f, 0f);
            go.transform.localScale = Vector3.one * Mathf.Lerp(.35f, .65f, (i % 3) / 2f);
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 4;
            sr.color = new Color(1f, 1f, 1f, .88f);
        }
    }

    private static void InstallEnemyVariety(int level)
    {
        EnemyController[] enemies = UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        string[] sheets = EnemySheets[level - 1];
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyController enemy = enemies[i];
            bool boss = level == 5 && i == enemies.Length - 1;
            int hp = boss ? 8 : 1 + (level - 1) / 2;
            enemy.Configure(hp, 1.05f + level * .12f + (i % 2) * .12f, 4f + level * .35f, boss ? 2 : 1, 1.5f + (i % 3) * .35f);
            CraftpixSpriteAnimator animator = enemy.GetComponent<CraftpixSpriteAnimator>();
            if (animator == null) animator = enemy.gameObject.AddComponent<CraftpixSpriteAnimator>();
            animator.Configure(LoadFrames(sheets[i % sheets.Length]), 7f + level * .7f);
            bool alreadyBoss = enemy.gameObject.name == "EMBER THRONE BOSS";
            enemy.gameObject.name = boss ? "EMBER THRONE BOSS" : $"L{level} CraftPix Enemy {i + 1}";
            if (boss && !alreadyBoss) enemy.transform.localScale *= 1.45f;
            EditorUtility.SetDirty(enemy);
        }
    }

    private static Sprite[] LoadFrames(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(NaturalOrder).ToArray();
    private static Sprite LoadFirstSprite(string path)
    {
        Sprite[] sprites = LoadFrames(path);
        return sprites.Length > 0 ? sprites[0] : AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static int NaturalOrder(Sprite sprite)
    {
        string digits = new string(sprite.name.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
        return int.TryParse(digits, out int value) ? value : 0;
    }

    private static void RemoveMissingScripts(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
    }

    private static void EnsureCameraAudioListener()
    {
        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (camera != null && camera.GetComponent<AudioListener>() == null) camera.gameObject.AddComponent<AudioListener>();
        AudioListener[] listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 1; i < listeners.Length; i++) listeners[i].enabled = false;
    }

    private static void EnsureBuildSettings()
    {
        string[] all = new[] { "Assets/Scenes/CampaignHub.unity", "Assets/Scenes/KaelMovementTest.unity" }.Concat(Scenes).ToArray();
        EditorBuildSettings.scenes = all.Where(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
            .Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
    }

    private static void WriteUsageMatrix()
    {
        const string folder = "Assets/Documentation";
        Directory.CreateDirectory(folder);
        File.WriteAllText(folder + "/CraftPix_Asset_Usage_Matrix.md",
@"# Echoes of Ember - CraftPix Asset Usage Matrix

This project uses every production-ready CraftPix pack supplied for the final assignment. Source duplicates (PSD/Tiled copies), coupons and store-preview images are deliberately excluded from runtime builds.

| CraftPix pack | Integrated role |
|---|---|
| Free Knight Character Sprites | Kael player; idle, walk, run, jump, three attacks, defend, hurt and death |
| Dungeon Platformer Tileset | Level 01 geometry, bridges, lava, door and chest visuals |
| Medieval Tileset | Campaign platforms and architectural variation |
| Crystal Cave Backgrounds | Level 02 Crystal Depths atmosphere |
| Fantasy 2D Battlegrounds | Unique layered atmosphere for Levels 01, 03, 04 and 05 |
| Skeleton Sprite Sheets | Warrior, archer and spearman enemy variants |
| Ghost Sprite Sheets | Onre, Yurei and Gotoku enemy variants |
| Fantasy Enemies | Skeleton, Plent and Fire Spirit variants; Ember Throne boss mix |
| Pixel Magic Effects | Animated visible Fireball, cast spark, trail and impact |
| Basic Pixel RPG UI | HUD, hotbar, modal frames, buttons, inventory and equipment presentation |
| Animated Magic Book | Spell Book and Quest Book presentation |
| 40 Loot Icons | Chests, Ember, equipment, potion and relic rewards |
| RPG Skill/Splash Icons | Skill and status presentation only, never used as projectiles |
| Dungeon Objects | Supplies, pedestals, traps and non-blocking environmental storytelling across all levels |

## Campaign progression
Level 01 Ember Ruins -> Level 02 Crystal Depths -> Level 03 Ashen Forge -> Level 04 Shadow Citadel -> Level 05 Ember Throne. Enemy health, speed and detection increase by stage; the final enemy is an 8 HP boss.

## Runtime exclusions
`COUPON.png`, previews, PSD duplicates and Tiled-source duplicates are documentation/source material and are not loaded at runtime. This prevents duplicated textures and unnecessary build size while retaining all actual game art.
");
    }
}
