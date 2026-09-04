#if UNITY_EDITOR
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

/// <summary>Creates a playable dungeon level using the imported CraftPix art.</summary>
public static class EmberRuinsBuilder
{
    private const string ScenePath = "Assets/Scenes/Level01_EmberRuins.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Characters/Player_Kael.prefab";
    private const string Dungeon = "Assets/Art/Tilesets/Dungeon/PNG/";
    private const string Enemies = "Assets/Art/Enemies/";
    private const float TileSize = .64f;

    [MenuItem("Echoes of Ember/Rebuild Level 01 - Dungeon Art", priority = 30)]
    public static void CreateLevel()
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab == null) { EditorUtility.DisplayDialog("Echoes of Ember", "Run Setup Kael (Knight 1) first.", "OK"); return; }
        EnsurePlayerComponents();
        EnsureEnemyFrames();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Level01_EmberRuins";
        Camera camera = CreateCamera();
        Transform world = new GameObject("WORLD - Ember Ruins").transform;
        CreateBackground(world);
        CreateDungeonGeometry(world);
        CreateDecorations(world);
        GameObject kael = CreatePlayer(playerPrefab, world);
        camera.gameObject.AddComponent<CameraFollow2D>().SetTarget(kael.transform);
        CreatePickupsAndEncounters(world);
        CreateDeathZone(world);
        CreateGoal(world);
        CreateUi(kael.GetComponent<PlayerHealth>());
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("Echoes of Ember: Level 01 rebuilt with CraftPix dungeon art.");
    }

    private static void EnsurePlayerComponents()
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        if (contents.GetComponent<PlayerController>() == null) contents.AddComponent<PlayerController>();
        if (contents.GetComponent<PlayerHealth>() == null) contents.AddComponent<PlayerHealth>();
        PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static void EnsureEnemyFrames()
    {
        SliceHorizontal(Enemies + "FantasyEnemies/Skeleton/Idle.png");
        SliceHorizontal(Enemies + "FantasyEnemies/Fire_Spirit/Idle.png");
        SliceHorizontal(Enemies + "Skeletons/Skeleton_Warrior/Idle.png");
        SliceHorizontal(Enemies + "Ghosts/Gotoku/Idle.png");
    }

    private static void SliceHorizontal(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (texture == null || importer == null) return;
        int frames = Mathf.Max(1, texture.width / 128);
        if (importer.spriteImportMode == SpriteImportMode.Multiple && AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Count() == frames) return;
        SpriteMetaData[] data = new SpriteMetaData[frames];
        for (int i = 0; i < frames; i++) data[i] = new SpriteMetaData { name = texture.name + "_" + i, rect = new Rect(i * 128, 0, 128, texture.height), pivot = new Vector2(.5f, .1f), alignment = (int)SpriteAlignment.Custom };
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 128;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritesheet = data;
        importer.SaveAndReimport();
    }

    private static Camera CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera"; cameraObject.transform.position = new Vector3(-4.5f, 0f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 4.5f; camera.backgroundColor = new Color(.018f, .025f, .055f);
        var lightObject = new GameObject("Global Light 2D", typeof(Light2D));
        lightObject.GetComponent<Light2D>().lightType = Light2D.LightType.Global; lightObject.GetComponent<Light2D>().intensity = 1.15f;
        return camera;
    }

    private static void CreateBackground(Transform parent)
    {
        Sprite backdrop = LoadSprite(Dungeon + "Background/Pale/Background.png");
        Sprite depths = LoadSprite(Dungeon + "Background/Pale/bg.png");
        Sprite mist = LoadSprite(Dungeon + "Background/Pale/myst.png");
        for (int i = 0; i < 4; i++)
        {
            float x = -9f + i * 19.15f;
            CreateVisual(parent, "Dungeon Backdrop " + i, backdrop, new Vector3(x, .3f), Vector3.one * 2f, Color.white, -30);
            CreateVisual(parent, "Dungeon Depth " + i, depths, new Vector3(x, .1f), Vector3.one * 2f, new Color(.75f, .82f, 1f, .8f), -29);
            CreateVisual(parent, "Dungeon Mist " + i, mist, new Vector3(x, -1.2f), Vector3.one * 2f, new Color(.8f, .65f, 1f, .45f), -28);
        }
    }

    private static void CreateDungeonGeometry(Transform parent)
    {
        Sprite[] rock = { LoadSprite(Dungeon + "Tiles_rock/tile1.png"), LoadSprite(Dungeon + "Tiles_rock/tile2.png"), LoadSprite(Dungeon + "Tiles_rock/tile3.png"), LoadSprite(Dungeon + "Tiles_rock/tile4.png") };
        // Each gap is only 0.8 world-units, intentionally fair for Kael's jump.
        CreateGround(parent, "Tutorial Ground", -9f, 18, rock);
        CreateGround(parent, "Skeleton Hall", 3.3f, 17, rock);
        CreateGround(parent, "Ember Crossing", 15.2f, 16, rock);
        CreateGround(parent, "Guardian Approach", 26.4f, 20, rock);
        CreateFloatingPlatform(parent, "Tutorial Ledge", -2.1f, -1.95f, 4, rock);
        CreateFloatingPlatform(parent, "Arena Ledge", 8.2f, -1.70f, 5, rock);
        CreateFloatingPlatform(parent, "Ember Ledge", 20.3f, -1.95f, 4, rock);
        CreateFloatingPlatform(parent, "Gate Ledge", 34.5f, -1.70f, 5, rock);
        Sprite lava = LoadSprite(Dungeon + "Tiles_lava/lava_tile1.png");
        foreach (float start in new[] { 2f, 13.8f, 25f }) for (float x = start; x <= start + .8f; x += TileSize) CreateVisual(parent, "Lava", lava, new Vector3(x, -4.45f), Vector3.one, Color.white, -1);
    }

    private static void CreateGround(Transform parent, string name, float x, int tiles, Sprite[] sprites)
    {
        float width = tiles * TileSize;
        CreatePhysicsPlatform(parent, name + " Collider", new Vector2(x, -3.65f), new Vector2(width, 1.3f));
        float start = x - width * .5f + TileSize * .5f;
        for (int i = 0; i < tiles; i++)
        {
            CreateVisual(parent, name + " Top " + i, sprites[i % sprites.Length], new Vector3(start + i * TileSize, -3.32f), Vector3.one, Color.white, 0);
            CreateVisual(parent, name + " Fill " + i, sprites[(i + 2) % sprites.Length], new Vector3(start + i * TileSize, -3.96f), Vector3.one, Color.white, -1);
        }
    }

    private static void CreateFloatingPlatform(Transform parent, string name, float x, float y, int tiles, Sprite[] sprites)
    {
        float width = tiles * TileSize;
        CreatePhysicsPlatform(parent, name + " Collider", new Vector2(x, y - .22f), new Vector2(width, .34f));
        float start = x - width * .5f + TileSize * .5f;
        for (int i = 0; i < tiles; i++) CreateVisual(parent, name + " Stone " + i, sprites[(i + 1) % sprites.Length], new Vector3(start + i * TileSize, y), Vector3.one, Color.white, 1);
    }

    private static void CreatePhysicsPlatform(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var item = new GameObject(name, typeof(BoxCollider2D)); item.transform.SetParent(parent); item.transform.position = position; item.GetComponent<BoxCollider2D>().size = size;
    }

    private static void CreateDecorations(Transform parent)
    {
        Sprite torch = LoadSprite(Dungeon + "Details/torch1_1.png"); Sprite stalactite = LoadSprite(Dungeon + "Details/stalactite2.png"); Sprite stalagmite = LoadSprite(Dungeon + "Details/stalagmite3.png"); Sprite statue = LoadSprite(Dungeon + "Details/marker_statue2.png"); Sprite bridge = LoadSprite(Dungeon + "Details/bridge2.png");
        for (int i = 0; i < 13; i++) { float x = -8.5f + i * 3.5f; CreateVisual(parent, "Torch " + i, torch, new Vector3(x, -2.25f), Vector3.one * .9f, Color.white, 3); CreateVisual(parent, "Stalactite " + i, stalactite, new Vector3(x + 1.2f, 3.5f), Vector3.one * .85f, Color.white, -20); }
        foreach (float x in new[] { -5f, 5.5f, 17.6f, 29.5f }) CreateVisual(parent, "Ancient Statue", statue, new Vector3(x, -2.3f), Vector3.one, Color.white, 2);
        foreach (float x in new[] { .2f, 12.1f, 24f }) { CreateVisual(parent, "Bridge Detail", bridge, new Vector3(x, -3f), Vector3.one * 1.5f, Color.white, 2); CreateVisual(parent, "Stalagmite", stalagmite, new Vector3(x + .9f, -2.65f), Vector3.one * .7f, Color.white, 2); }
    }

    private static GameObject CreatePlayer(GameObject prefab, Transform parent)
    {
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab); player.name = "Kael Emberbound"; player.transform.SetParent(parent); player.transform.position = new Vector3(-8f, -2.98f); player.transform.localScale = Vector3.one * 2.25f; return player;
    }

    private static void CreatePickupsAndEncounters(Transform parent)
    {
        Sprite ember = LoadSprite(Dungeon + "Items/000_0045_coin.png"); Sprite heart = LoadSprite(Dungeon + "Items/000_0065_heart.png");
        float[] positions = { -7f, -5.4f, -3.8f, -2.1f, 0f, 4.5f, 6.2f, 8.2f, 10f, 16f, 18f, 20.3f, 22.2f, 27.2f, 30f, 33.2f, 36.5f };
        foreach (float x in positions)
        {
            bool onLedge = Mathf.Abs(x + 2.1f) < .01f || Mathf.Abs(x - 8.2f) < .01f || Mathf.Abs(x - 20.3f) < .01f;
            CreateShard(parent, ember, new Vector2(x, onLedge ? -.95f : -2.2f));
        }
        CreateHeart(parent, heart, new Vector2(19f, -2.15f)); CreateCheckpoint(parent, LoadSprite(Dungeon + "Details/torch2_1.png"), new Vector2(15.9f, -2.25f));
        CreateEnemy(parent, "Skeleton Scout", GetFrame(Enemies + "FantasyEnemies/Skeleton/Idle.png"), new Vector2(5.5f, -2.72f), 1, 1.2f, .8f);
        CreateEnemy(parent, "Skeleton Guard", GetFrame(Enemies + "Skeletons/Skeleton_Warrior/Idle.png"), new Vector2(11.1f, -2.72f), 1, 1.1f, .95f);
        CreateEnemy(parent, "Fire Spirit", GetFrame(Enemies + "FantasyEnemies/Fire_Spirit/Idle.png"), new Vector2(23.1f, -2.3f), 1, 1.35f, .85f);
        // Final enemy is deliberately kept well before the shrine entrance.
        CreateEnemy(parent, "Ruins Guardian", GetFrame(Enemies + "Ghosts/Gotoku/Idle.png"), new Vector2(27.8f, -2.55f), 1, 1f, 1.35f);
    }

    private static void CreateShard(Transform parent, Sprite sprite, Vector2 position)
    {
        var item = new GameObject("Ember Shard", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(EmberShard)); item.transform.SetParent(parent); item.transform.position = position; item.transform.localScale = Vector3.one * 1.1f; item.GetComponent<SpriteRenderer>().sprite = sprite; item.GetComponent<SpriteRenderer>().sortingOrder = 6; item.GetComponent<CircleCollider2D>().isTrigger = true;
    }
    private static void CreateHeart(Transform parent, Sprite sprite, Vector2 position)
    {
        var item = new GameObject("Heart Pickup", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(HeartPickup)); item.transform.SetParent(parent); item.transform.position = position; item.transform.localScale = Vector3.one * 1.15f; item.GetComponent<SpriteRenderer>().sprite = sprite; item.GetComponent<SpriteRenderer>().sortingOrder = 6; item.GetComponent<CircleCollider2D>().isTrigger = true;
    }
    private static void CreateCheckpoint(Transform parent, Sprite sprite, Vector2 position)
    {
        var item = new GameObject("Ember Checkpoint", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Checkpoint)); item.transform.SetParent(parent); item.transform.position = position; item.transform.localScale = Vector3.one * 1.15f; item.GetComponent<SpriteRenderer>().sprite = sprite; item.GetComponent<SpriteRenderer>().sortingOrder = 5; item.GetComponent<BoxCollider2D>().isTrigger = true; item.GetComponent<BoxCollider2D>().size = new Vector2(.45f, .75f);
    }
    private static void CreateEnemy(Transform parent, string name, Sprite sprite, Vector2 position, int health, float speed, float scale)
    {
        var item = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(EnemyController)); item.transform.SetParent(parent); item.transform.position = position; item.transform.localScale = Vector3.one * scale;
        item.GetComponent<SpriteRenderer>().sprite = sprite; item.GetComponent<SpriteRenderer>().sortingOrder = 4; BoxCollider2D hitbox = item.GetComponent<BoxCollider2D>(); hitbox.isTrigger = true; hitbox.size = new Vector2(.62f, .85f); hitbox.offset = new Vector2(0f, .08f); Rigidbody2D body = item.GetComponent<Rigidbody2D>(); body.gravityScale = 0f; body.freezeRotation = true;
        SerializedObject so = new SerializedObject(item.GetComponent<EnemyController>()); so.FindProperty("health").intValue = health; so.FindProperty("moveSpeed").floatValue = speed; so.FindProperty("patrolDistance").floatValue = 1.4f; so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateDeathZone(Transform parent)
    {
        var item = new GameObject("Lava Death Zone", typeof(BoxCollider2D), typeof(DeathZone)); item.transform.SetParent(parent); item.transform.position = new Vector2(15f, -6.3f); item.GetComponent<BoxCollider2D>().size = new Vector2(80f, 1.2f); item.GetComponent<BoxCollider2D>().isTrigger = true;
    }
    private static void CreateGoal(Transform parent)
    {
        // A real end-of-level shrine: the gate sits on a broad, safe stone
        // dais connected to the main path, rather than floating at the edge.
        const float shrineX = 33.8f;
        Sprite[] stones = { LoadSprite(Dungeon + "Tiles_rock/tile2.png"), LoadSprite(Dungeon + "Tiles_rock/tile3.png"), LoadSprite(Dungeon + "Tiles_rock/tile4.png") };
        CreatePhysicsPlatform(parent, "Ember Gate Shrine Collider", new Vector2(shrineX, -3.65f), new Vector2(6.4f, 1.3f));
        float start = shrineX - 2.88f;
        for (int i = 0; i < 10; i++)
        {
            Sprite stone = stones[i % stones.Length];
            CreateVisual(parent, "Gate Shrine Stone " + i, stone, new Vector3(start + i * TileSize, -3.32f), Vector3.one, Color.white, 2);
            CreateVisual(parent, "Gate Shrine Fill " + i, stones[(i + 1) % stones.Length], new Vector3(start + i * TileSize, -3.96f), Vector3.one, Color.white, 1);
        }
        var item = new GameObject("Ember Gate - Press E", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(LevelGoal)); item.transform.SetParent(parent); item.transform.position = new Vector2(shrineX, -2.28f); item.transform.localScale = Vector3.one * 1.55f; item.GetComponent<SpriteRenderer>().sprite = LoadSprite(Dungeon + "Details/door.png"); item.GetComponent<SpriteRenderer>().sortingOrder = 7; item.GetComponent<BoxCollider2D>().isTrigger = true; item.GetComponent<BoxCollider2D>().size = new Vector2(1.35f, 1.7f); item.GetComponent<BoxCollider2D>().offset = new Vector2(0f, .08f);
        Sprite torch = LoadSprite(Dungeon + "Details/torch2_1.png");
        CreateVisual(parent, "Gate Torch Left", torch, new Vector3(shrineX - 2.25f, -2.28f), Vector3.one * 1.15f, new Color(1f, .72f, .3f, 1f), 7);
        CreateVisual(parent, "Gate Torch Right", torch, new Vector3(shrineX + 2.25f, -2.28f), Vector3.one * 1.15f, new Color(1f, .72f, .3f, 1f), 7);
    }

    private static void CreateUi(PlayerHealth hero)
    {
        var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        TextMeshProUGUI hp = AddLabel(canvasObject.transform, "HP", "HP  5/5", new Vector2(32, -28), new Vector2(0, 1), 28, new Color(1f, .46f, .36f)); TextMeshProUGUI score = AddLabel(canvasObject.transform, "Score", "EMBER  0", new Vector2(-32, -28), new Vector2(1, 1), 28, new Color(1f, .73f, .18f), TextAlignmentOptions.Right); TextMeshProUGUI kills = AddLabel(canvasObject.transform, "Kills", "KILLS  0", new Vector2(-32, -66), new Vector2(1, 1), 18, Color.white, TextAlignmentOptions.Right); TextMeshProUGUI message = AddLabel(canvasObject.transform, "Message", "", new Vector2(0, 120), new Vector2(.5f, 0), 26, Color.white, TextAlignmentOptions.Center); AddLabel(canvasObject.transform, "Objective", "Reach the Ember Gate  •  A/D Move  •  Space Jump  •  J Attack  •  K Defend", new Vector2(0, 28), new Vector2(.5f, 0), 17, new Color(.85f, .9f, 1f), TextAlignmentOptions.Center);
        GameObject pause = CreatePanel(canvasObject.transform, "PAUSED", "Resume", MenuAction.Resume, "Restart", MenuAction.Restart); GameObject over = CreatePanel(canvasObject.transform, "KAEL HAS FALLEN", "Restart", MenuAction.Restart, "Movement Test", MenuAction.BackToTest); GameObject victory = CreatePanel(canvasObject.transform, "EMBER GATE RESTORED", "Restart", MenuAction.Restart, "Movement Test", MenuAction.BackToTest); var manager = new GameObject("GameManager", typeof(GameManager)); manager.GetComponent<GameManager>().Configure(hero, hp, score, kills, message, pause, over, victory);
    }
    private static TextMeshProUGUI AddLabel(Transform parent, string name, string text, Vector2 position, Vector2 anchor, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var item = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); item.transform.SetParent(parent, false); RectTransform rect = item.GetComponent<RectTransform>(); rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = new Vector2(1000, 48); TextMeshProUGUI label = item.GetComponent<TextMeshProUGUI>(); label.text = text; label.fontSize = size; label.color = color; label.alignment = alignment; label.textWrappingMode = TextWrappingModes.NoWrap; return label;
    }
    private static GameObject CreatePanel(Transform parent, string title, string firstText, MenuAction first, string secondText, MenuAction second)
    {
        var panel = new GameObject(title + " Panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(parent, false); RectTransform rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(560, 280); panel.GetComponent<Image>().color = new Color(.025f, .04f, .10f, .94f); AddLabel(panel.transform, "Title", title, new Vector2(0, 70), new Vector2(.5f, .5f), 36, new Color(1f, .68f, .24f), TextAlignmentOptions.Center); CreateButton(panel.transform, firstText, new Vector2(-130, -45), first); CreateButton(panel.transform, secondText, new Vector2(130, -45), second); return panel;
    }
    private static void CreateButton(Transform parent, string title, Vector2 position, MenuAction action)
    {
        var item = new GameObject(title + " Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuActionButton)); item.transform.SetParent(parent, false); RectTransform rect = item.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(220, 58); item.GetComponent<Image>().color = new Color(.9f, .29f, .08f, .95f); MenuActionButton handler = item.GetComponent<MenuActionButton>(); handler.SetAction(action); UnityEventTools.AddPersistentListener(item.GetComponent<Button>().onClick, handler.InvokeAction); AddLabel(item.transform, "Label", title, Vector2.zero, new Vector2(.5f, .5f), 20, Color.white, TextAlignmentOptions.Center);
    }
    private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    private static Sprite GetFrame(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    private static GameObject CreateVisual(Transform parent, string name, Sprite sprite, Vector3 position, Vector3 scale, Color color, int order)
    {
        var item = new GameObject(name, typeof(SpriteRenderer)); item.transform.SetParent(parent); item.transform.position = position; item.transform.localScale = scale; SpriteRenderer renderer = item.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order; return item;
    }
}
#endif
