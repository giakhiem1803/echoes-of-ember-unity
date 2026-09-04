using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Fireball combat skill with a guaranteed visible pixel-flame base.</summary>
public sealed class PlayerMagic : MonoBehaviour
{
    [SerializeField] private Sprite[] fireballFrames;
    [SerializeField] private float maxMana = 100f, manaRegenPerSecond = 14f, fireballCost = 25f, cooldown = 1.0f;
    private float mana, availableAt;
    private SpriteRenderer heroSprite;
    public float Mana => mana;
    public float MaxMana => maxMana;
    public float Cooldown01 => Mathf.Clamp01((availableAt - Time.time) / cooldown);

    private void Awake() { mana = maxMana; heroSprite = GetComponent<SpriteRenderer>(); }
    private void Update()
    {
        mana = Mathf.Min(maxMana, mana + manaRegenPerSecond * Time.deltaTime);
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.fKey.wasPressedThisFrame || Time.timeScale == 0f) return;
        if (!RpgProgression.HasFireball) { GameManager.Instance?.ShowMessage("Find the Fireball Scroll"); return; }
        if (Time.time < availableAt) { GameManager.Instance?.ShowMessage("Fireball is cooling down"); return; }
        if (mana < fireballCost) { GameManager.Instance?.ShowMessage("Not enough mana"); return; }
        mana -= fireballCost; availableAt = Time.time + cooldown;
        EchoesAudioManager.Play(EchoesSfx.Fireball);
        float direction = heroSprite != null && heroSprite.flipX ? -1f : 1f;
        Vector2 start = (Vector2)transform.position + Vector2.right * direction * .78f + Vector2.up * .28f;
        MagicProjectile.CastBurst(fireballFrames, start - Vector2.right * direction * .18f);
        MagicProjectile.Create(fireballFrames, start, direction);
    }
    public void Configure(Sprite sprite) => fireballFrames = sprite == null ? null : new[] { sprite };
    public void Configure(Sprite[] sprites) => fireballFrames = sprites;
}

public sealed class MagicProjectile : MonoBehaviour
{
    private Sprite[] frames;
    private float age;
    private SpriteRenderer craftpixOverlay;
    private float nextTrail;

    public static void Create(Sprite[] source, Vector2 position, float direction)
    {
        GameObject root = new GameObject("Fireball Projectile", typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(MagicProjectile));
        root.transform.position = position;
        Rigidbody2D body = root.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0f; body.freezeRotation = true; body.linearVelocity = Vector2.right * direction * 14f;
        CircleCollider2D hitbox = root.GetComponent<CircleCollider2D>(); hitbox.isTrigger = true; hitbox.radius = .34f;
        // Keep the generated glow subtle: the actual CraftPix projectile must
        // be the clearly visible centrepiece, not a UI icon or a flat circle.
        MagicVisual.AddLayer(root.transform, "Fireball Glow", MagicVisual.Glow, Vector3.one * .78f, new Color(1f, .16f, .01f, .28f), 95);
        MagicVisual.AddLayer(root.transform, "Fireball Core", MagicVisual.Core, Vector3.one * .38f, new Color(1f, .78f, .14f, .85f), 97);
        SpriteRenderer overlay = MagicVisual.AddLayer(root.transform, "CraftPix Flame", null, Vector3.one * 1.32f, Color.white, 99);
        MagicProjectile p = root.GetComponent<MagicProjectile>(); p.frames = ValidFrames(source); p.craftpixOverlay = overlay; p.ApplyFrame();
        Object.Destroy(root, 4.0f);
    }

    public static void CastBurst(Sprite[] source, Vector2 position) => CreateBurst("Fireball Cast", source, position, .55f, .20f, 92);
    private static Sprite[] ValidFrames(Sprite[] source)
    {
        if (source == null) return null;
        List<Sprite> valid = new List<Sprite>(); foreach (Sprite s in source) if (s != null) valid.Add(s);
        return valid.Count > 0 ? valid.ToArray() : null;
    }

