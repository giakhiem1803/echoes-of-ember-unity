using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Non-destructive production pass.  It extends the five existing campaign
/// scenes instead of rebuilding their tested core, and consumes the CraftPix
/// rocky/ruins/dungeon packs as side-view decoration and gameplay landmarks.
/// Safe to run repeatedly.
/// </summary>
[InitializeOnLoad]
public static class UltimateEchoesUpgradeInstaller
{
    private const string Revision = "Echoes.UltimateExpansion.2026.08.26.r7";
    private const string RootName = "ULTIMATE CRAFTPIX EXPANSION";
    private static readonly string[] Levels =
    {
        "Assets/Scenes/Level01_EmberRuins.unity",
        "Assets/Scenes/Level02_CrystalDepths.unity",
        "Assets/Scenes/Level03_AshenForge.unity",
        "Assets/Scenes/Level04_ShadowCitadel.unity",
        "Assets/Scenes/Level05_EmberThrone.unity"
    };

    // Each pair is centre X / width. Adjacent pieces touch or overlap, so the
    // mandatory lower route never contains an invisible or impossible gap.
    // Challenge is supplied by enemies, traps and the optional upper route.
    private static readonly Vector2[] MainPlatforms =
    {
        new Vector2(47f, 14f), new Vector2(60f, 12f), new Vector2(72f, 12f),
        new Vector2(84f, 12f), new Vector2(96f, 12f), new Vector2(108f, 12f),
        new Vector2(119f, 10f)
    };

    static UltimateEchoesUpgradeInstaller()
    {
        EditorApplication.update -= AutoRun;
        EditorApplication.update += AutoRun;
    }

