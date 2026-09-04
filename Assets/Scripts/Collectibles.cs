using UnityEngine;
using UnityEngine.InputSystem;

public sealed class EmberShard : MonoBehaviour
{
    [SerializeField] private int scoreValue = 10;
    private Vector3 basePosition;
    private void Awake() => basePosition = transform.position;
    private void Update()
    {
        transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * 3f + basePosition.x) * 0.12f);
        transform.Rotate(0f, 0f, 90f * Time.deltaTime);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerHealth>() == null) return;
        GameManager.Instance?.AddScore(scoreValue);
        EchoesAudioManager.Play(EchoesSfx.Pickup);
        Destroy(gameObject);
    }
}

public sealed class HeartPickup : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth hero = other.GetComponent<PlayerHealth>();
        if (hero == null) return;
        hero.Heal(1);
        EchoesAudioManager.Play(EchoesSfx.Pickup);
        Destroy(gameObject);
    }
}

public sealed class Checkpoint : MonoBehaviour
{
    [SerializeField] private bool activated;
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController hero = other.GetComponent<PlayerController>();
        if (hero == null || activated) return;
        activated = true;
        hero.SetCheckpoint(transform.position + Vector3.up * 0.6f);
        GetComponent<SpriteRenderer>().color = new Color(0.35f, 1f, 0.65f);
        GameManager.Instance?.ShowMessage("Checkpoint ignited");
    }
}

public sealed class LevelGoal : MonoBehaviour
{
    [SerializeField] private string prompt = "Press E to enter the Ember Gate";
    private bool heroInRange;
    private bool opening;
    private SpriteRenderer visual;

    private void Awake() => visual = GetComponent<SpriteRenderer>();

    private void Update()
    {
        if (visual != null)
        {
            float glow = .78f + Mathf.Sin(Time.time * 4f) * .22f;
            visual.color = new Color(1f, glow, .52f, 1f);
        }
        // A gate is an exit, not a separate input puzzle. Reaching it is
        // enough: the player sees the completion panel then advances.
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerHealth>() == null) return;
        heroInRange = true;
        if (opening) return;
        opening = true;
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null) trigger.enabled = false;
        GameManager.Instance?.ShowMessage("Ember Gate awakened");
        GameManager.Instance?.VictoryThenAdvance();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null) heroInRange = false;
    }
}
