using UnityEngine;

/// <summary>ChestController keeps this compatibility entry point; the polished UI owns the modal.</summary>
public static class RpgLootPopup
{
    public static void Show(Sprite icon, string title) => RpgUiController.Instance?.ShowLoot(icon, title);
}
