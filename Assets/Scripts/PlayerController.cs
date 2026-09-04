using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Keyboard and mouse control for Kael. The component deliberately controls
/// the parameters authored in Kael_Controller, so animation behaviour stays
/// visible and easy to demonstrate during the project presentation.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Collider2D))]
public sealed class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float jumpForce = 8.5f;
    [SerializeField] private float groundProbeDistance = 0.12f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Combat")]
    [SerializeField] private float attackDuration = 0.48f;
    [SerializeField] private float attackRange = 1.15f;
    [SerializeField] private Vector2 attackHitboxSize = new Vector2(1.35f, 1.15f);
    [SerializeField] private int attackDamage = 1;

    private Rigidbody2D body;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D[] ownColliders;
    private float moveInput;
    private bool isGrounded;
    private bool isAttacking;
    private bool isDefending;
    private int nextAttack = 1;
    private Vector3 spawnPosition;
    private bool controlsEnabled = true;

    public bool IsDefending => isDefending;
    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        ownColliders = GetComponentsInChildren<Collider2D>();
        spawnPosition = transform.position;
    }

    private void Update()
    {
        if (!controlsEnabled) return;
        ReadMovementInput();
        ReadActionInput();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        // Safety net for a test or level with a missing platform: Kael never
        // becomes permanently lost below the camera.
        if (transform.position.y < -9f)
        {
            body.linearVelocity = Vector2.zero;
            transform.position = spawnPosition;
            return;
        }
        isGrounded = CheckGrounded();
        body.linearVelocity = new Vector2(moveInput * (walkSpeed + RpgProgression.SpeedBonus), body.linearVelocity.y);
    }

    private void ReadMovementInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
        moveInput = left == right ? 0f : (right ? 1f : -1f);

        if (moveInput != 0f && spriteRenderer != null)
            spriteRenderer.flipX = moveInput < 0f;

        bool wantsJump = keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame;
        if (wantsJump && isGrounded && !isDefending)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
            isGrounded = false;
            EchoesAudioManager.Play(EchoesSfx.Jump);
        }
    }

    private void ReadActionInput()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null) return;

        isDefending = !isAttacking && (keyboard.kKey.isPressed || (mouse != null && mouse.rightButton.isPressed));
        bool wantsAttack = keyboard.jKey.wasPressedThisFrame || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        if (wantsAttack && !isAttacking && !isDefending)
            StartCoroutine(PerformAttack());
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        EchoesAudioManager.Play(EchoesSfx.Sword);
        animator.SetInteger("AttackIndex", nextAttack);
        nextAttack = nextAttack == 3 ? 1 : nextAttack + 1;
        yield return new WaitForSeconds(attackDuration * 0.28f);
        ApplyAttackDamage();
        yield return new WaitForSeconds(attackDuration * 0.72f);
        animator.SetInteger("AttackIndex", 0);
        isAttacking = false;
    }

    private void ApplyAttackDamage()
    {
        float direction = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
        Vector2 origin = (Vector2)transform.position + Vector2.right * direction * attackRange * 0.55f;
        // A rectangular sword arc is intentional: it only reaches enemies in
        // front of Kael and prevents a slash from hitting through the stage.
        var damagedEnemies = new HashSet<EnemyController>();
        foreach (Collider2D target in Physics2D.OverlapBoxAll(origin, attackHitboxSize, 0f))
        {
            EnemyController enemy = target.GetComponentInParent<EnemyController>();
            if (enemy != null && damagedEnemies.Add(enemy))
                enemy.TakeDamage(attackDamage + RpgProgression.DamageBonus);
        }
    }

    public void SetCheckpoint(Vector3 position) => spawnPosition = position;
    public void SetControlsEnabled(bool value)
    {
        controlsEnabled = value;
        if (!value && body != null) body.linearVelocity = Vector2.zero;
    }

    private bool CheckGrounded()
    {
        Collider2D primary = GetComponent<Collider2D>();
        if (primary == null) return false;

        Bounds bounds = primary.bounds;
        float[] xPositions = { bounds.min.x + 0.06f, bounds.center.x, bounds.max.x - 0.06f };
        foreach (float x in xPositions)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, bounds.min.y + 0.025f), Vector2.down, groundProbeDistance, groundMask);
            if (hits.Any(hit => hit.collider != null && !ownColliders.Contains(hit.collider)))
                return true;
        }
        return false;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat("Speed", Mathf.Abs(moveInput));
        animator.SetBool("Grounded", isGrounded);
        animator.SetBool("IsDefending", isDefending);
    }

    private void OnDrawGizmosSelected()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        float direction = renderer != null && renderer.flipX ? -1f : 1f;
        Vector2 origin = (Vector2)transform.position + Vector2.right * direction * attackRange * 0.55f;
        Gizmos.color = new Color(1f, .55f, .1f, .45f);
        Gizmos.DrawWireCube(origin, attackHitboxSize);
    }
}
