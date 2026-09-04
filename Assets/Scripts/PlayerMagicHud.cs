using TMPro;
using UnityEngine;

/// <summary>Small HUD component created by the RPG installer. It is deliberately
/// independent from a particular scene's GameManager so every campaign scene can use it.</summary>
public sealed class PlayerMagicHud : MonoBehaviour
{
    private TMP_Text label;
    private PlayerMagic playerMagic;

    public void Configure(TMP_Text target) => label = target;

    private void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (label == null) return;
        if (playerMagic == null) playerMagic = FindAnyObjectByType<PlayerMagic>();
        if (!RpgProgression.HasFireball)
        {
            label.text = "FIREBALL: SEARCH CHESTS";
            return;
        }
        if (playerMagic == null) { label.text = "FIREBALL: READY"; return; }
        label.text = $"MANA {Mathf.CeilToInt(playerMagic.Mana)}/{Mathf.CeilToInt(playerMagic.MaxMana)}  [F] FIREBALL";
    }
}
