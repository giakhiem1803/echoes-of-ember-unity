using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runtime-safe campaign selector. This component must live outside an Editor
/// folder so it is included in Windows, Android and WebGL players.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class CampaignSceneButton : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public void Configure(string value) => sceneName = value;

    private void Awake()
    {
        Button button = GetComponent<Button>();
        button.onClick.RemoveListener(Load);
        button.onClick.AddListener(Load);
        button.interactable = CampaignProgress.IsUnlocked(sceneName);
    }

    public void Load()
    {
        if (string.IsNullOrWhiteSpace(sceneName) || !CampaignProgress.IsUnlocked(sceneName))
            return;

        SceneManager.LoadScene(sceneName);
    }
}
