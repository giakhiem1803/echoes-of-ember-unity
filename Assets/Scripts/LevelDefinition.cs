using UnityEngine;

[CreateAssetMenu(fileName = "LevelDefinition", menuName = "Echoes of Ember/Level Definition")]
public sealed class LevelDefinition : ScriptableObject
{
    [Header("Identity")]
    public int order;
    public string sceneName;
    public string displayName;
    [TextArea] public string objective;

    [Header("Difficulty")]
    [Min(1)] public int enemyHealth = 1;
    [Min(0.1f)] public float enemySpeed = 1f;
    [Min(1)] public int enemyDamage = 1;
    [Min(0)] public int expectedEnemyCount;
    [Min(0)] public int heartPickupCount;

    [Header("CraftPix art plan")]
    public string primaryTileset;
    public string backgroundPath;
    public string enemyPlan;
    public Color uiAccent = Color.white;
}
