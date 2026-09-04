using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Persistent campaign unlocks and per-level best results.</summary>
public static class CampaignProgress
{
    private const string UnlockKey = "Echoes.UnlockedLevel";
    private const string EmberPrefix = "Echoes.BestEmber.";
    private const string TimePrefix = "Echoes.BestTime.";
    private static readonly string[] LevelScenes = { "Level01_EmberRuins", "Level02_CrystalDepths", "Level03_AshenForge", "Level04_ShadowCitadel", "Level05_EmberThrone" };

    public static int UnlockedLevel => Mathf.Clamp(PlayerPrefs.GetInt(UnlockKey, 1), 1, 5);
    public static bool IsUnlocked(int order) => order <= UnlockedLevel;
    public static bool IsUnlocked(string sceneName)
    {
        int index = System.Array.IndexOf(LevelScenes, sceneName);
        return index >= 0 && IsUnlocked(index + 1);
    }

    public static void Complete(int order, int ember, float seconds)
    {
        int current = UnlockedLevel;
        if (order >= current && order < 5) PlayerPrefs.SetInt(UnlockKey, order + 1);
        string id = order.ToString();
        PlayerPrefs.SetInt(EmberPrefix + id, Mathf.Max(ember, PlayerPrefs.GetInt(EmberPrefix + id, 0)));
        float best = PlayerPrefs.GetFloat(TimePrefix + id, 0f);
        if (best <= 0f || seconds < best) PlayerPrefs.SetFloat(TimePrefix + id, seconds);
        PlayerPrefs.Save();
    }

    public static int BestEmber(int order) => PlayerPrefs.GetInt(EmberPrefix + order, 0);
    public static float BestTime(int order) => PlayerPrefs.GetFloat(TimePrefix + order, 0f);
    public static void Complete(string sceneName, int ember)
    {
        int index = System.Array.IndexOf(LevelScenes, sceneName);
        if (index >= 0) Complete(index + 1, ember, Time.timeSinceLevelLoad);
    }
    public static string NextScene(string sceneName)
    {
        int index = System.Array.IndexOf(LevelScenes, sceneName);
        return index >= 0 && index < LevelScenes.Length - 1 ? LevelScenes[index + 1] : string.Empty;
    }
    public static void LoadSceneIfUnlocked(int order, string sceneName)
    {
        if (IsUnlocked(order)) SceneManager.LoadScene(sceneName);
    }
}
