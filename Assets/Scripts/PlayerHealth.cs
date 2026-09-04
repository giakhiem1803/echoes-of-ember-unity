using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float invulnerabilitySeconds = 0.7f;
    private int currentHealth;
    private bool invulnerable;
    private bool dead;
    private Animator animator;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth + RpgProgression.HealthBonus;

    private void Awake() { animator = GetComponent<Animator>(); currentHealth = MaxHealth; }

    public void TakeDamage(int amount)
    {
        if (dead || invulnerable) return;
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null && controller.IsDefending) return;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        EchoesAudioManager.Play(EchoesSfx.Hurt);
        GameManager.Instance?.RefreshHud();
        if (currentHealth == 0)
        {
            dead = true;
            animator.SetBool("Dead", true);
            GetComponent<PlayerController>()?.SetControlsEnabled(false);
            GameManager.Instance?.GameOver();
            return;
        }
        animator.SetTrigger("Hurt");
        StartCoroutine(Invulnerability());
    }

    public void Heal(int amount)
    {
        if (dead) return;
        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        GameManager.Instance?.RefreshHud();
    }

    private IEnumerator Invulnerability()
    {
        invulnerable = true;
        yield return new WaitForSeconds(invulnerabilitySeconds);
        invulnerable = false;
    }
}
