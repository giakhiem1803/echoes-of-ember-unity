using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class EnemyController : MonoBehaviour
{
    [SerializeField] private int health = 1;
    [SerializeField] private float patrolDistance = 2f;
    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private float detectDistance = 4f;
    [SerializeField] private int contactDamage = 1;
    private Rigidbody2D body;
    private Vector3 origin;
    private Transform player;
    private float direction = -1f;
    private float nextDamageTime;
    private bool dead;

    public bool IsDead => dead;

    public void Configure(int hitPoints, float speed, float detection, int damage, float patrol)
    {
        health = Mathf.Max(1, hitPoints);
        moveSpeed = Mathf.Max(.25f, speed);
        detectDistance = Mathf.Max(1f, detection);
        contactDamage = Mathf.Max(1, damage);
        patrolDistance = Mathf.Max(.5f, patrol);
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;
        origin = transform.position;
    }

    private void Start()
    {
        PlayerHealth hero = FindAnyObjectByType<PlayerHealth>();
        player = hero != null ? hero.transform : null;
    }

    private void FixedUpdate()
    {
        if (dead) return;
        if (player != null && Mathf.Abs(player.position.x - transform.position.x) < detectDistance)
            direction = Mathf.Sign(player.position.x - transform.position.x);
        else if (Mathf.Abs(transform.position.x - origin.x) > patrolDistance)
            direction = -Mathf.Sign(transform.position.x - origin.x);

        body.linearVelocity = new Vector2(direction * moveSpeed, body.linearVelocity.y);
        transform.localScale = new Vector3(direction < 0 ? 1f : -1f, 1f, 1f);
    }

    public void TakeDamage(int amount)
    {
        if (dead) return;
        EchoesAudioManager.Play(EchoesSfx.Hit);
        health -= amount;
        if (health > 0)
        {
            StartCoroutine(HitFlash());
            return;
        }

        // Mark dead before touching physics. Every damage path checks this
        // flag, so the enemy cannot hurt Kael after a killing sword hit.
        dead = true;
        nextDamageTime = float.PositiveInfinity;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }
        foreach (Collider2D hitbox in GetComponentsInChildren<Collider2D>(true))
            hitbox.enabled = false;
        GameManager.Instance?.AddKill();
        GameManager.Instance?.AddScore(20);
        EchoesAudioManager.Play(EchoesSfx.EnemyDefeat);
        StartCoroutine(DieAfterFeedback());
    }

    private IEnumerator HitFlash()
    {
        SpriteRenderer render = GetComponent<SpriteRenderer>();
        if (render == null) yield break;
        Color original = render.color;
        render.color = new Color(1f, .38f, .22f, 1f);
        yield return new WaitForSeconds(.09f);
        if (!dead) render.color = original;
    }

    private IEnumerator DieAfterFeedback()
    {
        SpriteRenderer render = GetComponent<SpriteRenderer>();
        if (render != null)
        {
            render.color = new Color(1f, .45f, .35f, 1f);
            yield return new WaitForSeconds(.12f);
            render.color = new Color(1f, 1f, 1f, .35f);
        }
        yield return new WaitForSeconds(.28f);
        Destroy(gameObject);
    }

    // Enemies are combat hazards, not solid walls. Kael must be able to run
    // through after attacking or jump past one if he chooses.
    private void OnTriggerStay2D(Collider2D other)
    {
        if (dead) return;
        if (Time.time < nextDamageTime) return;
        PlayerHealth hero = other.GetComponentInParent<PlayerHealth>();
        if (hero == null) return;
        nextDamageTime = Time.time + 0.8f;
        hero.TakeDamage(contactDamage);
    }
}
