#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Creates reusable campaign definitions from the imported CraftPix library.</summary>
public static class CampaignContentSetup
{
    private const string DataDirectory = "Assets/GameData/Levels";

    [MenuItem("Echoes of Ember/Create Campaign Definitions", priority = 5)]
    public static void CreateDefinitions()
    {
        Directory.CreateDirectory(DataDirectory);
        Create(1, "Level01_EmberRuins", "Ember Ruins", "Reach the Ember Gate and restore the first flame.", 1, 1.2f, 1, 4, 2, "Dungeon Platformer Tileset", "Assets/Art/Tilesets/Dungeon/PNG/Background/Pale/Background.png", "Skeleton Scout, Skeleton Guard, Fire Spirit", new Color(1f, .55f, .16f));
        Create(2, "Level02_CrystalDepths", "Crystal Depths", "Cross the crystal caverns and cleanse the ghostlight.", 1, 1.45f, 1, 6, 2, "Dungeon tiles + Crystal Cave Backgrounds", "Assets/Art/Backgrounds/CrystalCave/background 2/background 2.png", "Ghost, Skeleton Scout", new Color(.25f, .85f, 1f));
        Create(3, "Level03_AshenForge", "Ashen Forge", "Survive the molten forge and break the ember seals.", 2, 1.7f, 1, 8, 1, "Dungeon lava tiles + Dungeon Objects", "Assets/Art/Backgrounds/Battlegrounds/Battleground2/Pale/Battleground2.png", "Skeleton Warrior, Fire Spirit", new Color(1f, .27f, .08f));
        Create(4, "Level04_ShadowCitadel", "Shadow Citadel", "Defeat the arena guard and open the citadel gate.", 2, 1.95f, 1, 10, 1, "Medieval Tileset", "Assets/Art/Backgrounds/Battlegrounds/Battleground2/Pale/bg.png", "Ghost, Skeleton Warrior", new Color(.66f, .36f, 1f));
        Create(5, "Level05_EmberThrone", "Ember Throne", "Defeat the throne guardian and awaken the final gate.", 3, 2.2f, 1, 1, 1, "Medieval + Dungeon + Battlegrounds", "Assets/Art/Backgrounds/Battlegrounds/Battleground2/Pale/Battleground2.png", "Ruins Guardian Boss", new Color(1f, .7f, .16f));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Echoes of Ember: five CraftPix campaign definitions created.");
    }

    private static void Create(int order, string scene, string title, string objective, int health, float speed, int damage, int enemies, int hearts, string tileset, string background, string enemiesText, Color accent)
    {
        string path = $"{DataDirectory}/{order:00}_{scene}.asset";
        LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
        if (level == null) { level = ScriptableObject.CreateInstance<LevelDefinition>(); AssetDatabase.CreateAsset(level, path); }
        level.order = order; level.sceneName = scene; level.displayName = title; level.objective = objective; level.enemyHealth = health; level.enemySpeed = speed; level.enemyDamage = damage; level.expectedEnemyCount = enemies; level.heartPickupCount = hearts; level.primaryTileset = tileset; level.backgroundPath = background; level.enemyPlan = enemiesText; level.uiAccent = accent;
        EditorUtility.SetDirty(level);
    }
}
#endif
