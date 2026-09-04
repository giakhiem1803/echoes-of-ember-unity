using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One-click replacement for every legacy HUD/panel in all campaign scenes.
/// It deliberately rebuilds the Canvas instead of layering new widgets over old ones.</summary>
public static class PolishedRpgUiInstaller
{
    private static readonly string[] Levels = {
        "Level01_EmberRuins", "Level02_CrystalDepths", "Level03_AshenForge", "Level04_ShadowCitadel", "Level05_EmberThrone" };
    private const string Ui = "Assets/Art/UI/RPG/PNG/";
    private const string Book = "Assets/Art/UI/MagicBook/PNG/";
    private const string Loot = "Assets/Art/Icons/Loot/2 Icons with back/";
    private const string Magic = "Assets/Art/Effects/Magic/1 Magic/";

    [MenuItem("Echoes of Ember/REBUILD Polished CraftPix UI (All Levels)")]
    public static void RebuildAll()
    {
        if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Echoes of Ember", "Exit Play Mode before rebuilding UI.", "OK"); return; }
        foreach (string level in Levels)
        {
            string path = "Assets/Scenes/" + level + ".unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            BuildScene();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Echoes of Ember", "CraftPix HUD, hotbar, inventory, spell book, quest book and chest popup were rebuilt for Levels 01–05.", "OK");
    }

