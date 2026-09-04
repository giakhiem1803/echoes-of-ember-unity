using UnityEngine;
using UnityEngine.SceneManagement;

public enum EchoesSfx { Jump, Sword, Fireball, Hit, EnemyDefeat, Pickup, Chest, Hurt, Victory, GameOver }

public sealed class EchoesAudioManager : MonoBehaviour
{
    public static EchoesAudioManager Instance { get; private set; }
    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        musicSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = PlayerPrefs.GetFloat("Echoes.MusicVolume", .28f);
        sfxSource.volume = PlayerPrefs.GetFloat("Echoes.SfxVolume", .65f);
        musicSource.clip = BuildMusic(SeasonIndex());
        musicSource.Play();
    }

    public static void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("Echoes.MusicVolume", value); PlayerPrefs.Save();
        if (Instance != null && Instance.musicSource != null) Instance.musicSource.volume = value;
    }

    public static void SetSfxVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("Echoes.SfxVolume", value); PlayerPrefs.Save();
        if (Instance != null && Instance.sfxSource != null) Instance.sfxSource.volume = value;
    }

    public static void Play(EchoesSfx kind)
    {
        if (Instance != null && Instance.sfxSource != null) Instance.sfxSource.PlayOneShot(BuildSfx(kind));
    }

    private static int SeasonIndex()
    {
        string scene = SceneManager.GetActiveScene().name;
        for (int i = 1; i <= 5; i++) if (scene.Contains("0" + i)) return i - 1;
        return 0;
    }

    private static AudioClip BuildMusic(int level)
    {
        const int rate = 22050; const float length = 8f;
        float[] data = new float[(int)(rate * length)];
        float[] roots = { 110f, 123.47f, 98f, 82.41f, 73.42f };
        float root = roots[Mathf.Clamp(level, 0, roots.Length - 1)];
        int[] pattern = { 0, 3, 7, 10, 7, 3, 5, 8 };
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)rate; int step = Mathf.FloorToInt(t * 2f) % pattern.Length;
            float f = root * Mathf.Pow(2f, pattern[step] / 12f);
            data[i] = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t)) * .035f + Mathf.Sin(2f * Mathf.PI * root * .5f * t) * .025f;
        }
        AudioClip clip = AudioClip.Create("Echoes Level " + (level + 1), data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
    }

    private static AudioClip BuildSfx(EchoesSfx kind)
    {
        const int rate = 22050; float duration = kind == EchoesSfx.Victory || kind == EchoesSfx.GameOver ? .75f : .22f;
        float[] data = new float[(int)(rate * duration)];
        float start = kind switch { EchoesSfx.Jump => 280f, EchoesSfx.Sword => 190f, EchoesSfx.Fireball => 520f, EchoesSfx.Hit => 120f, EchoesSfx.EnemyDefeat => 240f, EchoesSfx.Pickup => 740f, EchoesSfx.Chest => 420f, EchoesSfx.Hurt => 95f, EchoesSfx.Victory => 392f, EchoesSfx.GameOver => 220f, _ => 300f };
        float sweep = kind == EchoesSfx.GameOver || kind == EchoesSfx.Hurt ? -170f : 260f;
        for (int i = 0; i < data.Length; i++) { float t = i / (float)rate, p = t / duration; float f = Mathf.Max(40f, start + sweep * p); float wave = kind == EchoesSfx.Sword ? Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t)) : Mathf.Sin(2f * Mathf.PI * f * t); data[i] = wave * (1f - p) * .22f; }
        AudioClip clip = AudioClip.Create("SFX " + kind, data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
    }
}
