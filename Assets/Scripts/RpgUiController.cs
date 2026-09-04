using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Owns the polished RPG HUD and modal books.  All time pauses are
/// centralized here so inventory, spells, quest and loot never fight each other.</summary>
public sealed class RpgUiController : MonoBehaviour
{
    public static RpgUiController Instance { get; private set; }
    public static bool ModalOpen => Instance != null && Instance.modal != null;

    private PlayerHealth player;
    private PlayerMagic magic;
    private GameObject inventory, spellBook, questBook, lootPopup;
    private TextMeshProUGUI hp, mana, ember, kills, fireballState, questText, lootTitle, lootDescription;
    private Image fireballIcon, fireballCooldown, lootIcon;
    private Image[] inventoryIcons;
    private TextMeshProUGUI[] inventoryNames;
    private GameObject modal;
    private float nextRefresh;

    public void Configure(PlayerHealth hero, PlayerMagic playerMagic,
        GameObject inventoryPanel, GameObject spellPanel, GameObject questPanel, GameObject lootPanel,
        TextMeshProUGUI hpText, TextMeshProUGUI manaText, TextMeshProUGUI emberText, TextMeshProUGUI killsText,
        TextMeshProUGUI fireballText, TextMeshProUGUI questProgress, TextMeshProUGUI popupTitle, TextMeshProUGUI popupDescription,
        Image fireball, Image cooldown, Image popupIcon, Image[] itemIcons, TextMeshProUGUI[] itemNames)
    {
        Instance = this; player = hero; magic = playerMagic; inventory = inventoryPanel; spellBook = spellPanel; questBook = questPanel; lootPopup = lootPanel;
        hp = hpText; mana = manaText; ember = emberText; kills = killsText; fireballState = fireballText; questText = questProgress;
        lootTitle = popupTitle; lootDescription = popupDescription; fireballIcon = fireball; fireballCooldown = cooldown; lootIcon = popupIcon;
        inventoryIcons = itemIcons; inventoryNames = itemNames;
        CloseAllImmediate();
    }

    private void Awake() => Instance = this;
    private void Update()
    {
        if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .08f; Refresh(); }
        Keyboard key = Keyboard.current;
        if (key == null || GameManager.Instance == null) return;
        if (key.iKey.wasPressedThisFrame) Toggle(inventory);
        else if (key.bKey.wasPressedThisFrame) Toggle(spellBook);
        else if (key.qKey.wasPressedThisFrame) Toggle(questBook);
        else if (modal != null && (key.escapeKey.wasPressedThisFrame || (modal == lootPopup && key.eKey.wasPressedThisFrame))) CloseModal();
    }

    private void Refresh()
    {
        if (player != null && hp != null) hp.text = $"{player.CurrentHealth}/{player.MaxHealth}";
        if (magic != null && mana != null) mana.text = $"{Mathf.CeilToInt(magic.Mana)}/{Mathf.CeilToInt(magic.MaxMana)}";
        if (ember != null && GameManager.Instance != null) ember.text = GameManager.Instance.Score.ToString();
        if (kills != null && GameManager.Instance != null) kills.text = GameManager.Instance.Kills.ToString();
        bool unlocked = RpgProgression.HasFireball;
        if (fireballState != null) fireballState.text = unlocked ? "F  FIREBALL  25" : "F  LOCKED";
        if (fireballIcon != null) fireballIcon.color = unlocked ? Color.white : new Color(.25f,.25f,.25f,.9f);
        if (fireballCooldown != null && magic != null) fireballCooldown.fillAmount = magic.Cooldown01;
        RefreshInventory(unlocked);
        if (questText != null)
        {
            int enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Length;
            questText.text = $"MAIN QUEST\nReach the Ember Gate\n\nEnemies remaining: {enemies}\nChests opened: {GameManager.Instance?.ChestCount ?? 0}\nGate: {(enemies == 0 ? "RESTORED" : "LOCKED")}";
        }
    }

    private void RefreshInventory(bool fireball)
    {
        bool[] owned = { fireball, RpgProgression.HasEmberBlade, RpgProgression.HasCinderMail, RpgProgression.HasWindRelic, true };
        if (inventoryIcons != null) for (int i = 0; i < inventoryIcons.Length; i++)
            if (inventoryIcons[i] != null) inventoryIcons[i].color = owned[Mathf.Min(i, owned.Length - 1)] ? Color.white : new Color(.22f,.22f,.22f,.95f);
        if (inventoryNames != null) for (int i = 0; i < inventoryNames.Length; i++)
            if (inventoryNames[i] != null && i < owned.Length && !owned[i]) inventoryNames[i].text = "LOCKED";
    }

    private void Toggle(GameObject target)
    {
        if (target == null) return;
        if (modal == target) { CloseModal(); return; }
        if (modal != null) CloseModal();
        modal = target; modal.SetActive(true); Time.timeScale = 0f;
    }
    public void OpenInventory() => Open(inventory);
    public void OpenSpellBook() => Open(spellBook);
    public void OpenQuestBook() => Open(questBook);
    private void Open(GameObject target)
    {
        if (target == null) return;
        if (modal != null) modal.SetActive(false);
        modal = target; modal.SetActive(true); Time.timeScale = 0f;
        Refresh();
    }
    public void CloseModal() { if (modal != null) modal.SetActive(false); modal = null; Time.timeScale = 1f; }
    public void CloseAllImmediate()
    {
        if (inventory != null) inventory.SetActive(false); if (spellBook != null) spellBook.SetActive(false);
        if (questBook != null) questBook.SetActive(false); if (lootPopup != null) lootPopup.SetActive(false); modal = null;
    }
    public void ShowLoot(Sprite icon, string title)
    {
        if (lootPopup == null) return;
        if (lootIcon != null) { lootIcon.sprite = icon; lootIcon.color = Color.white; }
        if (lootTitle != null) lootTitle.text = title;
        if (lootDescription != null) lootDescription.text = "A relic has been added to your inventory.\nPress E or click CONTINUE.";
        Toggle(lootPopup);
    }
}
