using UnityEngine;

/// <summary>Small persistent inventory designed for an action-platformer.
/// It intentionally stores only earned rewards rather than scene references.</summary>
public static class RpgProgression
{
    private const string FireballKey = "Echoes.Skill.Fireball";
    private const string WeaponKey = "Echoes.Equipment.EmberBlade";
    private const string ArmorKey = "Echoes.Equipment.CinderMail";
    private const string RelicKey = "Echoes.Equipment.WindRelic";

    public static bool HasFireball => PlayerPrefs.GetInt(FireballKey, 0) == 1;
    public static bool HasEmberBlade => PlayerPrefs.GetInt(WeaponKey, 0) == 1;
    public static bool HasCinderMail => PlayerPrefs.GetInt(ArmorKey, 0) == 1;
    public static bool HasWindRelic => PlayerPrefs.GetInt(RelicKey, 0) == 1;
    public static int DamageBonus => PlayerPrefs.GetInt(WeaponKey, 0) == 1 ? 1 : 0;
    public static int HealthBonus => PlayerPrefs.GetInt(ArmorKey, 0) == 1 ? 2 : 0;
    public static float SpeedBonus => PlayerPrefs.GetInt(RelicKey, 0) == 1 ? .65f : 0f;

    public static void UnlockFireball() => Set(FireballKey);
    public static void GrantEmberBlade() => Set(WeaponKey);
    public static void GrantCinderMail() => Set(ArmorKey);
    public static void GrantWindRelic() => Set(RelicKey);
    private static void Set(string key) { PlayerPrefs.SetInt(key, 1); PlayerPrefs.Save(); }
}

