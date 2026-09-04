using UnityEngine;

/// <summary>Lightweight frame animation for CraftPix sprite sheets.  It keeps
/// enemies animated without requiring dozens of duplicate Animator Controllers.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CraftpixSpriteAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 9f;
    [SerializeField] private bool randomStart = true;
    private SpriteRenderer view;
    private float time;

    public void Configure(Sprite[] sprites, float fps)
    {
        frames = sprites;
        framesPerSecond = Mathf.Max(1f, fps);
        if (view == null) view = GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0) view.sprite = frames[0];
    }

    private void Awake()
    {
        view = GetComponent<SpriteRenderer>();
        if (randomStart && frames != null && frames.Length > 0)
            time = Random.Range(0, frames.Length) / Mathf.Max(1f, framesPerSecond);
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0 || view == null) return;
        time += Time.deltaTime;
        view.sprite = frames[Mathf.FloorToInt(time * framesPerSecond) % frames.Length];
    }
}