    private static void BuildScene()
    {
        // Clean only broken component references left by earlier experimental builders.
        // This prevents the old "Missing Script" warnings from polluting a submission scene.
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects()) RemoveMissingScripts(root);
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // Remove only widgets inside Canvas. World sprites, colliders and gameplay remain untouched.
        for (int i = canvas.transform.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(canvas.transform.GetChild(i).gameObject);
        foreach (MonoBehaviour script in canvas.GetComponents<MonoBehaviour>()) if (script != null && script.GetType() != typeof(Canvas)) script.enabled = false;

        Sprite panel = SpriteAt(Ui + "Action_panel.png"), slot = SpriteAt(Ui + "Inventory.png"), button = SpriteAt(Ui + "Buttons.png"), book = SpriteAt(Book + "Open_book_bookmarks1.png");
        Sprite[] loot = Enumerable.Range(1, 12).Select(i => SpriteAt(Loot + "Icons_" + i.ToString("00") + ".png")).ToArray();
        Sprite[] frames = Enumerable.Range(1, 10).Select(i => SpriteAt(Magic + i + ".png")).Where(s => s != null).ToArray();

        PlayerHealth hero = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
        PlayerMagic magic = hero == null ? null : hero.GetComponent<PlayerMagic>() ?? hero.gameObject.AddComponent<PlayerMagic>();
        if (magic != null) magic.Configure(frames);
        GameManager manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();

        // Compact, readable HUD.  The inner positions are relative to the centre of each frame;
        // this avoids the old issue where icons were anchored in the middle but text was anchored
        // at a corner, causing both to drift apart at different aspect ratios.
        // HUD deliberately stays lightweight: gameplay, not UI, owns the screen.
        GameObject left = Box(canvas.transform, "Vitals Frame", null, new Vector2(98, 36), new Vector2(12, -10), new Vector2(0, 1), new Color(.015f,.025f,.06f,.45f));
        TextMeshProUGUI hp = Stat(left.transform, "HP", loot[0], new Vector2(-39, 9), new Color(1f,.36f,.25f));
        TextMeshProUGUI mana = Stat(left.transform, "MANA", loot[1], new Vector2(-39, -9), new Color(.2f,.85f,1f));
        GameObject right = Box(canvas.transform, "Progress Frame", null, new Vector2(98, 36), new Vector2(-12, -10), new Vector2(1, 1), new Color(.015f,.025f,.06f,.45f));
        TextMeshProUGUI ember = Stat(right.transform, "EMBER", loot[2], new Vector2(-39, 9), new Color(1f,.72f,.12f));
        TextMeshProUGUI kills = Stat(right.transform, "KILLS", loot[3], new Vector2(-39, -9), Color.white);

        // Bottom hotbar always leaves the view clear.
        GameObject hotbar = new GameObject("RPG Hotbar", typeof(RectTransform)); hotbar.transform.SetParent(canvas.transform, false);
        RectTransform hb = hotbar.GetComponent<RectTransform>(); hb.anchorMin = hb.anchorMax = new Vector2(.5f,0); hb.pivot = new Vector2(.5f,0); hb.sizeDelta = new Vector2(150,38); hb.anchoredPosition = new Vector2(0,6);
        Sprite[] hotIcons = { loot[4], loot[5], loot[6], loot[7] };
        string[] hotNames = {"J\nSWORD", "K\nSHIELD", "F\nFIREBALL", "I\nBAG"};
        Image fireIcon = null, cooldown = null;
        for (int i=0;i<4;i++)
        {
            GameObject s = Box(hotbar.transform, "Hotbar Slot " + i, null, new Vector2(30,30), new Vector2(-54 + i * 36, 17), new Vector2(.5f,.5f), new Color(.025f,.04f,.09f,.72f));
            Image icon = Icon(s.transform, "Icon", hotIcons[i], new Vector2(18,18), new Vector2(0,3));
            Label(s.transform, hotNames[i].Substring(0, 1), 9, new Vector2(26,10), new Vector2(0,-11), TextAlignmentOptions.Center, new Color(1f,.8f,.32f));
            if (i == 2) { fireIcon=icon; cooldown=Icon(s.transform,"Cooldown",hotIcons[i],new Vector2(20,20),new Vector2(0,3)); cooldown.type=Image.Type.Filled; cooldown.fillMethod=Image.FillMethod.Radial360; cooldown.fillAmount=0; cooldown.color=new Color(0,0,0,.64f); }
        }

        // Inventory with actual CraftPix frame and grid/equipment sections.
        GameObject inv = ModalRoot(canvas.transform, "Inventory Window");
        GameObject invFrame = Box(inv.transform, "CraftPix Inventory Frame", slot, new Vector2(1220,690), Vector2.zero, new Vector2(.5f,.5f), Color.white);
        Header(invFrame.transform, "INVENTORY & EQUIPMENT", new Color(1f,.82f,.32f));
        Label(invFrame.transform,"BAG",24,new Vector2(420,40),new Vector2(-300,210),TextAlignmentOptions.Left,new Color(.2f,.95f,.65f));
        Label(invFrame.transform,"EQUIPMENT",24,new Vector2(350,40),new Vector2(300,210),TextAlignmentOptions.Left,new Color(.2f,.95f,.65f));
        Image[] items=new Image[5]; TextMeshProUGUI[] names=new TextMeshProUGUI[5];
        string[] itemNames={"FIREBALL SCROLL","EMBER BLADE","CINDER MAIL","WIND RELIC","EMBER"};
        for(int i=0;i<5;i++) { float x=-420+(i%3)*150, y=90-(i/3)*175; GameObject cell=Box(invFrame.transform,"Item Slot "+i,panel,new Vector2(130,145),new Vector2(x,y),new Vector2(.5f,.5f),Color.white); items[i]=Icon(cell.transform,"Item Icon",loot[(i+6)%loot.Length],new Vector2(72,72),new Vector2(65,88)); names[i]=Label(cell.transform,itemNames[i],14,new Vector2(118,46),new Vector2(65,29),TextAlignmentOptions.Center,new Color(1f,.82f,.32f)); }
        string[] equip={"WEAPON\nEmber Blade", "ARMOR\nCinder Mail", "RELIC\nWind Relic", "SKILL\nFireball"};
        for(int i=0;i<4;i++) { GameObject cell=Box(invFrame.transform,"Equipment Slot "+i,panel,new Vector2(205,100),new Vector2(300+(i%2)*220,100-(i/2)*125),new Vector2(.5f,.5f),Color.white); Icon(cell.transform,"Equip Icon",loot[(i+1)%loot.Length],new Vector2(48,48),new Vector2(44,50)); Label(cell.transform,equip[i],15,new Vector2(130,56),new Vector2(125,50),TextAlignmentOptions.Left,new Color(1f,.82f,.32f)); }
        Label(invFrame.transform,"Fireball Scroll: unlock Fireball\nEmber Blade: +1 Damage  •  Cinder Mail: +2 Max HP  •  Wind Relic: +Move Speed",15,new Vector2(1050,70),new Vector2(0,-265),TextAlignmentOptions.Center,new Color(.93f,.86f,.65f));
        AddButton(invFrame.transform,"Close",button,new Vector2(200,56),new Vector2(465,-290), "[I] CLOSE", null);

        // CraftPix magic book reused for spell and quest, with real two-page content.
        GameObject spell = BookRoot(canvas.transform,"Spell Book",book,new Color(1f,.95f,.88f));
        BuildSpellPages(spell.transform, loot[6]);
        GameObject quest = BookRoot(canvas.transform,"Quest Book",book,new Color(.78f,1f,.94f));
        TextMeshProUGUI questProgress = BuildQuestPages(quest.transform);

        // Loot popup: centered, modal and confirmable.
        GameObject popup=ModalRoot(canvas.transform,"Chest Reward Popup");
        GameObject popFrame=Box(popup.transform,"Loot Reward Frame",panel,new Vector2(680,350),Vector2.zero,new Vector2(.5f,.5f),Color.white);
        Header(popFrame.transform,"TREASURE FOUND",new Color(1f,.75f,.18f));
        Image popupIcon=Icon(popFrame.transform,"Reward Icon",loot[8],new Vector2(108,108),new Vector2(126,145));
        TextMeshProUGUI popupTitle=Label(popFrame.transform,"",26,new Vector2(390,55),new Vector2(390,170),TextAlignmentOptions.Left,new Color(1f,.8f,.25f));
        TextMeshProUGUI popupDesc=Label(popFrame.transform,"",17,new Vector2(390,105),new Vector2(390,110),TextAlignmentOptions.Left,new Color(.94f,.9f,.76f));
        AddButton(popFrame.transform,"Continue",button,new Vector2(240,55),new Vector2(400,42),"E / CLICK TO CONTINUE",null);

        GameObject uiHost = new GameObject("RPG UI Controller", typeof(RpgUiController)); uiHost.transform.SetParent(canvas.transform,false);
        RpgUiController controller=uiHost.GetComponent<RpgUiController>();
        controller.Configure(hero,magic,inv,spell,quest,popup,hp,mana,ember,kills,null,questProgress,popupTitle,popupDesc,fireIcon,cooldown,popupIcon,items,names);
        SetCloseButtons(inv,controller); SetCloseButtons(spell,controller); SetCloseButtons(quest,controller); SetCloseButtons(popup,controller);
        // Do not clear GameManager's pause/game-over/victory references.  Those
        // panels are part of the playable flow and are authored by the scene
        // builder (or repaired safely by GameManager at runtime).
        EditorUtility.SetDirty(canvas.gameObject); EditorUtility.SetDirty(uiHost);
    }