    private void Update()
    {
        age += Time.deltaTime; ApplyFrame();
        if (Time.time >= nextTrail) { nextTrail = Time.time + .055f; MagicVisual.CreateTrail(transform.position); }
        Camera cam = Camera.main;
        if (cam != null && Mathf.Abs(transform.position.x - cam.transform.position.x) > 35f) Destroy(gameObject);
    }
    private void ApplyFrame()
    {
        if (craftpixOverlay == null) return;
        if (frames != null && frames.Length > 0) { craftpixOverlay.sprite = frames[Mathf.FloorToInt(age * 16f) % frames.Length]; craftpixOverlay.enabled = true; }
        else craftpixOverlay.enabled = false;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null) return;
        EnemyController enemy = other.GetComponentInParent<EnemyController>();
        if (enemy != null) { enemy.TakeDamage(2); CreateBurst("Fireball Impact", frames, transform.position, 1.25f, .36f, 102); Destroy(gameObject); return; }
        if (!other.isTrigger) { CreateBurst("Fireball Impact", frames, transform.position, 1f, .30f, 102); Destroy(gameObject); }
    }
    private static void CreateBurst(string name, Sprite[] source, Vector2 pos, float scale, float life, int order)
    {
        GameObject root = new GameObject(name, typeof(MagicBurst)); root.transform.position = pos; root.transform.localScale = Vector3.one * scale;
        MagicVisual.AddLayer(root.transform, "Impact Glow", MagicVisual.Glow, Vector3.one, new Color(1f, .13f, .01f, .62f), order);
        MagicVisual.AddLayer(root.transform, "Impact Core", MagicVisual.Core, Vector3.one * .72f, Color.white, order + 1);
        SpriteRenderer overlay = MagicVisual.AddLayer(root.transform, "CraftPix Impact", null, Vector3.one * .9f, Color.white, order + 2);
        root.GetComponent<MagicBurst>().Setup(ValidFrames(source), overlay, life);
    }
}

public sealed class MagicBurst : MonoBehaviour
{
    private Sprite[] frames; private SpriteRenderer overlay; private float lifetime, age;
    public void Setup(Sprite[] source, SpriteRenderer target, float duration) { frames = source; overlay = target; lifetime = duration; }
    private void Update()
    {
        age += Time.deltaTime; transform.localScale *= 1f + Time.deltaTime * 2.2f;
        if (overlay != null) { if (frames != null && frames.Length > 0) overlay.sprite = frames[Mathf.Min(frames.Length - 1, Mathf.FloorToInt(age / lifetime * frames.Length))]; else overlay.enabled = false; }
        if (age >= lifetime) Destroy(gameObject);
    }
}

/// <summary>Reliable pixel flame base, with CraftPix frames layered on top when available.</summary>
public static class MagicVisual
{
    private static Sprite glow, core;
    public static Sprite Glow => glow ??= MakeSprite(20, true);
    public static Sprite Core => core ??= MakeSprite(12, false);
    public static SpriteRenderer AddLayer(Transform parent, string name, Sprite sprite, Vector3 scale, Color color, int order)
    {
        GameObject child = new GameObject(name, typeof(SpriteRenderer)); child.transform.SetParent(parent, false); child.transform.localScale = scale;
        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order; return renderer;
    }
    public static void CreateTrail(Vector3 position)
    {
        GameObject go = new GameObject("Fireball Trail", typeof(SpriteRenderer), typeof(MagicFade)); go.transform.position = position; go.transform.localScale = Vector3.one * .33f;
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = Glow; renderer.color = new Color(1f, .24f, .02f, .52f); renderer.sortingOrder = 94;
        go.GetComponent<MagicFade>().Setup(.26f);
    }
    private static Sprite MakeSprite(int size, bool soft)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        float radius = (size - 1) * .5f;
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) / radius;
            float alpha = soft ? Mathf.Clamp01(1f - distance) * .8f : distance < .82f ? 1f : 0f;
            Color color = Color.Lerp(new Color(1f, .12f, .01f, alpha), new Color(1f, .92f, .16f, alpha), Mathf.Clamp01(1f - distance * 1.35f)); color.a = alpha; texture.SetPixel(x, y, color);
        }
        texture.Apply(); return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 16f);
    }
}

public sealed class MagicFade : MonoBehaviour
{
    private float duration, age; private SpriteRenderer spriteRenderer;
    public void Setup(float seconds) { duration = seconds; spriteRenderer = GetComponent<SpriteRenderer>(); }
    private void Update() { age += Time.deltaTime; if (spriteRenderer != null) { Color c = spriteRenderer.color; c.a = Mathf.Lerp(c.a, 0f, Time.deltaTime * 8f); spriteRenderer.color = c; } if (age >= duration) Destroy(gameObject); }
}