    [MenuItem("Echoes of Ember/ULTIMATE UPGRADE - Maps UI Systems", priority = -50)]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string active = SceneManager.GetActiveScene().path;
        try
        {
            ConfigureImportedArt();
            int completed = 0;
            for (int i = 0; i < Levels.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Levels[i]) == null) continue;
                EditorUtility.DisplayProgressBar("Echoes of Ember", $"Expanding stage {i + 1}/5", (i + 1f) / Levels.Length);
                Scene scene = EditorSceneManager.OpenScene(Levels[i], OpenSceneMode.Single);
                if (!ExpandScene(scene, i + 1))
                    throw new InvalidOperationException($"Could not find Kael in {Levels[i]}; expansion was not marked complete.");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                completed++;
            }
            if (completed != Levels.Length) throw new InvalidOperationException($"Only {completed}/{Levels.Length} campaign scenes were expanded.");
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            EditorPrefs.SetBool(Revision, true);
            Debug.Log("Echoes of Ember: ultimate 5-stage expansion installed. Restart UI, checkpoints, long routes and CraftPix landmarks are ready.");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (!string.IsNullOrEmpty(active) && AssetDatabase.LoadAssetAtPath<SceneAsset>(active) != null)
                EditorSceneManager.OpenScene(active, OpenSceneMode.Single);
        }
    }

    private static void AutoRun()
    {
        if (EditorPrefs.GetBool(Revision, false)) { EditorApplication.update -= AutoRun; return; }
        if (EditorApplication.isPlaying)
        {
            // Scene assets cannot be rebuilt safely while Play Mode owns them.
            // Stop once, then this update callback will continue installation.
            EditorApplication.isPlaying = false;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try
        {
            Install();
            if (EditorPrefs.GetBool(Revision, false)) EditorApplication.update -= AutoRun;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Echoes of Ember expansion will retry: " + exception.Message);
        }
    }

    private static void ConfigureImportedArt()
    {
        string[] roots =
        {
            "Assets/Art/Props/RockyObjects",
            "Assets/Art/Props/TopDownRuins",
            "Assets/Art/Props/TopDownDungeon"
        };
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", roots))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("__MACOSX") || path.EndsWith("COUPON.png", StringComparison.OrdinalIgnoreCase)) continue;
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
            bool changed = importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point || importer.textureCompression != TextureImporterCompression.Uncompressed;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 64f;
            if (changed) importer.SaveAndReimport();
        }
    }

    private static bool ExpandScene(Scene scene, int stage)
    {
        RemoveMissingScripts(scene);
        PlayerController player = FindInScene<PlayerController>(scene);
        if (player == null) return false;
        GameObject old = FindRoot(scene, RootName);
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        GameObject root = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        float groundTop = FindGroundTop(player.transform.position);
        if (float.IsNegativeInfinity(groundTop)) groundTop = player.transform.position.y - 1.2f;

        Sprite groundSprite = FindReusableGroundSprite(player.transform.position);
        Sprite checkpointSprite = LoadFirstSprite("Assets/Art/Props/RockyObjects", "Cave_entrance");
        Sprite[] landmarks = LoadLandmarks(stage);
        Sprite trapSprite = LoadFirstSprite("Assets/Art/Props/TopDownDungeon", "trap_animation");

        // Guaranteed lower route. The previous revision left 0.5-1.5 unit
        // seams between colliders; depending on Kael's collider these behaved
        // like walls/holes. These pieces now overlap slightly from X=40 to 124.
        foreach (Vector2 definition in MainPlatforms)
            CreatePlatform(root.transform, definition.x, groundTop - .45f, definition.y, .9f, groundSprite, stage);

        // Optional upper route gives exploration without making completion rely
        // on precision jumps.
        float[] upperX = { 53f, 62f, 70f, 84f, 92f, 107f };
        for (int i = 0; i < upperX.Length; i++)
            CreatePlatform(root.transform, upperX[i], groundTop + 2.1f + (i % 2) * .65f, 3.4f, .45f, groundSprite, stage);

        CreateCheckpoint(root.transform, new Vector3(61f, groundTop + .85f, 0f), checkpointSprite);

        // Three deliberate hazards, all with a safe visual telegraph and a
        // normal route around them.
        float[] traps = { 68.5f, 90.5f, 109.5f };
        foreach (float x in traps) CreateTrap(root.transform, new Vector3(x, groundTop + .18f, 0f), trapSprite, stage);

        // Decorative storytelling landmarks from every newly supplied pack.
        float[] decorationX = { 49f, 56f, 64f, 72f, 81f, 89f, 98f, 106f, 114f, 121f };
        for (int i = 0; i < decorationX.Length; i++)
        {
            Sprite sprite = landmarks.Length == 0 ? null : landmarks[i % landmarks.Length];
            if (sprite == null) continue;
            CreateDecoration(root.transform, sprite, new Vector3(decorationX[i], groundTop + .12f, 1f), .7f + (i % 3) * .15f, i % 2 == 0 ? 1 : -1);
        }

        CloneGameplayContent(root.transform, stage, groundTop);
        RelocateGoal(root.transform, groundTop, stage);

        CameraFollow2D follow = FindInScene<CameraFollow2D>(scene);
        if (follow != null)
        {
            SerializedObject so = new SerializedObject(follow);
            so.FindProperty("maxX").floatValue = 116f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Start every stage on solid ground even when an older builder saved a
        // stale Y coordinate.
        if (player.transform.position.y < groundTop - .2f)
            player.transform.position = new Vector3(player.transform.position.x, groundTop + 1.1f, player.transform.position.z);
        return true;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            T found = sceneRoot.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            if (sceneRoot.name == name) return sceneRoot;
        return null;
    }

    private static void RemoveMissingScripts(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
        }
    }

    private static float FindGroundTop(Vector3 playerPosition)
    {
        float best = float.NegativeInfinity;
        foreach (Collider2D col in UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (col.isTrigger || col.GetComponentInParent<PlayerController>() != null) continue;
            Bounds b = col.bounds;
            if (b.min.x <= playerPosition.x && b.max.x >= playerPosition.x && b.max.y <= playerPosition.y + .5f)
                best = Mathf.Max(best, b.max.y);
        }
        return best;
    }

    private static Sprite FindReusableGroundSprite(Vector3 playerPosition)
    {
        SpriteRenderer best = null;
        float distance = float.MaxValue;
        foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (renderer.sprite == null || renderer.GetComponentInParent<PlayerController>() != null) continue;
            if (renderer.GetComponent<Collider2D>() == null) continue;
            float d = Mathf.Abs(renderer.transform.position.x - playerPosition.x);
            if (d < distance) { best = renderer; distance = d; }
        }
        return best != null ? best.sprite : null;
    }

    private static void CreatePlatform(Transform parent, float x, float y, float width, float height, Sprite sprite, int stage)
    {
        GameObject go = new GameObject($"Route Platform {x:0}", typeof(SpriteRenderer), typeof(BoxCollider2D));
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, y, 0f);
        BoxCollider2D col = go.GetComponent<BoxCollider2D>();
        col.size = new Vector2(width, height);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        if (sprite != null)
        {
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(width, height);
        }
        else
        {
            sr.color = StageColor(stage);
            go.transform.localScale = new Vector3(width, height, 1f);
        }
    }

    private static void CreateCheckpoint(Transform parent, Vector3 position, Sprite sprite)
    {
        GameObject go = new GameObject("Checkpoint - Rocky Shrine", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Checkpoint));
        go.transform.SetParent(parent); go.transform.position = position;
        go.GetComponent<SpriteRenderer>().sprite = sprite;
        go.GetComponent<SpriteRenderer>().sortingOrder = 8;
        go.transform.localScale = Vector3.one * .75f;
        BoxCollider2D trigger = go.GetComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = new Vector2(2.2f, 2.8f);
    }

    private static void CreateTrap(Transform parent, Vector3 position, Sprite sprite, int stage)
    {
        GameObject go = new GameObject("Dungeon Spike Trap", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(CraftpixHazard));
        go.transform.SetParent(parent); go.transform.position = position;
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 9; sr.color = new Color(1f, .58f, .25f, 1f);
        go.transform.localScale = Vector3.one * .32f;
        BoxCollider2D trigger = go.GetComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = new Vector2(1.7f, .65f);
        go.GetComponent<CraftpixHazard>().Configure(stage >= 4 ? 2 : 1);
    }

    private static void CreateDecoration(Transform parent, Sprite sprite, Vector3 position, float scale, int facing)
    {
        GameObject go = new GameObject("CraftPix Landmark - " + sprite.name, typeof(SpriteRenderer));
        go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = new Vector3(scale * facing, scale, 1f);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 2; sr.color = new Color(1f, 1f, 1f, .92f);
    }

    private static void CloneGameplayContent(Transform parent, int stage, float groundTop)
    {
        Scene scene = parent.gameObject.scene;
        EnemyController enemyTemplate = FindInScene<EnemyController>(scene);
        ChestController chestTemplate = FindInScene<ChestController>(scene);
        EmberShard emberTemplate = FindInScene<EmberShard>(scene);
        HeartPickup heartTemplate = FindInScene<HeartPickup>(scene);

        float[] enemyX = { 55f, 66f, 76f, 86f, 99f, 111f };
        if (enemyTemplate != null)
        {
            for (int i = 0; i < enemyX.Length; i++)
            {
                EnemyController clone = UnityEngine.Object.Instantiate(enemyTemplate, new Vector3(enemyX[i], groundTop + .9f, 0f), Quaternion.identity, parent);
                clone.name = $"Stage {stage} Guardian {i + 1}";
                clone.Configure(1 + stage / 2, 1f + stage * .16f, 5f + stage, stage >= 4 ? 2 : 1, 2.2f);
            }
        }
        if (chestTemplate != null)
        {
            float[] chestX = { 70f, 104f };
            for (int i = 0; i < chestX.Length; i++)
            {
                ChestController chest = UnityEngine.Object.Instantiate(chestTemplate, new Vector3(chestX[i], groundTop + .75f, 0f), Quaternion.identity, parent);
                chest.name = $"Exploration Chest S{stage}-{i + 1}";
                SerializedObject so = new SerializedObject(chest);
                so.FindProperty("chestId").stringValue = $"ultimate-stage-{stage}-chest-{i + 1}";
                so.FindProperty("rewardTable").intValue = stage + i;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        if (emberTemplate != null)
        {
            for (int i = 0; i < 15; i++)
            {
                float x = 49f + i * 4.7f;
                float y = groundTop + 1.25f + (i % 3) * .72f;
                EmberShard shard = UnityEngine.Object.Instantiate(emberTemplate, new Vector3(x, y, 0f), Quaternion.identity, parent);
                shard.name = "Expansion Ember " + (i + 1);
            }
        }
        if (heartTemplate != null)
        {
            HeartPickup heart = UnityEngine.Object.Instantiate(heartTemplate, new Vector3(93f, groundTop + 1.35f, 0f), Quaternion.identity, parent);
            heart.name = "Expansion Heart";
        }
    }

    private static void RelocateGoal(Transform parent, float groundTop, int stage)
    {
        LevelGoal goal = FindInScene<LevelGoal>(parent.gameObject.scene);
        if (goal == null) return;
        goal.transform.position = new Vector3(121f, groundTop + 1.5f, 0f);
        GameObject dais = new GameObject("Final Gate Dais", typeof(SpriteRenderer), typeof(BoxCollider2D));
        dais.transform.SetParent(parent); dais.transform.position = new Vector3(121f, groundTop - .08f, 0f);
        dais.GetComponent<SpriteRenderer>().color = new Color(1f, .42f, .12f, .85f);
        dais.transform.localScale = new Vector3(4f, .25f, 1f);
        dais.GetComponent<BoxCollider2D>().size = Vector2.one;
    }

    private static Sprite[] LoadLandmarks(int stage)
    {
        var result = new List<Sprite>();
        string[] rockyNames = { "Dragon_bones_full", "Fern_tree", "Liana_bridges", "Cave_entrance", "Black_mushrooms" };
        string[] ruins = { "Blue-gray_ruins", "Brown_ruins", "Sand_ruins", "White_ruins", "Yellow_ruins" };
        foreach (string name in rockyNames) { Sprite s = LoadFirstSprite("Assets/Art/Props/RockyObjects", name); if (s != null) result.Add(s); }
        Sprite ruin = LoadFirstSprite("Assets/Art/Props/TopDownRuins", ruins[Mathf.Clamp(stage - 1, 0, ruins.Length - 1)]); if (ruin != null) result.Add(ruin);
        Sprite dungeon = LoadFirstSprite("Assets/Art/Props/TopDownDungeon", stage % 2 == 0 ? "Objects" : "doors_lever"); if (dungeon != null) result.Add(dungeon);
        return result.ToArray();
    }

    private static Sprite LoadFirstSprite(string folder, string nameContains)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("__MACOSX") || path.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) < 0) continue;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
        }
        return null;
    }

    private static Color StageColor(int stage) => stage switch
    {
        2 => new Color(.18f, .55f, .64f),
        3 => new Color(.48f, .17f, .08f),
        4 => new Color(.22f, .13f, .36f),
        5 => new Color(.38f, .28f, .08f),
        _ => new Color(.25f, .22f, .16f)
    };

    private static void EnsureBuildSettings()
    {
        string[] all = new[] { "Assets/Scenes/CampaignHub.unity", "Assets/Scenes/KaelMovementTest.unity" }.Concat(Levels).ToArray();
        EditorBuildSettings.scenes = all.Where(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null).Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
    }
}
