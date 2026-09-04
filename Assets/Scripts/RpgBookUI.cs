using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Interactive CraftPix book UI. B opens the spell book and Q opens the quest book.
/// It reads the real progression state instead of presenting decorative, inactive pages.
/// </summary>
public sealed class RpgBookUI : MonoBehaviour
{
    [SerializeField] private GameObject spellBook;
    [SerializeField] private GameObject questBook;
    [SerializeField] private TMP_Text spellPage;
    [SerializeField] private TMP_Text questPage;

    public void Configure(GameObject spells, GameObject quests, TMP_Text spellsText, TMP_Text questsText)
    {
        spellBook = spells;
        questBook = quests;
        spellPage = spellsText;
        questPage = questsText;
    }

    private void Start()
    {
        if (spellBook != null) spellBook.SetActive(false);
        if (questBook != null) questBook.SetActive(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.bKey.wasPressedThisFrame) Toggle(spellBook, questBook, RefreshSpells);
        if (keyboard.qKey.wasPressedThisFrame) Toggle(questBook, spellBook, RefreshQuests);
        if (keyboard.escapeKey.wasPressedThisFrame && IsBookOpen()) CloseAll();
    }

    private void Toggle(GameObject target, GameObject other, System.Action refresh)
    {
        if (target == null) return;
        bool open = !target.activeSelf;
        if (other != null) other.SetActive(false);
        target.SetActive(open);
        if (open) refresh();
    }

    private bool IsBookOpen() => (spellBook != null && spellBook.activeSelf) || (questBook != null && questBook.activeSelf);
    public void CloseAll()
    {
        if (spellBook != null) spellBook.SetActive(false);
        if (questBook != null) questBook.SetActive(false);
    }

    private void RefreshSpells()
    {
        if (spellPage == null) return;
        string fireball = RpgProgression.HasFireball
            ? "FIREBALL  [F]\nCost: 25 Mana    Damage: 2\nStatus: UNLOCKED"
            : "FIREBALL  [F]\nFind the Fireball Scroll\nStatus: LOCKED";
        spellPage.text = "SPELLBOOK\n\n" + fireball + "\n\nDEFEND  [K]\nHold your stance to reduce danger.\nStatus: READY\n\nPress B or Esc to close";
    }

    private void RefreshQuests()
    {
        if (questPage == null) return;
        int enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Length;
        string enemyLine = enemies > 0 ? $"Defeat or evade enemies: {enemies} remain" : "Area cleared: no enemies remain";
        questPage.text = "EMBER QUEST LOG\n\nMAIN QUEST\nRestore the Ember Gate\nTravel to the gate at the end of this area.\n\nOPTIONAL\n" + enemyLine + "\nOpen treasure chests to gain relics.\n\nPress Q or Esc to close";
    }
}
