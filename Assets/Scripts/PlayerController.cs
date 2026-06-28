using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 320f;
    [SerializeField] float groundAcceleration = 5500f;
    [SerializeField] float airAcceleration = 1200f;
    [SerializeField] float groundDeceleration = 6000f;
    [SerializeField] float airControl = 0.6f;
    [SerializeField] float jumpHeight = 75f;
    [SerializeField] int maxAirJumps = 1;
    [SerializeField] float doubleJumpHeight = 75f;

    [Header("Gravity")]
    [SerializeField] float gravityScale = 52f;
    [SerializeField] float riseGravityMultiplier = 3.8f;
    [SerializeField] float fallGravityMultiplier = 4.8f;
    [SerializeField] float maxFallSpeed = 1300f;

    [Header("Dash")]
    [SerializeField] float dashSpeed = 650f;
    [SerializeField] float dashDuration = 0.07f;
    [SerializeField] float dashCooldown = 0.4f;
    [SerializeField] bool dashZeroGravity = true;
    [SerializeField] bool dashHardStop = true;

    [Header("Attack")]
    [SerializeField] float attackDuration = 0.35f;
    [SerializeField] float attackRange = 180f;
    [SerializeField] Vector2 attackBoxSize = new Vector2(200f, 180f);
    [SerializeField] int attackDamage = 15;
    [SerializeField] float attackHitTime = 0.15f;
    [SerializeField] LayerMask monsterMask = ~0;
    [SerializeField] AudioClip hitSound1;
    [SerializeField] AudioClip hitSound2;

    [Header("Movement SFX")]
    [SerializeField] AudioClip dashSound;
    [SerializeField] AudioClip jumpSound;
    [SerializeField] AudioClip landingSound;
    [SerializeField] AudioClip runSound;

    [Header("Ground Check")]
    [SerializeField] float groundCheckDistance = 0.35f;
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float coyoteTime = 0.05f;

    [Header("Collider")]
    [SerializeField] Vector2 bodyColliderOffset = new Vector2(0f, 35f);
    [SerializeField] Vector2 bodyColliderSize = new Vector2(40f, 70f);

    [Header("Input")]
    [SerializeField] InputActionAsset inputActions;

    Rigidbody2D rb;
    CharacterStats characterStats;
    SPUM_Prefabs spum;
    Collider2D bodyCollider;
    AudioSource sfxSource;
    AudioSource runLoopSource;

    InputAction moveAction;
    InputAction jumpAction;
    InputAction attackAction;
    InputAction dashAction;

    float moveInput;
    int facing = 1;
    bool isGrounded;
    float coyoteTimer;
    int airJumpsRemaining;
    bool isDashing;
    bool isAttacking;
    float dashTimer;
    float dashCooldownTimer;
    Vector2 dashDirection;
    int attackIndex;
    bool wasGrounded;
    PlayerState currentAnimState = PlayerState.IDLE;
    int lastHealth;
    bool isHurt;
    bool isDead;
    Coroutine hurtRoutine;
    Coroutine deathRoutine;

    public bool IsDead => isDead;
    public bool IsGrounded => isGrounded;
    public float VerticalVelocity => rb != null ? rb.linearVelocity.y : 0f;

    public InputActionAsset GetInputActions() => inputActions;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        characterStats = GetComponent<CharacterStats>();
        bodyCollider = GetComponent<Collider2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.gravityScale = gravityScale;

        spum = GetComponentInChildren<SPUM_Prefabs>();
        if (spum != null)
        {
            if (!spum.allListsHaveItemsExist())
                spum.PopulateAnimationLists();
            spum.OverrideControllerInit();
        }

        RemoveChildPhysics();
        ApplyBodyCollider();
        ResetVisualTransform();
        BindInputActions();
        SetupSfxSource();
        airJumpsRemaining = maxAirJumps;
    }

    void Start()
    {
        wasGrounded = CheckGrounded();
        isGrounded = wasGrounded;

        if (characterStats != null)
            lastHealth = characterStats.CurrentHealth;
    }

    void OnEnable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
        attackAction?.Enable();
        dashAction?.Enable();

        if (characterStats != null)
        {
            characterStats.OnHealthChanged += HandleHealthChanged;
            characterStats.OnDeath += HandleDeath;
        }
    }

    void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
        attackAction?.Disable();
        dashAction?.Disable();

        if (characterStats != null)
        {
            characterStats.OnHealthChanged -= HandleHealthChanged;
            characterStats.OnDeath -= HandleDeath;
        }
    }

    void SetupSfxSource()
    {
        var sources = GetComponents<AudioSource>();
        if (sources.Length > 0)
            sfxSource = sources[0];
        else
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.loop = false;

        if (sources.Length > 1)
            runLoopSource = sources[1];
        else
            runLoopSource = gameObject.AddComponent<AudioSource>();

        runLoopSource.playOnAwake = false;
        runLoopSource.spatialBlend = 0f;
        runLoopSource.loop = true;
    }

    void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.PlayOneShot(clip, GameSettings.GetEffectiveSfxVolume());
    }

    void ApplyBodyCollider()
    {
        if (bodyCollider is not BoxCollider2D box)
            return;

        box.offset = bodyColliderOffset;
        box.size = bodyColliderSize;
    }

    void RemoveChildPhysics()
    {
        foreach (var childRb in GetComponentsInChildren<Rigidbody2D>(true))
        {
            if (childRb == rb)
                continue;

            var childCol = childRb.GetComponent<Collider2D>();
            if (childCol != null)
                Destroy(childCol);

            Destroy(childRb);
        }

        foreach (var childCol in GetComponentsInChildren<Collider2D>(true))
        {
            if (childCol == bodyCollider)
                continue;
            if (childCol.GetComponent<Rigidbody2D>() == null)
                Destroy(childCol);
        }
    }

    void ResetVisualTransform()
    {
        if (spum == null)
            return;

        var rect = spum.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = Vector2.zero;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
        }
    }

    void BindInputActions()
    {
        if (inputActions == null)
            return;

        var map = inputActions.FindActionMap("Player");
        moveAction = map.FindAction("Move");
        jumpAction = map.FindAction("Jump");
        attackAction = map.FindAction("Attack");
        dashAction = map.FindAction("Sprint");
    }

    void HandleHealthChanged(CharacterStats stats)
    {
        if (isDead || stats.CurrentHealth <= 0)
        {
            lastHealth = stats.CurrentHealth;
            return;
        }

        if (stats.CurrentHealth < lastHealth)
            PlayHurtAnimation();

        lastHealth = stats.CurrentHealth;
    }

    void HandleDeath(CharacterStats _)
    {
        if (isDead || deathRoutine != null)
            return;

        deathRoutine = StartCoroutine(DeathRoutine());
    }

    void PlayHurtAnimation()
    {
        if (isDead || isHurt)
            return;

        if (hurtRoutine != null)
            StopCoroutine(hurtRoutine);

        hurtRoutine = StartCoroutine(HurtRoutine());
    }

    IEnumerator HurtRoutine()
    {
        isHurt = true;

        if (isDashing)
            EndDash();

        isAttacking = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (runLoopSource != null && runLoopSource.isPlaying)
            runLoopSource.Stop();

        PlaySpumState(PlayerState.DAMAGED, 0);
        yield return new WaitForSeconds(GetClipDuration(PlayerState.DAMAGED, 0, 0.35f));

        isHurt = false;
        hurtRoutine = null;
        currentAnimState = PlayerState.IDLE;
    }

    IEnumerator DeathRoutine()
    {
        isDead = true;
        isHurt = false;
        isAttacking = false;

        if (isDashing)
            EndDash();

        if (hurtRoutine != null)
        {
            StopCoroutine(hurtRoutine);
            hurtRoutine = null;
        }

        if (runLoopSource != null && runLoopSource.isPlaying)
            runLoopSource.Stop();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        PlaySpumState(PlayerState.DEATH, 0);
        yield return new WaitForSeconds(GetClipDuration(PlayerState.DEATH, 0, 0.8f));

        deathRoutine = null;
        LevelDeathController.Instance?.HandlePlayerDeath();
    }

    void PlaySpumState(PlayerState state, int index)
    {
        if (spum == null || !HasSpumState(state))
            return;

        index = Mathf.Clamp(index, 0, GetSpumStateCount(state) - 1);
        spum.PlayAnimation(state, index);
        currentAnimState = state;
    }

    bool HasSpumState(PlayerState state)
    {
        return GetSpumStateCount(state) > 0;
    }

    int GetSpumStateCount(PlayerState state)
    {
        if (spum == null)
            return 0;

        return state switch
        {
            PlayerState.IDLE => spum.IDLE_List.Count,
            PlayerState.MOVE => spum.MOVE_List.Count,
            PlayerState.ATTACK => spum.ATTACK_List.Count,
            PlayerState.DAMAGED => spum.DAMAGED_List.Count,
            PlayerState.DEBUFF => spum.DEBUFF_List.Count,
            PlayerState.DEATH => spum.DEATH_List.Count,
            PlayerState.OTHER => spum.OTHER_List.Count,
            _ => 0
        };
    }

    float GetClipDuration(PlayerState state, int index, float fallback)
    {
        if (spum == null)
            return fallback;

        var clips = state switch
        {
            PlayerState.DAMAGED => spum.DAMAGED_List,
            PlayerState.DEATH => spum.DEATH_List,
            PlayerState.ATTACK => spum.ATTACK_List,
            PlayerState.OTHER => spum.OTHER_List,
            _ => null
        };

        if (clips == null || clips.Count == 0)
            return fallback;

        index = Mathf.Clamp(index, 0, clips.Count - 1);
        var clip = clips[index];
        return clip != null ? clip.length : fallback;
    }

    float ReadHorizontalInput()
    {
        if (moveAction != null)
            return moveAction.ReadValue<Vector2>().x;

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return 0f;

        float x = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            x += 1f;
        return x;
    }

    bool WasJumpPressed()
    {
        if (jumpAction != null && jumpAction.WasPressedThisFrame())
            return true;
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    bool WasAttackPressed()
    {
        if (attackAction != null && attackAction.WasPressedThisFrame())
            return true;
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        return (keyboard != null && keyboard.enterKey.wasPressedThisFrame)
            || (mouse != null && mouse.leftButton.wasPressedThisFrame);
    }

    bool WasDashPressed()
    {
        if (dashAction != null && dashAction.WasPressedThisFrame())
            return true;
        return Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame;
    }

    float JumpVelocity(float height)
    {
        float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        return Mathf.Sqrt(2f * gravity * height);
    }

    void PerformJump(float height)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, JumpVelocity(height));
        isGrounded = false;
        rb.WakeUp();
        PlaySfx(jumpSound);
    }

    void Update()
    {
        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
        }
        else if (isDashing)
        {
            isDashing = false;
            EndDash();
        }

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        if (isAttacking || isHurt || isDead)
            return;

        moveInput = ReadHorizontalInput();

        if (WasJumpPressed())
            TryJump();

        if (WasAttackPressed() && !isDashing)
            StartCoroutine(AttackRoutine());

        if (WasDashPressed() && dashCooldownTimer <= 0f)
            StartDash();

        UpdateAnimation();
    }

    void TryJump()
    {
        if (isDashing || isAttacking)
            return;

        bool fromGround = CheckGrounded() || coyoteTimer > 0f;

        if (fromGround)
        {
            PerformJump(jumpHeight);
            coyoteTimer = 0f;
            airJumpsRemaining = maxAirJumps;
            return;
        }

        if (airJumpsRemaining <= 0)
            return;

        PerformJump(doubleJumpHeight);
        airJumpsRemaining--;
    }

    void FixedUpdate()
    {
        bool groundedNow = CheckGrounded();

        if (groundedNow && !wasGrounded && rb.linearVelocity.y <= 1f)
            PlaySfx(landingSound);

        wasGrounded = groundedNow;
        isGrounded = groundedNow;

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            airJumpsRemaining = maxAirJumps;
        }
        else if (coyoteTimer > 0f)
            coyoteTimer -= Time.fixedDeltaTime;

        if (isAttacking || isHurt || isDead)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            UpdateRunLoopSound();
            return;
        }

        if (isDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            UpdateRunLoopSound();
            return;
        }

        ApplyHorizontalMovement();
        ApplyGravityModifiers();
        UpdateRunLoopSound();
    }

    void UpdateRunLoopSound()
    {
        if (runLoopSource == null || runSound == null)
            return;

        bool shouldRun = isGrounded
            && Mathf.Abs(moveInput) > 0.01f
            && !isDashing
            && !isAttacking
            && !isHurt
            && !isDead;

        float volume = GameSettings.GetEffectiveSfxVolume();

        if (shouldRun)
        {
            runLoopSource.volume = volume;
            if (!runLoopSource.isPlaying)
            {
                runLoopSource.clip = runSound;
                runLoopSource.Play();
            }
        }
        else if (runLoopSource.isPlaying)
            runLoopSource.Stop();
    }

    void ApplyHorizontalMovement()
    {
        float speedMultiplier = characterStats != null ? characterStats.MoveSpeedMultiplier : 1f;
        float maxSpeed = isGrounded ? moveSpeed * speedMultiplier : moveSpeed * airControl * speedMultiplier;
        float targetX = moveInput * maxSpeed;

        if (Mathf.Abs(moveInput) > 0.01f)
        {
            float accel = isGrounded ? groundAcceleration : airAcceleration;
            targetX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, accel * Time.fixedDeltaTime);
        }
        else if (isGrounded)
        {
            targetX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, groundDeceleration * Time.fixedDeltaTime);
        }

        rb.linearVelocity = new Vector2(targetX, rb.linearVelocity.y);
    }

    void ApplyGravityModifiers()
    {
        float baseGravity = Physics2D.gravity.y * rb.gravityScale;
        float extraGravity = 0f;

        if (rb.linearVelocity.y < 0f)
            extraGravity = baseGravity * (fallGravityMultiplier - 1f);
        else if (rb.linearVelocity.y > 0f)
            extraGravity = baseGravity * (riseGravityMultiplier - 1f);

        if (Mathf.Abs(extraGravity) > 0.001f)
        {
            rb.linearVelocity += Vector2.up * extraGravity * Time.fixedDeltaTime;
        }

        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }

    bool CheckGrounded()
    {
        if (bodyCollider == null)
            return false;

        var bounds = bodyCollider.bounds;
        var origin = new Vector2(bounds.center.x, bounds.min.y + 0.05f);
        var hits = Physics2D.RaycastAll(origin, Vector2.down, groundCheckDistance, groundMask);

        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider == bodyCollider)
                continue;
            if (hit.transform.IsChildOf(transform))
                continue;
            return true;
        }

        return false;
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        float direction = Mathf.Abs(moveInput) > 0.01f ? Mathf.Sign(moveInput) : facing;
        dashDirection = new Vector2(direction, 0f);

        if (dashZeroGravity)
            rb.gravityScale = 0f;

        rb.linearVelocity = dashDirection * dashSpeed;

        if (spum != null && spum.OTHER_List.Count > 0)
            spum.PlayAnimation(PlayerState.OTHER, 0);

        PlaySfx(dashSound);
    }

    void EndDash()
    {
        if (dashZeroGravity)
            rb.gravityScale = gravityScale;

        if (dashHardStop)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        currentAnimState = PlayerState.IDLE;
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        int currentAttack = 0;
        if (spum != null && spum.ATTACK_List.Count > 0)
        {
            currentAttack = attackIndex % spum.ATTACK_List.Count;
            spum.PlayAnimation(PlayerState.ATTACK, currentAttack);
            attackIndex++;
        }

        // Ждём момент "попадания" внутри анимации, затем проверяем хитбокс
        yield return new WaitForSeconds(attackHitTime);
        PerformAttackHit();

        // Ждём оставшееся время анимации
        float remaining = Mathf.Max(0f, attackDuration - attackHitTime);
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        PlayHitSound(currentAttack);

        yield return new WaitForSeconds(attackDuration);
        isAttacking = false;
        currentAnimState = PlayerState.IDLE;
    }

    void PerformAttackHit()
    {
        var attackOrigin = (Vector2)transform.position + Vector2.right * facing * (attackRange * 0.5f);
        var hits = Physics2D.OverlapBoxAll(attackOrigin, attackBoxSize, 0f, monsterMask);
        Debug.Log($"[Player] Атака: hits={hits.Length}, monsterMask={monsterMask.value}, origin={attackOrigin}, box={attackBoxSize}");

        var allHits = Physics2D.OverlapBoxAll(attackOrigin, attackBoxSize, 0f);
        Debug.Log($"[Player] Всего коллайдеров: {allHits.Length}");
        foreach (var h in allHits)
            Debug.Log($"  {h.name} layer={LayerMask.LayerToName(h.gameObject.layer)} ena={h.enabled} trig={h.isTrigger}");

        foreach (var hit in hits)
        {
            if (hit == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            var monsterStats = hit.GetComponentInParent<MonsterStats>();
            if (monsterStats != null && !monsterStats.IsDead)
            {
                int damage = Mathf.RoundToInt(attackDamage * (characterStats != null
                    ? characterStats.PhysicalAttackMultiplier
                    : 1f));
                monsterStats.TakeDamage(damage);
                DamagePopup.Show(hit.bounds.center, damage, Color.red);
                Debug.Log($"[Player] Удар по {monsterStats.name} на {damage} урона. HP={monsterStats.CurrentHealth}/{monsterStats.MaxHealth}");
            }
            else
            {
                Debug.Log($"[Player] Коллайдер {hit.name} — нет MonsterStats (parent={hit.transform.parent?.name})");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider2D>();
        if (bodyCollider == null)
            return;

        // Ground check
        var bounds = bodyCollider.bounds;
        var origin = new Vector2(bounds.center.x, bounds.min.y + 0.05f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + Vector2.down * groundCheckDistance);

        // Attack hitbox
        Gizmos.color = Color.red;
        var attackOrigin = (Vector2)transform.position + Vector2.right * facing * (attackRange * 0.5f);
        Gizmos.DrawWireCube(attackOrigin, attackBoxSize);
    }

    void PlayHitSound(int attackAnimIndex)
    {
        AudioClip clip = attackAnimIndex % 2 == 0 ? hitSound1 : hitSound2;
        if (clip == null)
            clip = attackAnimIndex % 2 == 0 ? hitSound2 : hitSound1;

        PlaySfx(clip);
    }

    void UpdateAnimation()
    {
        if (spum == null || isAttacking || isDashing || isHurt || isDead)
            return;

        if (Mathf.Abs(moveInput) > 0.01f)
        {
            facing = moveInput > 0f ? 1 : -1;
            FlipVisual(facing);
        }

        PlayerState nextState;
        if (!isGrounded)
            nextState = PlayerState.MOVE;
        else if (Mathf.Abs(moveInput) > 0.01f)
            nextState = PlayerState.MOVE;
        else
            nextState = PlayerState.IDLE;

        if (nextState == currentAnimState)
            return;

        currentAnimState = nextState;
        spum.PlayAnimation(currentAnimState, 0);
    }

    void FlipVisual(int direction)
    {
        var visual = spum.transform;
        var scale = visual.localScale;
        visual.localScale = new Vector3(Mathf.Abs(scale.x) * (direction > 0 ? -1f : 1f), scale.y, scale.z);
    }

}
