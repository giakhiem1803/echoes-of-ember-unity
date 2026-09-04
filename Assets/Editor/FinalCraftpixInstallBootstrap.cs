using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot bootstrap used to finish the final content pass even when the
/// editor reloaded while Play Mode was shutting down. It removes itself from
/// the update loop after a successful install and records a persistent flag.
/// </summary>
[InitializeOnLoad]
public static class FinalCraftpixInstallBootstrap
{
    private const string BootstrapKey = "Echoes.FinalCraftpixBootstrap.2026.08.26.r1";

    static FinalCraftpixInstallBootstrap()
    {
        if (EditorPrefs.GetBool(BootstrapKey, false)) return;
        EditorApplication.update -= RunWhenReady;
        EditorApplication.update += RunWhenReady;
        Debug.Log("Echoes of Ember: final CraftPix bootstrap is waiting for Edit Mode.");
    }

    private static void RunWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.ExitPlaymode();
            return;
        }

        EditorApplication.update -= RunWhenReady;
        FinalCraftpixProjectInstaller.Install();
        EditorPrefs.SetBool(BootstrapKey, true);
        Debug.Log("Echoes of Ember: final CraftPix bootstrap completed.");
    }
}
