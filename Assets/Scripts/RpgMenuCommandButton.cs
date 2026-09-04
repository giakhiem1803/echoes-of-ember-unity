using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class RpgMenuCommandButton : MonoBehaviour
{
    [SerializeField] private RpgMenuCommand command;

    public RpgMenuCommand Command => command;

    public void Configure(RpgMenuCommand value)
    {
        command = value;
    }

    private void Awake()
    {
        Button button = GetComponent<Button>();
        button.onClick.RemoveListener(Invoke);
        button.onClick.AddListener(Invoke);
    }

    public void Invoke()
    {
        CompleteRpgMenuController.Instance?.Run(command);
    }
}
