#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds real Spell Book and Quest Book overlays from the Free Animated Magic Book pack.</summary>
public static class MagicBookUiInstaller
{
    private static readonly string[] Scenes =
    {
        "Assets/Scenes/Level01_EmberRuins.unity", "Assets/Scenes/Level02_CrystalDepths.unity",
        "Assets/Scenes/Level03_AshenForge.unity", "Assets/Scenes/Level04_ShadowCitadel.unity",
        "Assets/Scenes/Level05_EmberThrone.unity"
    };

    [MenuItem("Echoes of Ember/Install Magic Book UI (B Spellbook, Q Quests)")]
    public static void Install()
    {
        Sprite book = SpriteAt("Assets/Art/UI/MagicBook/PNG/Open_book_bookmarks1.png");
        Sprite content = SpriteAt("Assets/Art/UI/MagicBook/PNG/book_content.png");
        if (book == null) { EditorUtility.DisplayDialog("Echoes of Ember", "Magic Book assets not found. Import the CraftPix ZIP into Assets/Art/UI/MagicBook first.", "OK"); return; }
        int count = 0;
        foreach (string scenePath in Scenes)
        {
            if (!System.IO.File.Exists(scenePath)) continue;
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            InstallScene(book, content);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            count++;
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Echoes of Ember", $"Magic Book UI installed in {count} levels.\nB = Spellbook | Q = Quest Log | Esc = Close", "Open Level 01");
        EditorSceneManager.OpenScene(Scenes[0], OpenSceneMode.Single);
    }

    private static void InstallScene(Sprite book, Sprite content)
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        Transform old = canvas.transform.Find("CraftPix Books"); if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("CraftPix Books", typeof(RectTransform)); root.transform.SetParent(canvas.transform, false);
        var spell = CreateBook(root.transform, "SPELL BOOK", book, content, new Color(1f,.72f,.23f));
        var quest = CreateBook(root.transform, "QUEST BOOK", book, content, new Color(.35f,.9f,.8f));
        var ui = root.AddComponent<RpgBookUI>(); ui.Configure(spell.go, quest.go, spell.text, quest.text);
    }

    private static (GameObject go, TMP_Text text) CreateBook(Transform parent, string title, Sprite book, Sprite content, Color accent)
    {
        var panel = new GameObject(title, typeof(RectTransform), typeof(Image)); panel.transform.SetParent(parent, false);
        var pr = panel.GetComponent<RectTransform>(); pr.anchorMin = pr.anchorMax = new Vector2(.5f,.5f); pr.sizeDelta = new Vector2(820,540);
        var image = panel.GetComponent<Image>(); image.sprite = book; image.preserveAspect = true; image.color = Color.white;
        var dark = new GameObject("Readable Page", typeof(RectTransform), typeof(Image)); dark.transform.SetParent(panel.transform, false);
        var dr = dark.GetComponent<RectTransform>(); dr.anchorMin = new Vector2(.13f,.18f); dr.anchorMax = new Vector2(.87f,.80f); dr.offsetMin = dr.offsetMax = Vector2.zero;
        var dimg = dark.GetComponent<Image>(); dimg.sprite = content != null ? content : null; dimg.color = new Color(.035f,.055f,.05f,.94f);
        var label = new GameObject("Page Text", typeof(RectTransform), typeof(TextMeshProUGUI)); label.transform.SetParent(dark.transform, false);
        var lr = label.GetComponent<RectTransform>(); lr.anchorMin=Vector2.zero; lr.anchorMax=Vector2.one; lr.offsetMin=new Vector2(30,22); lr.offsetMax=new Vector2(-30,-22);
        var text = label.GetComponent<TextMeshProUGUI>(); text.fontSize=24; text.enableWordWrapping=true; text.color=accent; text.alignment=TextAlignmentOptions.TopLeft;
        var hint = new GameObject("Close Hint", typeof(RectTransform), typeof(TextMeshProUGUI)); hint.transform.SetParent(panel.transform,false);
        var hr=hint.GetComponent<RectTransform>(); hr.anchorMin=new Vector2(.5f,.08f); hr.anchorMax=new Vector2(.5f,.08f); hr.sizeDelta=new Vector2(320,30);
        var ht=hint.GetComponent<TextMeshProUGUI>(); ht.text="[Esc] CLOSE"; ht.fontSize=16; ht.alignment=TextAlignmentOptions.Center; ht.color=Color.white;
        panel.SetActive(false); return (panel,text);
    }

    private static Sprite SpriteAt(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite) { importer.textureType=TextureImporterType.Sprite; importer.spritePixelsPerUnit=32; importer.filterMode=FilterMode.Point; importer.SaveAndReimport(); }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
