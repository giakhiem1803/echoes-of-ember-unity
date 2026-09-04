using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum RpgMenuCommand
{
    Close, Resume, Restart, CampaignHub, Inventory, Crafting, Settings, Levels,
    CraftBlade, CraftArmor, CraftRelic, NextLevel, LoadLevel1, LoadLevel2, LoadLevel3, LoadLevel4, LoadLevel5
}

public sealed class CompleteRpgMenuController : MonoBehaviour
{
    public static CompleteRpgMenuController Instance { get; private set; }
    [SerializeField] private GameObject pausePanel, craftingPanel, settingsPanel, levelsPanel;
    [SerializeField] private TextMeshProUGUI statusLabel;
    private GameObject openPanel;

    public void Configure(GameObject pause, GameObject crafting, GameObject settings, GameObject levels, TextMeshProUGUI status)
    {
        pausePanel = pause; craftingPanel = crafting; settingsPanel = settings; levelsPanel = levels; statusLabel = status;
        CloseAll(false);
    }
    private void Awake() => Instance = this;
    private void Update()
    {
        Keyboard key = Keyboard.current;
        if (key == null) return;
        if (key.cKey.wasPressedThisFrame) Toggle(craftingPanel);
        else if (key.oKey.wasPressedThisFrame) Toggle(settingsPanel);
        else if (key.lKey.wasPressedThisFrame) Toggle(levelsPanel);
        else if (openPanel != null && key.escapeKey.wasPressedThisFrame) CloseAll(true);
    }
    public void Run(RpgMenuCommand command)
    {
        switch (command)
        {
            case RpgMenuCommand.Close: case RpgMenuCommand.Resume: CloseAll(true); break;
            case RpgMenuCommand.Restart: Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); break;
            case RpgMenuCommand.CampaignHub: Time.timeScale = 1f; SceneManager.LoadScene("CampaignHub"); break;
            case RpgMenuCommand.Inventory: CloseAll(false); RpgUiController.Instance?.OpenInventory(); break;
            case RpgMenuCommand.Crafting: Toggle(craftingPanel); break;
            case RpgMenuCommand.Settings: Toggle(settingsPanel); break;
            case RpgMenuCommand.Levels: Toggle(levelsPanel); break;
            case RpgMenuCommand.CraftBlade: Craft(60, "Ember Blade crafted", RpgProgression.GrantEmberBlade); break;
            case RpgMenuCommand.CraftArmor: Craft(80, "Cinder Mail crafted", RpgProgression.GrantCinderMail); break;
            case RpgMenuCommand.CraftRelic: Craft(100, "Wind Relic crafted", RpgProgression.GrantWindRelic); break;
            case RpgMenuCommand.NextLevel: Time.timeScale = 1f; GameManager.Instance?.NextLevel(); break;
            default: LoadLevel((int)command - (int)RpgMenuCommand.LoadLevel1 + 1); break;
        }
    }
    private void Craft(int cost, string success, System.Action reward)
    {
        if (GameManager.Instance == null || !GameManager.Instance.TrySpendScore(cost)) { SetStatus($"Not enough Ember — need {cost}"); return; }
        reward(); EchoesAudioManager.Play(EchoesSfx.Chest); SetStatus(success);
    }
    private void LoadLevel(int level)
    {
        level = Mathf.Clamp(level, 1, 5); Time.timeScale = 1f;
        SceneManager.LoadScene(level switch { 1 => "Level01_EmberRuins", 2 => "Level02_CrystalDepths", 3 => "Level03_AshenForge", 4 => "Level04_ShadowCitadel", _ => "Level05_EmberThrone" });
    }
    private void Toggle(GameObject target)
    {
        if (target == null) return;
        if (openPanel == target) { CloseAll(true); return; }
        CloseAll(false); target.SetActive(true); openPanel = target; Time.timeScale = 0f;
    }
    public void OpenPause() => Toggle(pausePanel);
    public void CloseAll(bool resume)
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (craftingPanel != null) craftingPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (levelsPanel != null) levelsPanel.SetActive(false);
        openPanel = null; if (resume) Time.timeScale = 1f;
    }
    private void SetStatus(string text) { if (statusLabel != null) statusLabel.text = text; GameManager.Instance?.ShowMessage(text); }
}

public enum EchoesVolumeChannel { Music, Sfx }
