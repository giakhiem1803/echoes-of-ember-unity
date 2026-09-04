#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the four post-Ember-Ruins campaign scenes from the CraftPix packs.
/// The layout deliberately keeps a continuous lower path: platforming is a
/// choice and combat challenge, never an unavoidable jump-gap.
/// </summary>
public static class CampaignSceneBuilder
{
    private const string Scenes = "Assets/Scenes/";
    private const string PlayerPrefab = "Assets/Prefabs/Characters/Player_Kael.prefab";
    private const string Dungeon = "Assets/Art/Tilesets/Dungeon/";
    private const string Crystal = "Assets/Art/Backgrounds/CrystalCave/";
    private const string Battle = "Assets/Art/Backgrounds/Battlegrounds/";
    private const string Medieval = "Assets/Art/Tilesets/Medieval/";
    private const string Enemies = "Assets/Art/Enemies/";
    private const float Tile = .64f;

    [MenuItem("Echoes of Ember/Build Campaign Levels 02-05", priority = 41)]
    public static void BuildCampaign()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab) == null)
        {
            EditorUtility.DisplayDialog("Echoes of Ember", "Player_Kael.prefab is missing. Run Setup Kael first.", "OK");
            return;
        }
        BuildLevel(2, "Level02_CrystalDepths", "CRYSTAL DEPTHS", "Find the Moon Shard beyond the crystal caverns.", new Color(.20f, .85f, 1f), 4);
        BuildLevel(3, "Level03_AshenForge", "ASHEN FORGE", "Survive the forge and restore the Ember Sigil.", new Color(1f, .34f, .12f), 5);
        BuildLevel(4, "Level04_ShadowCitadel", "SHADOW CITADEL", "Break the siege and open the citadel gate.", new Color(.72f, .38f, 1f), 6);
        BuildLevel(5, "Level05_EmberThrone", "EMBER THRONE", "Defeat the throne guardians and reclaim the flame.", new Color(1f, .72f, .12f), 7);
        BuildCampaignHub();
        UpdateBuildSettings();
        // The campaign builder owns the gameplay layout only. Always finish by
        // installing the canonical runtime menu system so legacy MenuAction
        // buttons can never overwrite Restart / Campaign / Next Level wiring.
        CompleteRpgMenuInstaller.Install();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Echoes of Ember", "Campaign levels 02-05 and Campaign Hub were created. Add them to Build Settings before testing.", "OK");
    }

    [MenuItem("Echoes of Ember/Repair Campaign Hub Runtime Buttons", priority = 42)]
    public static void RebuildCampaignHubOnly()
    {
        BuildCampaignHub();
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Echoes of Ember: Campaign Hub rebuilt with runtime-safe scene buttons.");
    }

    private static void BuildLevel(int index, string sceneName, string title, string objective, Color accent, int encounterCount)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = sceneName;
        Camera camera = MakeCamera();
        Transform world = new GameObject("WORLD - " + title).transform;
        BuildBackdrop(world, index);
        Sprite[] floor = FloorTiles(index);
        BuildFairRoute(world, floor, index);
        GameObject kael = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab));
        kael.name = "Kael Emberbound"; kael.transform.SetParent(world); kael.transform.position = new Vector3(-9.4f, -2.97f); kael.transform.localScale = Vector3.one * 2.25f;
        camera.gameObject.AddComponent<CameraFollow2D>().SetTarget(kael.transform);
        BuildEncounters(world, index, encounterCount);
        BuildGate(world, index, floor);
        BuildDeathSafety(world);
        BuildHud(kael.GetComponent<PlayerHealth>(), title, objective, accent, index);
        EditorSceneManager.SaveScene(scene, Scenes + sceneName + ".unity");
    }

    private static Camera MakeCamera()
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera"; cameraObject.transform.position = new Vector3(-4.5f, 0f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 4.5f; camera.backgroundColor = new Color(.018f, .025f, .055f);
        var light = new GameObject("Global Light 2D", typeof(Light2D)); light.GetComponent<Light2D>().lightType = Light2D.LightType.Global; light.GetComponent<Light2D>().intensity = 1.1f;
        return camera;
    }

    private static void BuildBackdrop(Transform parent, int index)
    {
        string[] paths;
        switch (index)
        {
            case 2: paths = new[] { Crystal + "background 2/background 2.png", Crystal + "background 2/Plan 1.png", Crystal + "background 2/Plan 2.png", Crystal + "background 2/Plan 3.png", Crystal + "background 2/Plan 4.png" }; break;
            case 3: paths = new[] { Battle + "Battleground2/Pale/bg.png", Battle + "Battleground2/Pale/mountaims.png", Battle + "Battleground2/Pale/wall@windows.png", Battle + "Battleground2/Pale/columns&falgs.png", Battle + "Battleground2/Pale/floor.png" }; break;
            case 4: paths = new[] { Battle + "Battleground4/Pale/sky.png", Battle + "Battleground4/Pale/back_trees.png", Battle + "Battleground4/Pale/crypt.png", Battle + "Battleground4/Pale/graves.png", Battle + "Battleground4/Pale/ground.png" }; break;
            default: paths = new[] { Battle + "Battleground1/Bright/sky.png", Battle + "Battleground1/Bright/hills&trees.png", Battle + "Battleground1/Bright/ruins_bg.png", Battle + "Battleground1/Bright/ruins.png", Battle + "Battleground1/Bright/stones&grass.png" }; break;
        }
        for (int repeat = 0; repeat < 4; repeat++)
        {
            float x = -10f + repeat * 20f;
            for (int i = 0; i < paths.Length; i++)
            {
                Sprite sprite = SpriteAt(paths[i]); if (sprite == null) continue;
                MakeSprite(parent, "Backdrop " + repeat + " Layer " + i, sprite, new Vector3(x, -.15f), Vector3.one * 2.05f, Color.white, -40 + i);
            }
        }
    }

    private static Sprite[] FloorTiles(int index)
    {
        if (index == 4)
        {
            Sprite[] medieval = { SpriteAt(Medieval + "Tiles/tile1.png"), SpriteAt(Medieval + "Tiles/tile10.png"), SpriteAt(Medieval + "Tiles/tile20.png"), SpriteAt(Medieval + "Tiles/tile30.png") };
            if (medieval.All(sprite => sprite != null)) return medieval;
        }
        return new[] { SpriteAt(Dungeon + "PNG/Tiles_rock/tile1.png"), SpriteAt(Dungeon + "PNG/Tiles_rock/tile2.png"), SpriteAt(Dungeon + "PNG/Tiles_rock/tile3.png"), SpriteAt(Dungeon + "PNG/Tiles_rock/tile4.png") };
    }

    private static void BuildFairRoute(Transform parent, Sprite[] tiles, int index)
    {
        // Full continuous base route from -11 to +42. Raised stones are optional reward routes.
        BuildPlatform(parent, "Main Expedition Route", 15.5f, -3.65f, 55f, 1.35f, tiles, 0);
        float[] ledges = { -5f, 3f, 11.5f, 20f, 29f, 37f };
        for (int i = 0; i < ledges.Length; i++)
            BuildPlatform(parent, "Reward Ledge " + i, ledges[i], -1.72f - (i % 2) * .25f, 2.6f, .34f, tiles, 2);
        if (index == 4)
        {
            Sprite barrel = SpriteAt(Medieval + "Objects/barrel.png");
            Sprite torch = SpriteAt(Medieval + "Objects/torch.png");
            Sprite chain = SpriteAt(Medieval + "Objects/chain1.png");
            for (int i = 0; i < 12; i++)
            {
                float x = -8f + i * 4.1f;
                MakeSprite(parent, "Citadel Torch " + i, torch, new Vector3(x, -2.2f), Vector3.one, Color.white, 5);
                MakeSprite(parent, "Citadel Barrel " + i, barrel, new Vector3(x + .65f, -2.75f), Vector3.one * .7f, Color.white, 4);
                MakeSprite(parent, "Citadel Chain " + i, chain, new Vector3(x + 1.3f, 3.2f), Vector3.one, Color.white, -1);
            }
        }
    }

    private static void BuildPlatform(Transform parent, string name, float centerX, float y, float width, float colliderHeight, Sprite[] tiles, int order)
    {
        var physical = new GameObject(name + " Collider", typeof(BoxCollider2D)); physical.transform.SetParent(parent); physical.transform.position = new Vector2(centerX, y); physical.GetComponent<BoxCollider2D>().size = new Vector2(width, colliderHeight);
        int count = Mathf.CeilToInt(width / Tile); float start = centerX - width * .5f + Tile * .5f;
        for (int i = 0; i < count; i++)
        {
            Sprite sprite = tiles[i % tiles.Length]; if (sprite == null) continue;
            MakeSprite(parent, name + " Stone " + i, sprite, new Vector3(start + i * Tile, y + colliderHeight * .34f), Vector3.one, Color.white, order);
            if (colliderHeight > .5f) MakeSprite(parent, name + " Fill " + i, tiles[(i + 2) % tiles.Length], new Vector3(start + i * Tile, y - colliderHeight * .18f), Vector3.one, Color.white, order - 1);
        }
    }

    private static void BuildEncounters(Transform parent, int level, int count)
    {
        Sprite ember = SpriteAt(Dungeon + "PNG/Items/000_0045_coin.png");
        Sprite heart = SpriteAt(Dungeon + "PNG/Items/000_0065_heart.png");
        for (int i = 0; i < 18 + level * 3; i++) MakePickup(parent, ember, new Vector2(-8.5f + i * 1.55f, i % 4 == 0 ? -.95f : -2.15f), false);
        MakePickup(parent, heart, new Vector2(13.2f, -2.1f), true); MakePickup(parent, heart, new Vector2(31.4f, -2.1f), true);
        string enemyPath = level == 2 ? Enemies + "Ghosts/Yurei/Idle.png" : level == 3 ? Enemies + "FantasyEnemies/Fire_Spirit/Idle.png" : level == 4 ? Enemies + "Skeletons/Skeleton_Spearman/Idle.png" : Enemies + "Skeletons/Skeleton_Warrior/Idle.png";
        SliceEnemy(enemyPath);
        Sprite enemy = AssetDatabase.LoadAllAssetsAtPath(enemyPath).OfType<Sprite>().FirstOrDefault();
        for (int i = 0; i < count; i++) MakeEnemy(parent, "Guardian " + (i + 1), enemy, new Vector2(-2f + i * 6.8f, -2.72f), 1.05f + level * .1f);
    }

    private static void MakePickup(Transform parent, Sprite sprite, Vector2 position, bool heart)
    {
        if (sprite == null) return;
        Type type = heart ? typeof(HeartPickup) : typeof(EmberShard);
        var item = new GameObject(heart ? "Heart Flask" : "Ember Shard", typeof(SpriteRenderer), typeof(CircleCollider2D), type); item.transform.SetParent(parent); item.transform.position = position; item.transform.localScale = Vector3.one * (heart ? 1.05f : .95f); item.GetComponent<SpriteRenderer>().sprite = sprite; item.GetComponent<SpriteRenderer>().sortingOrder = 7; item.GetComponent<CircleCollider2D>().isTrigger = true;
    }

    private static void MakeEnemy(Transform parent, string name, Sprite sprite, Vector2 position, float speed)
    {
        if (sprite == null) return;
        var item = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(EnemyController)); item.transform.SetParent(parent); item.transform.position = position; item.transform.localScale = Vector3.one * 1.05f; item.GetComponent<SpriteRenderer>().sprite = sprite; item.GetComponent<SpriteRenderer>().sortingOrder = 6;
        BoxCollider2D box = item.GetComponent<BoxCollider2D>(); box.isTrigger = true; box.size = new Vector2(.6f, .9f); box.offset = new Vector2(0, .05f); Rigidbody2D body = item.GetComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true;
        SerializedObject so = new SerializedObject(item.GetComponent<EnemyController>()); so.FindProperty("moveSpeed").floatValue = speed; so.FindProperty("patrolDistance").floatValue = 1.5f; so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildGate(Transform parent, int index, Sprite[] tiles)
    {
        const float gateX = 40f;
        BuildPlatform(parent, "Gate Dais", gateX, -3.65f, 6.5f, 1.35f, tiles, 1);
        Sprite door = SpriteAt(index == 4 ? Medieval + "Objects/door3.png" : Dungeon + "PNG/Details/door.png");
        var gate = new GameObject("Level Gate - Press E", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(LevelGoal)); gate.transform.SetParent(parent); gate.transform.position = new Vector2(gateX, -2.26f); gate.transform.localScale = Vector3.one * 1.55f; gate.GetComponent<SpriteRenderer>().sprite = door; gate.GetComponent<SpriteRenderer>().sortingOrder = 8; BoxCollider2D trigger = gate.GetComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = new Vector2(1.25f, 1.65f);
        Sprite torch = SpriteAt(index == 4 ? Medieval + "Objects/torch.png" : Dungeon + "PNG/Details/torch2_1.png");
        MakeSprite(parent, "Gate Torch L", torch, new Vector3(gateX - 2.05f, -2.2f), Vector3.one * 1.1f, Color.white, 8); MakeSprite(parent, "Gate Torch R", torch, new Vector3(gateX + 2.05f, -2.2f), Vector3.one * 1.1f, Color.white, 8);
    }

    private static void BuildDeathSafety(Transform parent)
    {
        var zone = new GameObject("Fall Safety", typeof(BoxCollider2D), typeof(DeathZone)); zone.transform.SetParent(parent); zone.transform.position = new Vector2(15f, -7.2f); zone.GetComponent<BoxCollider2D>().size = new Vector2(70f, 1f); zone.GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private static void BuildHud(PlayerHealth hero, string title, string objective, Color accent, int level)
    {
        var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        TextMeshProUGUI hp = Label(canvasObject.transform, "HP", "HP  5/5", new Vector2(32, -28), new Vector2(0, 1), 28, new Color(1f, .46f, .36f)); TextMeshProUGUI score = Label(canvasObject.transform, "Score", "EMBER  0", new Vector2(-32, -28), new Vector2(1, 1), 28, accent, TextAlignmentOptions.Right); TextMeshProUGUI kills = Label(canvasObject.transform, "Kills", "KILLS  0", new Vector2(-32, -66), new Vector2(1, 1), 18, Color.white, TextAlignmentOptions.Right); TextMeshProUGUI message = Label(canvasObject.transform, "Message", "", new Vector2(0, 115), new Vector2(.5f, 0), 25, Color.white, TextAlignmentOptions.Center);
        Label(canvasObject.transform, "Stage", $"{title}  •  Stage {level}/5", new Vector2(0, -28), new Vector2(.5f, 1), 20, accent, TextAlignmentOptions.Center); Label(canvasObject.transform, "Objective", objective + "  •  A/D Move  •  Space Jump  •  J Attack  •  K Defend", new Vector2(0, 26), new Vector2(.5f, 0), 16, Color.white, TextAlignmentOptions.Center);
        GameObject pause = Panel(canvasObject.transform, "PAUSED", "Resume", MenuAction.Resume, "Restart", MenuAction.Restart, accent); GameObject over = Panel(canvasObject.transform, "KAEL HAS FALLEN", "Restart", MenuAction.Restart, "Campaign", MenuAction.CampaignHub, accent); GameObject win = Panel(canvasObject.transform, level < 5 ? "GATE RESTORED" : "EMBER THRONE RECLAIMED", level < 5 ? "Next Stage" : "Campaign Hub", level < 5 ? MenuAction.NextLevel : MenuAction.CampaignHub, "Restart", MenuAction.Restart, accent);
        var manager = new GameObject("GameManager", typeof(GameManager)); manager.GetComponent<GameManager>().Configure(hero, hp, score, kills, message, pause, over, win);
    }

    private static void BuildCampaignHub()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); scene.name = "CampaignHub"; MakeCamera().transform.position = new Vector3(0, 0, -10);
        Sprite bg = SpriteAt(Battle + "Battleground1/Bright/Battleground1.png"); MakeSprite(null, "Campaign Background", bg, Vector3.zero, Vector3.one * 2.1f, Color.white, -10);
        var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720); new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Label(canvasObject.transform, "Title", "ECHOES OF EMBER", new Vector2(0, 180), new Vector2(.5f, .5f), 56, new Color(1f, .69f, .18f), TextAlignmentOptions.Center); Label(canvasObject.transform, "SubTitle", "Choose an expedition. Complete a stage to unlock the next Ember path.", new Vector2(0, 120), new Vector2(.5f, .5f), 20, Color.white, TextAlignmentOptions.Center);
        string[] names = { "01  EMBER RUINS", "02  CRYSTAL DEPTHS", "03  ASHEN FORGE", "04  SHADOW CITADEL", "05  EMBER THRONE" }; string[] scenes = { "Level01_EmberRuins", "Level02_CrystalDepths", "Level03_AshenForge", "Level04_ShadowCitadel", "Level05_EmberThrone" };
        for (int i = 0; i < names.Length; i++) CreateHubButton(canvasObject.transform, names[i], scenes[i], new Vector2(0, 55 - i * 62));
        EditorSceneManager.SaveScene(scene, Scenes + "CampaignHub.unity");
    }

    private static void UpdateBuildSettings()
    {
        string[] paths =
        {
            Scenes + "CampaignHub.unity",
            Scenes + "KaelMovementTest.unity",
            Scenes + "Level01_EmberRuins.unity",
            Scenes + "Level02_CrystalDepths.unity",
            Scenes + "Level03_AshenForge.unity",
            Scenes + "Level04_ShadowCitadel.unity",
            Scenes + "Level05_EmberThrone.unity"
        };
        EditorBuildSettings.scenes = paths.Where(System.IO.File.Exists).Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
    }

    private static GameObject Panel(Transform parent, string title, string first, MenuAction firstAction, string second, MenuAction secondAction, Color accent)
    {
        var panel = new GameObject(title + " Panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(parent, false); RectTransform rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(600, 290); panel.GetComponent<Image>().color = new Color(.018f, .04f, .10f, .94f); Label(panel.transform, "Title", title, new Vector2(0, 72), new Vector2(.5f, .5f), 34, accent, TextAlignmentOptions.Center); Button(panel.transform, first, new Vector2(-140, -45), firstAction, accent); Button(panel.transform, second, new Vector2(140, -45), secondAction, accent); return panel;
    }
    private static void CreateHubButton(Transform parent, string text, string scene, Vector2 position)
    {
        var item = new GameObject(text + " Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CampaignSceneButton)); item.transform.SetParent(parent, false); RectTransform rect = item.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(410, 48); item.GetComponent<Image>().color = new Color(.08f, .2f, .38f, .94f); CampaignSceneButton action = item.GetComponent<CampaignSceneButton>(); action.Configure(scene); UnityEventTools.AddPersistentListener(item.GetComponent<Button>().onClick, action.Load); Label(item.transform, "Label", text, Vector2.zero, new Vector2(.5f, .5f), 20, Color.white, TextAlignmentOptions.Center);
    }
    private static void Button(Transform parent, string text, Vector2 position, MenuAction action, Color color)
    {
        var item = new GameObject(text + " Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuActionButton)); item.transform.SetParent(parent, false); RectTransform rect = item.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(235, 58); item.GetComponent<Image>().color = color; MenuActionButton handler = item.GetComponent<MenuActionButton>(); handler.SetAction(action); UnityEventTools.AddPersistentListener(item.GetComponent<Button>().onClick, handler.InvokeAction); Label(item.transform, "Label", text, Vector2.zero, new Vector2(.5f, .5f), 20, Color.white, TextAlignmentOptions.Center);
    }
    private static TextMeshProUGUI Label(Transform parent, string name, string text, Vector2 position, Vector2 anchor, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var item = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); item.transform.SetParent(parent, false); RectTransform rect = item.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = new Vector2(1100, 48); var label = item.GetComponent<TextMeshProUGUI>(); label.text = text; label.fontSize = size; label.color = color; label.alignment = alignment; label.textWrappingMode = TextWrappingModes.NoWrap; return label;
    }
    private static void SliceEnemy(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter; if (texture == null || importer == null) return; int frames = Mathf.Max(1, texture.width / 128); if (importer.spriteImportMode == SpriteImportMode.Multiple && AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Any()) return; SpriteMetaData[] data = new SpriteMetaData[frames]; for (int i = 0; i < frames; i++) data[i] = new SpriteMetaData { name = texture.name + "_" + i, rect = new Rect(i * 128, 0, 128, texture.height), pivot = new Vector2(.5f, .1f), alignment = (int)SpriteAlignment.Custom }; importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple; importer.spritePixelsPerUnit = 128; importer.filterMode = FilterMode.Point; importer.spritesheet = data; importer.SaveAndReimport();
    }
    private static Sprite SpriteAt(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    private static GameObject MakeSprite(Transform parent, string name, Sprite sprite, Vector3 position, Vector3 scale, Color color, int order)
    {
        var item = new GameObject(name, typeof(SpriteRenderer)); if (parent != null) item.transform.SetParent(parent); item.transform.position = position; item.transform.localScale = scale; SpriteRenderer renderer = item.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order; return item;
    }
}
#endif