    private static GameObject ModalRoot(Transform parent,string name) { var root=new GameObject(name,typeof(RectTransform),typeof(Image)); root.transform.SetParent(parent,false); var r=root.GetComponent<RectTransform>(); r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero; root.GetComponent<Image>().color=new Color(.015f,.02f,.055f,.64f); return root; }
    private static GameObject BookRoot(Transform parent,string name,Sprite sprite,Color accent) { GameObject root=ModalRoot(parent,name); GameObject b=Box(root.transform,"Open Magic Book",sprite,new Vector2(1220,720),Vector2.zero,new Vector2(.5f,.5f),Color.white); Header(b.transform,name.ToUpperInvariant(),accent); return root; }
    private static void BuildSpellPages(Transform root,Sprite icon) { Transform b=root.GetChild(0); Label(b,"SPELLS",23,new Vector2(350,42),new Vector2(-295,170),TextAlignmentOptions.Left,new Color(.25f,.95f,1f)); Label(b,"• Fireball\n\nPress F to cast",20,new Vector2(400,150),new Vector2(-270,65),TextAlignmentOptions.Left,new Color(1f,.78f,.2f)); Icon(b,"Fireball Art",icon,new Vector2(126,126),new Vector2(250,55)); Label(b,"FIREBALL\n\nMana Cost: 25\nDamage: 2\nCooldown: 1.2s\n\nA blazing projectile that burns enemies on impact.\nFind the Fireball Scroll to unlock.",19,new Vector2(420,310),new Vector2(275,25),TextAlignmentOptions.Left,new Color(.2f,.88f,1f)); Label(b,"[B] or [ESC] CLOSE",15,new Vector2(300,30),new Vector2(300,-255),TextAlignmentOptions.Center,new Color(.35f,.95f,.8f)); }
    private static TextMeshProUGUI BuildQuestPages(Transform root) { Transform b=root.GetChild(0); Label(b,"QUEST BOOK",23,new Vector2(350,42),new Vector2(-295,170),TextAlignmentOptions.Left,new Color(.3f,1f,.8f)); Label(b,"MAIN QUEST\n\nEmbers of the Ruins",20,new Vector2(400,150),new Vector2(-275,70),TextAlignmentOptions.Left,new Color(.25f,.85f,1f)); TextMeshProUGUI q=Label(b,"",18,new Vector2(440,330),new Vector2(270,20),TextAlignmentOptions.Left,new Color(.92f,.91f,.72f)); Label(b,"[Q] or [ESC] CLOSE",15,new Vector2(300,30),new Vector2(300,-255),TextAlignmentOptions.Center,new Color(.35f,.95f,.8f)); return q; }
    private static GameObject Box(Transform parent,string name,Sprite sprite,Vector2 size,Vector2 position,Vector2 anchor,Color color) { var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor;r.pivot=anchor;r.sizeDelta=size;r.anchoredPosition=position;Image i=go.GetComponent<Image>();i.sprite=sprite;i.type=sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;i.color=color; return go; }
    private static Image Icon(Transform p,string n,Sprite s,Vector2 size,Vector2 pos) { var go=new GameObject(n,typeof(RectTransform),typeof(Image));go.transform.SetParent(p,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=pos;var i=go.GetComponent<Image>();i.sprite=s;i.preserveAspect=true;return i; }
    private static TextMeshProUGUI Label(Transform p,string value,float size,Vector2 dimensions,Vector2 position,TextAlignmentOptions align,Color color) {var go=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(p,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=dimensions;r.anchoredPosition=position;var t=go.GetComponent<TextMeshProUGUI>();t.text=value;t.fontSize=size;t.color=color;t.alignment=align;t.enableWordWrapping=true;t.raycastTarget=false;return t;}
    private static TextMeshProUGUI Stat(Transform p,string title,Sprite icon,Vector2 pos,Color color) {Icon(p,title+" Icon",icon,new Vector2(14,14),pos); Label(p,title,9,new Vector2(28,15),pos+new Vector2(21,0),TextAlignmentOptions.Left,color); return Label(p,"0",11,new Vector2(24,16),pos+new Vector2(54,0),TextAlignmentOptions.Right,color);}
    private static void Header(Transform p,string text,Color c)=>Label(p,text,30,new Vector2(900,48),new Vector2(0,270),TextAlignmentOptions.Center,c);
    private static void AddButton(Transform parent,string name,Sprite s,Vector2 size,Vector2 pos,string text,Action action) {GameObject g=Box(parent,name,s,size,pos,new Vector2(.5f,.5f),Color.white);Button b=g.AddComponent<Button>();ColorBlock cb=b.colors;cb.normalColor=Color.white;cb.highlightedColor=new Color(1f,.88f,.45f);cb.pressedColor=new Color(.75f,.45f,.16f);b.colors=cb;if(action!=null)b.onClick.AddListener(()=>action());Label(g.transform,text,16,size,Vector2.zero,TextAlignmentOptions.Center,Color.white);}
    private static void SetCloseButtons(GameObject root,RpgUiController controller){foreach(Button b in root.GetComponentsInChildren<Button>(true)) b.onClick.RemoveAllListeners();foreach(Button b in root.GetComponentsInChildren<Button>(true)) b.onClick.AddListener(controller.CloseModal);}
    private static Sprite SpriteAt(string path){return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderByDescending(s=>s.rect.width*s.rect.height).FirstOrDefault();}
    private static void RemoveMissingScripts(GameObject go)
    {
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        foreach (Transform child in go.transform) RemoveMissingScripts(child.gameObject);
    }
}
