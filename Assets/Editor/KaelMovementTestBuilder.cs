#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

/// <summary>Creates a separate, disposable movement demo scene for Kael.</summary>
public static class KaelMovementTestBuilder
{
    private const string ScenePath = "Assets/Scenes/KaelMovementTest.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Characters/Player_Kael.prefab";
    private const string GroundSpritePath = "Assets/Materials/KaelMovementGround.png";
    private const string RebuildMarkerPath = "Library/KaelMovementTest.rebuild";

    [InitializeOnLoadMethod]
    private static void RebuildWhenRequested()
    {
        if (!File.Exists(RebuildMarkerPath)) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(RebuildMarkerPath);
            CreateScene();
        };
    }

    [MenuItem("Echoes of Ember/Create Kael Movement Test Scene", priority = 20)]
    public static void CreateScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Kael movement test", "Run Echoes of Ember > Setup Kael (Knight 1) before creating the test scene.", "OK");
            return;
        }

        EnsurePlayerControllerOnPrefab();
        Sprite groundSprite = CreateGroundSprite();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "KaelMovementTest";
        CreateCamera();
        CreateBackground(groundSprite);
        CreateGround(groundSprite);
        CreatePlayer(prefab);
        CreateInstructions();

        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("Echoes of Ember: KaelMovementTest created. Press Play and use A/D, Space, J, K.");
    }

    private static void EnsurePlayerControllerOnPrefab()
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        if (contents.GetComponent<PlayerController>() == null)
            contents.AddComponent<PlayerController>();
        PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static Sprite CreateGroundSprite()
    {
        if (!File.Exists(GroundSpritePath))
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Color[] pixels = { new(0.13f, 0.08f, 0.12f), new(0.18f, 0.11f, 0.16f), new(0.18f, 0.11f, 0.16f), new(0.13f, 0.08f, 0.12f) };
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(GroundSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(GroundSpritePath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(GroundSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(GroundSpritePath);
    }

    private static void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.6f;
        camera.backgroundColor = new Color(0.035f, 0.06f, 0.13f);

        var globalLightObject = new GameObject("Global Light 2D", typeof(Light2D));
        Light2D globalLight = globalLightObject.GetComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.intensity = 1f;
    }

    private static void CreateBackground(Sprite sprite)
    {
        var backdrop = new GameObject("Backdrop", typeof(SpriteRenderer));
        var renderer = backdrop.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.09f, 0.18f, 0.30f);
        renderer.sortingOrder = -5;
        backdrop.transform.position = new Vector3(0f, 0.3f, 0f);
        // The generated test sprite is 2x2 pixels at 64 PPU (0.03125 world units).
        // These values intentionally convert it into a full camera-sized backdrop.
        backdrop.transform.localScale = new Vector3(640f, 320f, 1f);
    }

    private static void CreateGround(Sprite sprite)
    {
        var ground = new GameObject("Ground", typeof(SpriteRenderer));
        var renderer = ground.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.22f, 0.14f, 0.20f);
        renderer.sortingOrder = -1;
        ground.transform.position = new Vector3(0f, -4.2f, 0f);
        ground.transform.localScale = new Vector3(650f, 138f, 1f);

        // Physics is deliberately independent from the visual sprite. This
        // prevents a tiny source texture or visual scale from changing the
        // platform collision area.
        var physicsFloor = new GameObject("PhysicsFloor", typeof(BoxCollider2D));
        physicsFloor.transform.position = new Vector3(0f, -2.54f, 0f);
        physicsFloor.GetComponent<BoxCollider2D>().size = new Vector2(30f, 1f);

        var platform = new GameObject("Floating Platform", typeof(SpriteRenderer));
        var platformRenderer = platform.GetComponent<SpriteRenderer>();
        platformRenderer.sprite = sprite;
        platformRenderer.color = new Color(0.48f, 0.25f, 0.15f);
        platform.transform.position = new Vector3(2f, -0.9f, 0f);
        platform.transform.localScale = new Vector3(110f, 14f, 1f);

        var platformPhysics = new GameObject("Floating Platform Physics", typeof(BoxCollider2D));
        platformPhysics.transform.position = platform.transform.position;
        platformPhysics.GetComponent<BoxCollider2D>().size = new Vector2(3.4f, 0.44f);
    }

    private static void CreatePlayer(GameObject prefab)
    {
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.name = "Kael";
        player.transform.position = new Vector3(-4.6f, -2.03f, 0f);
        player.transform.localScale = Vector3.one * 2.25f;
    }

    private static void CreateInstructions()
    {
        var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        AddText(canvasObject.transform, "Title", "KAEL — MOVEMENT TEST", new Vector2(30, -24), new Vector2(0, 1), 30, new Color(1f, 0.72f, 0.34f));
        AddText(canvasObject.transform, "Hint", "A / D: Move     SPACE / W: Jump     J / Left Click: Attack     K / Right Click: Defend", new Vector2(30, -66), new Vector2(0, 1), 18, Color.white);
        AddText(canvasObject.transform, "Objective", "Practice movement and combat animations before entering the Ember Ruins.", new Vector2(30, 32), new Vector2(0, 0), 16, new Color(0.78f, 0.86f, 1f));
    }

    private static void AddText(Transform parent, string name, string message, Vector2 anchoredPosition, Vector2 anchor, float fontSize, Color color)
    {
        var item = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(1100f, 42f);
        var label = item.GetComponent<TextMeshProUGUI>();
        label.text = message;
        label.fontSize = fontSize;
        label.color = color;
        label.enableWordWrapping = false;
    }
}
#endif
