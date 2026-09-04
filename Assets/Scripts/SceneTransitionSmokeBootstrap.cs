using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Test-only bootstrap used by a diagnostic player build. It verifies the same
/// runtime SceneManager transition used by the Campaign Hub without modifying
/// any submission scene.
/// </summary>
public sealed class SceneTransitionSmokeBootstrap : MonoBehaviour
{
    [SerializeField] private string targetScene = "Level01_EmberRuins";
    [SerializeField] private float loadDelay = 0.5f;
    [SerializeField] private float verificationDelay = 4f;

    private IEnumerator Start()
    {
        // Diagnostic bootstrap must never affect the normal game or submitted build.
        // Run it only when explicitly requested with: -echoes-smoke
        bool smokeRequested = false;
        foreach (string arg in System.Environment.GetCommandLineArgs())
        {
            if (arg == "-echoes-smoke")
            {
                smokeRequested = true;
                break;
            }
        }
        if (!Application.isBatchMode || !smokeRequested)
            yield break;

        DontDestroyOnLoad(gameObject);
        Debug.Log("ECHOES TRANSITION SMOKE: bootstrap started");
        yield return new WaitForSecondsRealtime(loadDelay);

        AsyncOperation load = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
        if (load == null)
        {
            Debug.LogError("ECHOES TRANSITION SMOKE FAILED: LoadSceneAsync returned null");
            Application.Quit(21);
            yield break;
        }

        while (!load.isDone)
            yield return null;

        Debug.Log("ECHOES TRANSITION SMOKE: loaded " + SceneManager.GetActiveScene().name);
        yield return new WaitForSecondsRealtime(verificationDelay);

        bool correctScene = SceneManager.GetActiveScene().name == targetScene;
        bool hasCamera = Camera.main != null;
        if (correctScene && hasCamera)
        {
            Debug.Log("ECHOES TRANSITION SMOKE SUCCESS: Campaign-style transition to Level01 is stable");
            Application.Quit(0);
        }
        else
        {
            Debug.LogError($"ECHOES TRANSITION SMOKE FAILED: scene={SceneManager.GetActiveScene().name}, camera={hasCamera}");
            Application.Quit(22);
        }
    }
}
