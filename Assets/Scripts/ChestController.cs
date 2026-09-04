using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ChestController : MonoBehaviour
{
    [SerializeField] private string chestId;
    [SerializeField] private int rewardTable;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openedSprite;
    [SerializeField] private Sprite[] rewardIcons;
    private bool heroInRange;
    private bool opened;
    private PlayerHealth nearbyHero;
    private SpriteRenderer display;
    private const string OpenPrefix = "Echoes.Chest.";

    public void Configure(string id, int table, Sprite closed, Sprite openedVisual, Sprite[] icons = null)
    {
        chestId = id; rewardTable = table; closedSprite = closed; openedSprite = openedVisual; rewardIcons = icons;
    }
    private void Awake()
    {
        display = GetComponent<SpriteRenderer>();
        opened = PlayerPrefs.GetInt(OpenPrefix + chestId, 0) == 1;
        RefreshVisual();
    }
    private void Update()
    {
        if (!heroInRange || opened || Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame) return;
        Open();
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerHealth>() == null) return;
        heroInRange = true;
        nearbyHero = other.GetComponentInParent<PlayerHealth>();
        if (!opened) GameManager.Instance?.ShowMessage("Press E to open chest");
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null) heroInRange = false;
    }
    private void Open()
    {
        opened = true;
        PlayerPrefs.SetInt(OpenPrefix + chestId, 1); PlayerPrefs.Save();
        GameManager.Instance?.RegisterChest();
        EchoesAudioManager.Play(EchoesSfx.Chest);
        RefreshVisual();
        string reward;
        // First chest in a campaign provides the core ability; later rewards are deterministic by chest id.
        Sprite rewardIcon;
        if (!RpgProgression.HasFireball) { RpgProgression.UnlockFireball(); reward = "Fireball Scroll unlocked! Press F"; rewardIcon = GetIcon(0); }
        else
        {
            switch (rewardTable % 4)
            {
                case 0: GameManager.Instance?.AddScore(50); reward = "+50 Ember"; rewardIcon = GetIcon(1); break;
                case 1: RpgProgression.GrantEmberBlade(); reward = "Ember Blade equipped: +1 damage"; rewardIcon = GetIcon(2); break;
                case 2: RpgProgression.GrantCinderMail(); nearbyHero?.Heal(2); reward = "Cinder Mail found: +2 max HP"; rewardIcon = GetIcon(3); break;
                default: RpgProgression.GrantWindRelic(); reward = "Wind Relic found: +move speed"; rewardIcon = GetIcon(4); break;
            }
        }
        GameManager.Instance?.ShowMessage(reward);
        RpgLootPopup.Show(rewardIcon, reward);
    }
    private void RefreshVisual() { if (display != null) display.sprite = opened ? openedSprite : closedSprite; }
    private Sprite GetIcon(int index) => rewardIcons != null && index >= 0 && index < rewardIcons.Length ? rewardIcons[index] : null;
}
