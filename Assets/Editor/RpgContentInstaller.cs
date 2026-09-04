#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Installs the treasure / spell gameplay pass across the campaign without requiring
/// manual placement in five scenes. Safe to run again: it replaces only its own
/// "RPG Chests" objects and its Mana HUD label.
/// </summary>
public static class RpgContentInstaller
{
    private const string Root = "Assets";
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Level01_EmberRuins.unity",
        "Assets/Scenes/Level02_CrystalDepths.unity",
        "Assets/Scenes/Level03_AshenForge.unity",
        "Assets/Scenes/Level04_ShadowCitadel.unity",
        "Assets/Scenes/Level05_EmberThrone.unity"
    };

    [MenuItem("Echoes of Ember/Install Complete CraftPix RPG UI")]
    public static void Install()
    {
        NormalizeIconSprites();
        Sprite chestClosed = LoadSprite("Assets/Art/Tilesets/Medieval/Objects/chest_closed.png");
        Sprite chestOpened = LoadSprite("Assets/Art/Tilesets/Medieval/Objects/chest_opened.png");
        Sprite fireball = LoadSprite("Assets/Art/Effects/Magic/1 Magic/1.png");
        Sprite inventoryFrame = LoadSprite("Assets/Art/UI/RPG/PNG/Inventory.png");
        Sprite[] rewardIcons = LoadRewardIcons();
        if (chestClosed == null || chestOpened == null || fireball == null)
        {
            EditorUtility.DisplayDialog("Echoes of Ember", "CraftPix sprites are missing. Check Assets/Art/Tilesets and Assets/Art/Effects.", "OK");
            return;
        }

        ConfigurePlayerPrefab(fireball);
        int completed = 0;
        try
        {
            foreach (string path in ScenePaths)
            {
                if (!System.IO.File.Exists(path)) continue;
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                InstallScene(path, chestClosed, chestOpened, inventoryFrame, rewardIcons);
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                completed++;
            }
        }
        finally { EditorUtility.ClearProgressBar(); }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Echoes of Ember", $"RPG pass installed in {completed} levels.\n\nWalk into a chest and press E. The first chest unlocks Fireball (F).", "Open Level 01");
        if (System.IO.File.Exists(ScenePaths[0])) EditorSceneManager.OpenScene(ScenePaths[0], OpenSceneMode.Single);
    }

    private static void ConfigurePlayerPrefab(Sprite fireball)
    {
        const string path = "Assets/Prefabs/Characters/Player_Kael.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
        if (prefabRoot == null) return;
        try
        {
            PlayerMagic magic = prefabRoot.GetComponent<PlayerMagic>();
            if (magic == null) magic = prefabRoot.AddComponent<PlayerMagic>();
            magic.Configure(fireball);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }
    }

    private static void InstallScene(string path, Sprite closed, Sprite opened, Sprite inventoryFrame, Sprite[] rewardIcons)
    {
        int level = GetLevelNumber(path);
        EditorUtility.DisplayProgressBar("Echoes of Ember", $"Building RPG content for level {level}...", level / 5f);
        GameObject oldRoot = GameObject.Find("RPG Chests");
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);
        GameObject root = new GameObject("RPG Chests");

        // Positions deliberately align with the safe floor route, not on elevated decorative blocks.
        float[] xs = { -4.5f, 8.5f, 22.5f };
        for (int i = 0; i < xs.Length; i++)
        {
            GameObject chest = new GameObject($"Treasure Chest L{level}-{i + 1}");
            chest.transform.SetParent(root.transform);
            chest.transform.position = new Vector3(xs[i], -2.55f, 0);
            chest.transform.localScale = Vector3.one * .8f;
            SpriteRenderer renderer = chest.AddComponent<SpriteRenderer>();
            renderer.sprite = closed; renderer.sortingOrder = 14;
            BoxCollider2D trigger = chest.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true; trigger.size = new Vector2(1.5f, 1.25f); trigger.offset = new Vector2(0, .35f);
            ChestController controller = chest.AddComponent<ChestController>();
            controller.Configure($"L{level}_Chest_{i + 1}", (level + i) % 4, closed, opened, rewardIcons);
        }
        InstallManaHud(inventoryFrame, LoadSprite("Assets/Art/Icons/Skills/PNG/1.png"), rewardIcons);
    }

    private static void InstallManaHud(Sprite inventoryFrame, Sprite fireballIcon, Sprite[] rewardIcons)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;
        Transform existing = canvas.transform.Find("Mana");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
        GameObject go = new GameObject("Mana", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(PlayerMagicHud));
        go.transform.SetParent(canvas.transform, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1); rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1); rect.anchoredPosition = new Vector2(-36, -84); rect.sizeDelta = new Vector2(500, 42);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = 24; text.alignment = TextAlignmentOptions.Right; text.color = new Color(0.35f, 0.85f, 1f);
        go.GetComponent<PlayerMagicHud>().Configure(text);
        InstallSkillSlot(canvas, fireballIcon);
        InstallStatIcons(canvas, rewardIcons);
        InstallInventory(canvas, inventoryFrame, rewardIcons);
    }

    private static void InstallStatIcons(Canvas canvas, Sprite[] icons)
    {
        InstallStatIcon(canvas, "HP Icon", icons != null && icons.Length > 3 ? icons[3] : null, new Vector2(0, 1), new Vector2(28, -32));
        InstallStatIcon(canvas, "Ember Icon", icons != null && icons.Length > 1 ? icons[1] : null, new Vector2(1, 1), new Vector2(-205, -33));
        InstallStatIcon(canvas, "Kills Icon", icons != null && icons.Length > 2 ? icons[2] : null, new Vector2(1, 1), new Vector2(-202, -73));
    }

    private static void InstallStatIcon(Canvas canvas, string name, Sprite iconSprite, Vector2 anchor, Vector2 position)
    {
        Transform old = canvas.transform.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); go.transform.SetParent(canvas.transform, false);
        RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor.x > .5f ? new Vector2(1, 1) : new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(30, 30);
        UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>(); image.sprite = iconSprite; image.preserveAspect = true; image.color = Color.white;
    }

    private static void InstallSkillSlot(Canvas canvas, Sprite fireballIcon)
    {
        Transform old = canvas.transform.Find("Fireball Skill Slot");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject root = new GameObject("Fireball Skill Slot", typeof(RectTransform), typeof(SkillIconHud));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.GetComponent<RectTransform>(); rootRect.anchorMin = rootRect.anchorMax = new Vector2(1, 1); rootRect.pivot = new Vector2(1, 1); rootRect.anchoredPosition = new Vector2(-36, -166); rootRect.sizeDelta = new Vector2(112, 116);
        Image frame = root.AddComponent<UnityEngine.UI.Image>(); frame.color = new Color(.08f, .12f, .18f, .96f);
        GameObject iconObject = new GameObject("Skill Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image)); iconObject.transform.SetParent(root.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>(); iconRect.anchorMin = new Vector2(.5f, .5f); iconRect.anchorMax = new Vector2(.5f, .5f); iconRect.pivot = new Vector2(.5f, .5f); iconRect.anchoredPosition = new Vector2(0, 15); iconRect.sizeDelta = new Vector2(62, 62);
        Image icon = iconObject.GetComponent<UnityEngine.UI.Image>(); icon.sprite = fireballIcon; icon.preserveAspect = true;
        GameObject cooldownObject = new GameObject("Cooldown", typeof(RectTransform), typeof(UnityEngine.UI.Image)); cooldownObject.transform.SetParent(iconObject.transform, false);
        RectTransform cooldownRect = cooldownObject.GetComponent<RectTransform>(); cooldownRect.anchorMin = Vector2.zero; cooldownRect.anchorMax = Vector2.one; cooldownRect.offsetMin = cooldownRect.offsetMax = Vector2.zero;
        Image cooldown = cooldownObject.GetComponent<UnityEngine.UI.Image>(); cooldown.sprite = fireballIcon; cooldown.type = UnityEngine.UI.Image.Type.Filled; cooldown.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360; cooldown.fillClockwise = false; cooldown.color = new Color(.02f, .04f, .08f, .78f);
        GameObject textObject = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI)); textObject.transform.SetParent(root.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>(); textRect.anchorMin = new Vector2(0, 0); textRect.anchorMax = new Vector2(1, 0); textRect.pivot = new Vector2(.5f, 0); textRect.anchoredPosition = new Vector2(0, 8); textRect.sizeDelta = new Vector2(0, 28);
        TextMeshProUGUI caption = textObject.GetComponent<TextMeshProUGUI>(); caption.fontSize = 17; caption.color = new Color(1f, .78f, .26f); caption.alignment = TextAlignmentOptions.Center;
        root.GetComponent<SkillIconHud>().Configure(icon, cooldown, caption);
    }

    private static void InstallInventory(Canvas canvas, Sprite inventoryFrame, Sprite[] rewardIcons)
    {
        Transform old = canvas.transform.Find("RPG Inventory Controller");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject controller = new GameObject("RPG Inventory Controller", typeof(RpgInventoryUI));
        controller.transform.SetParent(canvas.transform, false);
        GameObject panel = new GameObject("Inventory Panel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        panel.transform.SetParent(controller.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f); panelRect.pivot = new Vector2(.5f, .5f);
        panelRect.sizeDelta = new Vector2(600, 390);
        UnityEngine.UI.Image backdrop = panel.GetComponent<UnityEngine.UI.Image>();
        backdrop.color = new Color(.035f, .07f, .12f, .96f);
        if (inventoryFrame != null) { backdrop.sprite = inventoryFrame; backdrop.type = UnityEngine.UI.Image.Type.Sliced; }

        GameObject body = new GameObject("Inventory Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        body.transform.SetParent(panel.transform, false);
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0, 0); bodyRect.anchorMax = new Vector2(1, 1);
        bodyRect.offsetMin = new Vector2(52, 208); bodyRect.offsetMax = new Vector2(-52, -38);
        TextMeshProUGUI text = body.GetComponent<TextMeshProUGUI>();
        text.fontSize = 21; text.alignment = TextAlignmentOptions.TopLeft; text.color = new Color(1f, .83f, .4f); text.enableWordWrapping = true;
        Image[] iconViews = new Image[5]; TMP_Text[] captions = new TMP_Text[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject slot = new GameObject($"Equipment Slot {i + 1}", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            slot.transform.SetParent(panel.transform, false);
            RectTransform slotRect = slot.GetComponent<RectTransform>(); slotRect.anchorMin = slotRect.anchorMax = new Vector2(.5f, .5f); slotRect.pivot = new Vector2(.5f, .5f); slotRect.anchoredPosition = new Vector2(-208 + i * 104, 15); slotRect.sizeDelta = new Vector2(86, 108);
            slot.GetComponent<UnityEngine.UI.Image>().color = new Color(.15f, .1f, .05f, .95f);
            GameObject icon = new GameObject("CraftPix Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image)); icon.transform.SetParent(slot.transform, false);
            RectTransform iconRect = icon.GetComponent<RectTransform>(); iconRect.anchorMin = iconRect.anchorMax = new Vector2(.5f, .62f); iconRect.pivot = new Vector2(.5f, .5f); iconRect.sizeDelta = new Vector2(58, 58);
            Image image = icon.GetComponent<UnityEngine.UI.Image>(); image.sprite = rewardIcons != null && i < rewardIcons.Length ? rewardIcons[i] : null; image.preserveAspect = true; iconViews[i] = image;
            GameObject captionObject = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI)); captionObject.transform.SetParent(slot.transform, false);
            RectTransform captionRect = captionObject.GetComponent<RectTransform>(); captionRect.anchorMin = new Vector2(0, 0); captionRect.anchorMax = new Vector2(1, 0); captionRect.pivot = new Vector2(.5f, 0); captionRect.anchoredPosition = new Vector2(0, 5); captionRect.sizeDelta = new Vector2(-8, 32);
            TextMeshProUGUI caption = captionObject.GetComponent<TextMeshProUGUI>(); caption.fontSize = 11; caption.alignment = TextAlignmentOptions.Center; caption.color = new Color(1f, .78f, .26f); captions[i] = caption;
        }
        controller.GetComponent<RpgInventoryUI>().Configure(panel, text, iconViews, captions);

        GameObject hint = new GameObject("Inventory Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
        hint.transform.SetParent(canvas.transform, false);
        RectTransform hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = hintRect.anchorMax = new Vector2(1, 1); hintRect.pivot = new Vector2(1, 1);
        hintRect.anchoredPosition = new Vector2(-36, -126); hintRect.sizeDelta = new Vector2(500, 36);
        TextMeshProUGUI hintText = hint.GetComponent<TextMeshProUGUI>();
        hintText.text = "[I] INVENTORY"; hintText.fontSize = 20; hintText.alignment = TextAlignmentOptions.Right; hintText.color = new Color(1f, .72f, .25f);
    }

    private static int GetLevelNumber(string path)
    {
        for (int i = 1; i <= 5; i++) if (path.Contains($"Level0{i}_")) return i;
        return 1;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        if (sprites.Length > 0) return sprites[0];
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite[] LoadRewardIcons()
    {
        // First icons from CraftPix Free 40 Loot Icons: used consistently for spell,
        // currency, weapon, armour and relic in the generated campaign.
        string[] paths =
        {
            "Assets/Art/Icons/Skills/PNG/1.png",
            "Assets/Art/Icons/Loot/2 Icons with back/Icons_02.png",
            "Assets/Art/Icons/Loot/2 Icons with back/Icons_03.png",
            "Assets/Art/Icons/Loot/2 Icons with back/Icons_04.png",
            "Assets/Art/Icons/Loot/2 Icons with back/Icons_05.png"
        };
        Sprite[] results = new Sprite[paths.Length];
        for (int i = 0; i < paths.Length; i++) results[i] = LoadSprite(paths[i]);
        return results;
    }

    private static void NormalizeIconSprites()
    {
        string[] ids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Icons" });
        foreach (string id in ids)
        {
            string path = AssetDatabase.GUIDToAssetPath(id);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            bool changed = importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled;
            if (!changed) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}
#endif

