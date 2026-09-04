using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private PlayerHealth player;
    [SerializeField] private TextMeshProUGUI hpLabel;
    [SerializeField] private TextMeshProUGUI scoreLabel;
    [SerializeField] private TextMeshProUGUI killsLabel;
    [SerializeField] private TextMeshProUGUI messageLabel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;
    private int score;
    private int kills;
    private int chestCount;
    private bool finished;
    public int Score => score;
    public int Kills => kills;
    public int ChestCount => chestCount;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        if (player == null) player = FindAnyObjectByType<PlayerHealth>();
        EnsureEssentialUi();
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }
    private void Start() => RefreshHud();
    private void Update()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;
        if (finished)
        {
            if (keyboard.rKey.wasPressedThisFrame) RestartLevel();
            if (keyboard.mKey.wasPressedThisFrame) BackToTest();
            return;
        }
        if (!RpgUiController.ModalOpen && keyboard.escapeKey.wasPressedThisFrame) TogglePause();
    }
    public void Configure(PlayerHealth hero, TextMeshProUGUI hp, TextMeshProUGUI scoreText, TextMeshProUGUI killsText, TextMeshProUGUI message, GameObject pause, GameObject over, GameObject victory)
    {
        player = hero; hpLabel = hp; scoreLabel = scoreText; killsLabel = killsText; messageLabel = message; pausePanel = pause; gameOverPanel = over; victoryPanel = victory;
    }
    public void AddScore(int value) { score += value; RefreshHud(); }
    public bool TrySpendScore(int value)
    {
        if (value <= 0) return true;
        if (score < value) { ShowMessage($"Need {value} Ember"); return false; }
        score -= value;
        RefreshHud();
        return true;
    }
    public void AddKill() { kills++; RefreshHud(); }
    public void RegisterChest() { chestCount++; RefreshHud(); }
    public void RefreshHud()
    {
        if (player != null && hpLabel != null) hpLabel.text = $"HP  {player.CurrentHealth}/{player.MaxHealth}";
        if (scoreLabel != null) scoreLabel.text = $"EMBER  {score}";
        if (killsLabel != null) killsLabel.text = $"KILLS  {kills}";
    }
    public void ShowMessage(string message)
    {
        if (messageLabel != null) { messageLabel.text = message; Invoke(nameof(ClearMessage), 2f); }
    }
    private void ClearMessage() { if (messageLabel != null) messageLabel.text = string.Empty; }
    public void TogglePause()
    {
        bool paused = Time.timeScale == 0f;
        Time.timeScale = paused ? 1f : 0f;
        if (pausePanel != null) pausePanel.SetActive(!paused);
    }
    public void GameOver()
    {
        if (finished) return;
        EnsureEssentialUi();
        finished = true;
        EchoesAudioManager.Play(EchoesSfx.GameOver);
        player?.GetComponent<PlayerController>()?.SetControlsEnabled(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }
    public void Victory()
    {
        if (finished) return;
        EnsureEssentialUi();
        finished = true;
        EchoesAudioManager.Play(EchoesSfx.Victory);
        player?.GetComponent<PlayerController>()?.SetControlsEnabled(false);
        CampaignProgress.Complete(SceneManager.GetActiveScene().name, score);
        if (victoryPanel != null) victoryPanel.SetActive(true);
    }
    /// <summary>Used by normal stage gates: show completion feedback first,
    /// then continue the campaign without requiring another button press.</summary>
    public void VictoryThenAdvance(float delaySeconds = 2f)
    {
        if (finished) return;
        EnsureEssentialUi();
        finished = true;
        EchoesAudioManager.Play(EchoesSfx.Victory);
        player?.GetComponent<PlayerController>()?.SetControlsEnabled(false);
        string current = SceneManager.GetActiveScene().name;
        CampaignProgress.Complete(current, score);
        string next = CampaignProgress.NextScene(current);
        if (string.IsNullOrEmpty(next))
        {
            if (victoryPanel != null) victoryPanel.SetActive(true);
            return;
        }
        if (victoryPanel != null) victoryPanel.SetActive(true);
        ShowMessage("Stage cleared — entering next area...");
        StartCoroutine(AdvanceAfterFeedback(next, delaySeconds));
    }
    private IEnumerator AdvanceAfterFeedback(string nextScene, float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextScene);
    }
    public void RestartLevel() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void BackToTest() { Time.timeScale = 1f; SceneManager.LoadScene("KaelMovementTest"); }
    public void NextLevel()
    {
        Time.timeScale = 1f;
        string next = CampaignProgress.NextScene(SceneManager.GetActiveScene().name);
        if (string.IsNullOrEmpty(next)) { OpenCampaignHub(); return; }
        SceneManager.LoadScene(next);
    }
    public void OpenCampaignHub() { Time.timeScale = 1f; SceneManager.LoadScene("CampaignHub"); }

    /// <summary>
    /// Earlier editor installers were allowed to rebuild the HUD and could clear
    /// the modal references on GameManager.  A submitted build must never become
    /// stuck because of a missing editor reference, so the essential menus repair
    /// themselves at runtime.  Authored CraftPix panels are kept when present.
    /// </summary>
    private void EnsureEssentialUi()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;
        }
        canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 100);
        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        else
        {
            EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem != null && eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                foreach (BaseInputModule oldModule in eventSystem.GetComponents<BaseInputModule>())
                    Destroy(oldModule);
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        if (pausePanel == null)
            pausePanel = CreateFallbackPanel(canvas.transform, "PAUSED", "RESUME", MenuAction.Resume, "RESTART", MenuAction.Restart);
        if (gameOverPanel == null)
            gameOverPanel = CreateFallbackPanel(canvas.transform, "KAEL HAS FALLEN", "RESTART", MenuAction.Restart, "CAMPAIGN", MenuAction.CampaignHub);
        if (victoryPanel == null)
            victoryPanel = CreateFallbackPanel(canvas.transform, "LEVEL COMPLETE", "NEXT LEVEL", MenuAction.NextLevel, "CAMPAIGN", MenuAction.CampaignHub);

        RepairPanelButtons(pausePanel);
        RepairPanelButtons(gameOverPanel);
        RepairPanelButtons(victoryPanel);
    }

    /// <summary>Installers may preserve a pretty CraftPix panel while its old
    /// serialized Button callback points at a deleted component. Reconnect the
    /// visible buttons at runtime so artwork and reliable navigation coexist.</summary>
    private static void RepairPanelButtons(GameObject panel)
    {
        if (panel == null) return;
        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
        {
            string label = button.name;
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null && !string.IsNullOrWhiteSpace(text.text)) label += " " + text.text;
            string key = label.ToLowerInvariant();
            MenuAction action;
            if (key.Contains("resume") || key.Contains("continue")) action = MenuAction.Resume;
            else if (key.Contains("restart") || key.Contains("again")) action = MenuAction.Restart;
            else if (key.Contains("next")) action = MenuAction.NextLevel;
            else if (key.Contains("campaign") || key.Contains("level") || key.Contains("hub")) action = MenuAction.CampaignHub;
            else if (key.Contains("movement") || key.Contains("test")) action = MenuAction.BackToTest;
            else continue;

            MenuActionButton handler = button.GetComponent<MenuActionButton>();
            if (handler == null) handler = button.gameObject.AddComponent<MenuActionButton>();
            handler.SetAction(action);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(handler.InvokeAction);
            button.interactable = true;
        }
    }

    private static GameObject CreateFallbackPanel(Transform parent, string title, string first, MenuAction firstAction, string second, MenuAction secondAction)
    {
        GameObject panel = new GameObject(title + " Panel (Runtime Safe)", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(720f, 390f);
        panel.GetComponent<Image>().color = new Color(.035f, .055f, .085f, .97f);
        CreateText(panel.transform, title, new Vector2(0f, 92f), new Vector2(640f, 80f), 42, new Color(1f, .67f, .18f));
        CreateButton(panel.transform, first, new Vector2(-170f, -70f), firstAction);
        CreateButton(panel.transform, second, new Vector2(170f, -70f), secondAction);
        panel.SetActive(false);
        return panel;
    }

    private static void CreateButton(Transform parent, string label, Vector2 position, MenuAction action)
    {
        GameObject buttonObject = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuActionButton));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(280f, 76f);
        buttonObject.GetComponent<Image>().color = new Color(.12f, .48f, .38f, 1f);
        MenuActionButton handler = buttonObject.GetComponent<MenuActionButton>();
        handler.SetAction(action);
        buttonObject.GetComponent<Button>().onClick.AddListener(handler.InvokeAction);
        CreateText(buttonObject.transform, label, Vector2.zero, rect.sizeDelta, 24, Color.white);
    }

    private static void CreateText(Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        GameObject textObject = new GameObject(value + " Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }
}

public enum MenuAction { Resume, Restart, BackToTest, NextLevel, CampaignHub }

public sealed class MenuActionButton : MonoBehaviour
{
    [SerializeField] private MenuAction action;
    public void SetAction(MenuAction value) => action = value;
    public void InvokeAction()
    {
        if (GameManager.Instance == null) return;
        switch (action)
        {
            case MenuAction.Resume: GameManager.Instance.TogglePause(); break;
            case MenuAction.Restart: GameManager.Instance.RestartLevel(); break;
            case MenuAction.BackToTest: GameManager.Instance.BackToTest(); break;
            case MenuAction.NextLevel: GameManager.Instance.NextLevel(); break;
            case MenuAction.CampaignHub: GameManager.Instance.OpenCampaignHub(); break;
        }
    }
}
