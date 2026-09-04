using UnityEngine;

/// <summary>Reusable trap behaviour for saws, fire plates and lava props.</summary>
[RequireComponent(typeof(Collider2D))]
public sealed class CraftpixHazard : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float cooldown = .75f;
    private float nextHit;
    public void Configure(int value) => damage = Mathf.Max(1, value);
    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < nextHit) return;
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null) return;
        nextHit = Time.time + cooldown;
        player.TakeDamage(damage);
    }
}
