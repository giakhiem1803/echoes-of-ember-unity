using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Visual Fireball slot: icon, lock state, radial cooldown and mana label.</summary>
public sealed class SkillIconHud : MonoBehaviour
{
    private Image icon;
    private Image cooldown;
    private TMP_Text caption;
    private PlayerMagic magic;

    public void Configure(Image iconImage, Image cooldownImage, TMP_Text captionText)
    {
        icon = iconImage; cooldown = cooldownImage; caption = captionText;
    }
    private void Update()
    {
        if (magic == null) magic = FindAnyObjectByType<PlayerMagic>();
        bool unlocked = RpgProgression.HasFireball;
        if (icon != null) icon.color = unlocked ? Color.white : new Color(.18f, .18f, .2f, .85f);
        if (cooldown != null)
        {
            cooldown.enabled = unlocked && magic != null && magic.Cooldown01 > 0.01f;
            cooldown.fillAmount = magic == null ? 0f : magic.Cooldown01;
        }
        if (caption != null)
        {
            caption.text = !unlocked ? "LOCKED" : magic == null ? "F  FIREBALL" : $"F  {Mathf.CeilToInt(magic.Mana)}";
        }
    }
}
