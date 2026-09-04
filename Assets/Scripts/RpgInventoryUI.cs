using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Keyboard inventory for reviewing chest rewards. This is intentionally
/// read-only: equipment is automatically equipped when discovered, which keeps the
/// platformer controls focused while still demonstrating a persistent RPG layer.</summary>
public sealed class RpgInventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text content;
    [SerializeField] private Image[] itemIcons;
    [SerializeField] private TMP_Text[] itemCaptions;

    public void Configure(GameObject panelObject, TMP_Text contentText, Image[] icons = null, TMP_Text[] captions = null)
    {
        panel = panelObject;
        content = contentText;
        itemIcons = icons;
        itemCaptions = captions;
    }

    private void Start() { if (panel != null) panel.SetActive(false); }
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.iKey.wasPressedThisFrame || panel == null) return;
        bool show = !panel.activeSelf;
        panel.SetActive(show);
        if (show) Refresh();
    }

    private void Refresh()
    {
        if (content == null) return;
        string fireball = RpgProgression.HasFireball ? "FIREBALL SCROLL  [F]" : "Fireball Scroll  —  not found";
        string blade = RpgProgression.DamageBonus > 0 ? "EMBER BLADE  +1 DAMAGE" : "Ember Blade  —  not found";
        string armor = RpgProgression.HealthBonus > 0 ? "CINDER MAIL  +2 MAX HP" : "Cinder Mail  —  not found";
        string relic = RpgProgression.SpeedBonus > 0 ? "WIND RELIC  + MOVE SPEED" : "Wind Relic  —  not found";
        content.text = "INVENTORY & EQUIPMENT";
        bool[] owned = { RpgProgression.HasFireball, RpgProgression.DamageBonus > 0, RpgProgression.HealthBonus > 0, RpgProgression.SpeedBonus > 0, true };
        string[] names = { "FIREBALL", "EMBER BLADE", "CINDER MAIL", "WIND RELIC", "EMBER" };
        for (int i = 0; itemIcons != null && i < itemIcons.Length; i++)
        {
            if (itemIcons[i] != null) itemIcons[i].color = owned[i] ? Color.white : new Color(.13f, .13f, .16f, .92f);
            if (itemCaptions != null && i < itemCaptions.Length && itemCaptions[i] != null)
                itemCaptions[i].text = owned[i] ? names[i] : "LOCKED";
        }
        content.text += $"\n\n{fireball}\n{blade}\n{armor}\n{relic}\n\nPress I to close";
    }
}
