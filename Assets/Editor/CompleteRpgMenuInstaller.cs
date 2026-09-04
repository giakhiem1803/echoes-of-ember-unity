using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CompleteRpgMenuInstaller
{
    // r4 forces one clean reinstall after legacy CampaignSceneBuilder panels
    // were found in saved scenes without RpgMenuCommandButton components.
    private const string Revision = "Echoes.CompleteRpgMenus.2026.08.27.r4";
    private const string RootName = "COMPLETE RPG MENUS";
    private static readonly string[] Levels =
    {
        "Assets/Scenes/Level01_EmberRuins.unity", "Assets/Scenes/Level02_CrystalDepths.unity",
        "Assets/Scenes/Level03_AshenForge.unity", "Assets/Scenes/Level04_ShadowCitadel.unity",
        "Assets/Scenes/Level05_EmberThrone.unity"
    };

    static CompleteRpgMenuInstaller() { EditorApplication.update -= AutoRun; EditorApplication.update += AutoRun; }

    [MenuItem("Echoes of Ember/INSTALL Complete RPG Menus (All Levels)", priority = -49)]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string active = SceneManager.GetActiveScene().path;
        try
        {
            int done = 0;
            for (int i = 0; i < Levels.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Levels[i]) == null) continue;
                EditorUtility.DisplayProgressBar("Echoes of Ember", "Installing complete RPG menus", (i + 1f) / Levels.Length);
                Scene scene = EditorSceneManager.OpenScene(Levels[i], OpenSceneMode.Single);
                if (!BuildScene(scene, i + 1)) throw new InvalidOperationException("Required Canvas/GameManager missing in " + Levels[i]);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); done++;
            }
            if (done != 5) throw new InvalidOperationException($"Only {done}/5 scenes received complete menus.");
            EditorPrefs.SetBool(Revision, true); AssetDatabase.SaveAssets();
            Debug.Log("Echoes of Ember: complete CraftPix RPG menus installed for all 5 campaign levels.");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (!string.IsNullOrEmpty(active) && AssetDatabase.LoadAssetAtPath<SceneAsset>(active) != null) EditorSceneManager.OpenScene(active, OpenSceneMode.Single);
        }
    }

    private static void AutoRun()
    {
        if (EditorPrefs.GetBool(Revision, false)) { EditorApplication.update -= AutoRun; return; }
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try { Install(); if (EditorPrefs.GetBool(Revision, false)) EditorApplication.update -= AutoRun; }
        catch (Exception e) { Debug.LogWarning("Complete RPG menus will retry: " + e.Message); }
    }

    private static bool BuildScene(Scene scene, int stage)
    {
        Canvas canvas = FindInScene<Canvas>(scene); GameManager manager = FindInScene<GameManager>(scene);
        if (canvas == null || manager == null) return false;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        EnsureEventSystem(scene);
        Transform old = canvas.transform.Find(RootName); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        GameObject root = Ui(RootName, canvas.transform); Stretch(root.GetComponent<RectTransform>());
        CompleteRpgMenuController controller = root.AddComponent<CompleteRpgMenuController>();

        Sprite mainSprite = SpriteAt("Assets/Art/UI/RPG/PNG/Main_menu.png");
        Sprite craftSprite = SpriteAt("Assets/Art/UI/RPG/PNG/Craft.png");
        Sprite settingsSprite = SpriteAt("Assets/Art/UI/RPG/PNG/Settings.png");
        Sprite levelsSprite = SpriteAt("Assets/Art/UI/RPG/PNG/Levels.png");
        Sprite resultSprite = SpriteAt("Assets/Art/UI/RPG/PNG/Win_loose.png");

        GameObject pause = Panel("PAUSED Panel", root.transform, mainSprite, new Vector2(760, 540));
        Title(pause.transform, "PAUSED", new Vector2(0, 190));
        ButtonAt(pause.transform, "RESUME", new Vector2(-170, 95), RpgMenuCommand.Resume);
        ButtonAt(pause.transform, "INVENTORY", new Vector2(170, 95), RpgMenuCommand.Inventory);
        ButtonAt(pause.transform, "CRAFT", new Vector2(-170, 25), RpgMenuCommand.Crafting);
        ButtonAt(pause.transform, "SETTINGS", new Vector2(170, 25), RpgMenuCommand.Settings);
        ButtonAt(pause.transform, "LEVELS", new Vector2(-170, -45), RpgMenuCommand.Levels);
        ButtonAt(pause.transform, "RESTART", new Vector2(170, -45), RpgMenuCommand.Restart);
        ButtonAt(pause.transform, "CAMPAIGN HUB", new Vector2(0, -120), RpgMenuCommand.CampaignHub, 340);

        GameObject crafting = Panel("CRAFTING Panel", root.transform, craftSprite, new Vector2(980, 690));
        Title(crafting.transform, "EMBER FORGE", new Vector2(0, 275));
        Info(crafting.transform, "Spend Ember to forge permanent equipment", new Vector2(0, 225), 27);
        CraftRow(crafting.transform, "EMBER BLADE", "+1 DAMAGE", "60 EMBER", new Vector2(0, 110), RpgMenuCommand.CraftBlade);
        CraftRow(crafting.transform, "CINDER MAIL", "+2 MAX HP", "80 EMBER", new Vector2(0, 5), RpgMenuCommand.CraftArmor);
        CraftRow(crafting.transform, "WIND RELIC", "+ MOVE SPEED", "100 EMBER", new Vector2(0, -100), RpgMenuCommand.CraftRelic);
        ButtonAt(crafting.transform, "CLOSE", new Vector2(0, -255), RpgMenuCommand.Close);
        TextMeshProUGUI status = Info(crafting.transform, "", new Vector2(0, -205), 25);

        GameObject settings = Panel("SETTINGS Panel", root.transform, settingsSprite, new Vector2(760, 580));
        Title(settings.transform, "SETTINGS", new Vector2(0, 215));
        SliderAt(settings.transform, "MUSIC", new Vector2(0, 95), EchoesVolumeChannel.Music);
        SliderAt(settings.transform, "SFX", new Vector2(0, 5), EchoesVolumeChannel.Sfx);
        Info(settings.transform, "ESC closes menus   •   Settings are saved", new Vector2(0, -90), 24);
        ButtonAt(settings.transform, "CLOSE", new Vector2(0, -180), RpgMenuCommand.Close);

        GameObject levels = Panel("LEVELS Panel", root.transform, levelsSprite, new Vector2(850, 590));
        Title(levels.transform, "CAMPAIGN LEVELS", new Vector2(0, 215));
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 140f;
            ButtonAt(levels.transform, (i + 1).ToString(), new Vector2(x, 40), (RpgMenuCommand)((int)RpgMenuCommand.LoadLevel1 + i), 110, 110);
            Info(levels.transform, i == stage - 1 ? "CURRENT" : StageShort(i + 1), new Vector2(x, -45), 18, 130, 35);
        }
        ButtonAt(levels.transform, "CLOSE", new Vector2(0, -180), RpgMenuCommand.Close);

        GameObject over = ResultPanel("KAEL HAS FALLEN Panel", root.transform, resultSprite, "KAEL HAS FALLEN", false);
        GameObject victory = ResultPanel("EMBER GATE RESTORED Panel", root.transform, resultSprite, "LEVEL COMPLETE", true);
        controller.Configure(pause, crafting, settings, levels, status);
        AssignManagerPanels(manager, pause, over, victory);
        pause.SetActive(false); crafting.SetActive(false); settings.SetActive(false); levels.SetActive(false); over.SetActive(false); victory.SetActive(false);
        return true;
    }

    private static GameObject ResultPanel(string name, Transform parent, Sprite sprite, string title, bool victory)
    {
        // Do not display CraftPix's full Win_loose showcase image here. It
        // already contains sample stars, words and buttons, which caused the
        // real UI to be drawn on top of a giant duplicate interface.
        GameObject panel = Panel(name, parent, sprite, new Vector2(680, 410));
        Title(panel.transform, title, new Vector2(0, 125));
        Info(panel.transform, victory ? "The Ember Gate is restored" : "The darkness claims another flame", new Vector2(0, 67), 24);
        ButtonAt(panel.transform, victory ? "NEXT LEVEL" : "RESTART", new Vector2(-145, -45), victory ? RpgMenuCommand.NextLevel : RpgMenuCommand.Restart, 250);
        ButtonAt(panel.transform, "CAMPAIGN HUB", new Vector2(145, -45), RpgMenuCommand.CampaignHub, 250);
        Info(panel.transform, victory ? "Ember  •  Kills  •  Chests saved" : "R also restarts the stage", new Vector2(0, -125), 20);
        return panel;
    }

    private static void AssignManagerPanels(GameManager manager, GameObject pause, GameObject over, GameObject victory)
    {
        SerializedObject so = new SerializedObject(manager); so.FindProperty("pausePanel").objectReferenceValue = pause;
        so.FindProperty("gameOverPanel").objectReferenceValue = over; so.FindProperty("victoryPanel").objectReferenceValue = victory; so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject Panel(string name, Transform parent, Sprite sprite, Vector2 size)
    {
        GameObject scrim = Ui(name, parent); Stretch(scrim.GetComponent<RectTransform>());
        Image shade = scrim.AddComponent<Image>(); shade.color = new Color(.015f, .025f, .04f, .62f); shade.raycastTarget = true;

        // Build a clean RPG window from simple pixel-colour layers. The source
        // CraftPix PNGs are showcase sheets, not ready-to-use nine-sliced
        // panels, so stretching them produced the huge broken UI seen in-game.
        GameObject frame = Ui("CraftPix Frame", scrim.transform); RectTransform rect = frame.GetComponent<RectTransform>(); rect.sizeDelta = size;
        Image outer = frame.AddComponent<Image>(); outer.color = new Color(.20f, .09f, .035f, 1f);

        GameObject border = Ui("Gold Border", frame.transform); RectTransform borderRect = border.GetComponent<RectTransform>(); Stretch(borderRect);
        borderRect.offsetMin = new Vector2(7, 7); borderRect.offsetMax = new Vector2(-7, -7);
        border.AddComponent<Image>().color = new Color(.78f, .49f, .16f, 1f);

        GameObject parchment = Ui("Parchment", border.transform); RectTransform paperRect = parchment.GetComponent<RectTransform>(); Stretch(paperRect);
        paperRect.offsetMin = new Vector2(7, 7); paperRect.offsetMax = new Vector2(-7, -7);
        parchment.AddComponent<Image>().color = new Color(.13f, .075f, .035f, .985f);

        GameObject header = Ui("Emerald Header", frame.transform); RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1); headerRect.anchorMax = Vector2.one; headerRect.pivot = new Vector2(.5f, 1f);
        headerRect.offsetMin = new Vector2(14, -66); headerRect.offsetMax = new Vector2(-14, -14);
        header.AddComponent<Image>().color = new Color(.055f, .34f, .23f, 1f);
        return scrim;
    }

    private static void CraftRow(Transform panel, string item, string bonus, string price, Vector2 position, RpgMenuCommand command)
    {
        Transform frame = panel.Find("CraftPix Frame"); GameObject row = Ui(item, frame); RectTransform r = row.GetComponent<RectTransform>(); r.anchoredPosition = position; r.sizeDelta = new Vector2(720, 82);
        Image bg = row.AddComponent<Image>(); bg.color = new Color(.18f, .10f, .05f, .88f);
        Info(row.transform, item, new Vector2(-215, 16), 25, 250, 34); Info(row.transform, bonus, new Vector2(-215, -18), 20, 250, 30);
        Info(row.transform, price, new Vector2(70, 0), 23, 170, 42); ButtonAt(row.transform, "CREATE", new Vector2(270, 0), command, 150, 56);
    }

    private static void SliderAt(Transform panel, string label, Vector2 position, EchoesVolumeChannel channel)
    {
        Transform frame = panel.Find("CraftPix Frame"); Info(frame, label, position + Vector2.left * 190, 28, 180, 50);
        GameObject go = Ui(label + " Slider", frame); RectTransform r = go.GetComponent<RectTransform>(); r.anchoredPosition = position + Vector2.right * 75; r.sizeDelta = new Vector2(350, 30);
        Slider slider = go.AddComponent<Slider>(); Image bg = go.AddComponent<Image>(); bg.color = new Color(.18f,.11f,.06f,.95f); slider.targetGraphic = bg;
        GameObject fillArea = Ui("Fill Area", go.transform); Stretch(fillArea.GetComponent<RectTransform>()); fillArea.GetComponent<RectTransform>().offsetMin = new Vector2(5,5); fillArea.GetComponent<RectTransform>().offsetMax = new Vector2(-5,-5);
        GameObject fill = Ui("Fill", fillArea.transform); Stretch(fill.GetComponent<RectTransform>()); Image fi = fill.AddComponent<Image>(); fi.color = new Color(.2f,.82f,.45f,1); slider.fillRect = fi.rectTransform;
        slider.minValue = 0; slider.maxValue = 1; go.AddComponent<EchoesVolumeSlider>().Configure(channel);
    }

    private static Button ButtonAt(Transform parent, string label, Vector2 position, RpgMenuCommand command, float width = 290, float height = 58)
    {
        Transform frame = parent.name.EndsWith("Panel") && parent.Find("CraftPix Frame") != null ? parent.Find("CraftPix Frame") : parent;
        GameObject go = Ui(label + " Button", frame); RectTransform r = go.GetComponent<RectTransform>(); r.anchoredPosition = position; r.sizeDelta = new Vector2(width, height);
        Image image = go.AddComponent<Image>(); image.color = new Color(.10f, .50f, .29f, 1f); Button button = go.AddComponent<Button>();
        ColorBlock colors = button.colors; colors.highlightedColor = new Color(.3f,.85f,.5f,1); colors.pressedColor = new Color(.08f,.35f,.2f,1); button.colors = colors;
        go.AddComponent<RpgMenuCommandButton>().Configure(command);
        TextMeshProUGUI labelText = Info(go.transform, label, Vector2.zero, 22, width - 12, height - 6);
        labelText.color = new Color(1f, .94f, .72f, 1f);
        return button;
    }

    private static TextMeshProUGUI Title(Transform panel, string text, Vector2 position) => Info(panel.Find("CraftPix Frame"), text, position, 36, 620, 56);
    private static TextMeshProUGUI Info(Transform parent, string text, Vector2 position, float size, float width = 620, float height = 55)
    {
        GameObject go = Ui(text + " Text", parent); RectTransform r = go.GetComponent<RectTransform>(); r.anchoredPosition = position; r.sizeDelta = new Vector2(width, height);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>(); tmp.text = text; tmp.fontSize = size; tmp.color = new Color(1f,.91f,.62f,1); tmp.alignment = TextAlignmentOptions.Center; tmp.enableWordWrapping = false; tmp.raycastTarget = false; return tmp;
    }

    private static GameObject Ui(string name, Transform parent) { GameObject go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go; }
    private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero; }
    private static string StageShort(int i) => new[] { "RUINS", "CRYSTAL", "FORGE", "SHADOW", "THRONE" }[i - 1];
    private static Sprite SpriteAt(string path) { Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path); return s != null ? s : AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(); }
    private static T FindInScene<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<T>(true)).FirstOrDefault();
    private static void EnsureEventSystem(Scene scene)
    {
        EventSystem es = FindInScene<EventSystem>(scene); if (es == null) { GameObject go = new GameObject("EventSystem", typeof(EventSystem)); SceneManager.MoveGameObjectToScene(go, scene); es = go.GetComponent<EventSystem>(); }
        StandaloneInputModule old = es.GetComponent<StandaloneInputModule>(); if (old != null) UnityEngine.Object.DestroyImmediate(old); if (es.GetComponent<InputSystemUIInputModule>() == null) es.gameObject.AddComponent<InputSystemUIInputModule>();
    }
}
