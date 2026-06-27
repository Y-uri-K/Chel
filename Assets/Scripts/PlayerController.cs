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

    [Header("Ground Check")]
    [SerializeField] float groundCheckDistance = 0.35f;
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float coyoteTime = 0.05f;

    [Header("Input")]
    [SerializeField] InputActionAsset inputActions;

    Rigidbody2D rb;
    CharacterStats characterStats;
    SPUM_Prefabs spum;
    Collider2D bodyCollider;

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
    PlayerState currentAnimState = PlayerState.IDLE;

    public bool IsGrounded => isGrounded;
    public float VerticalVelocity => rb != null ? rb.linearVelocity.y : 0f;

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
        ResetVisualTransform();
        BindInputActions();
        airJumpsRemaining = maxAirJumps;
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

    void OnEnable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
        attackAction?.Enable();
        dashAction?.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
        attackAction?.Disable();
        dashAction?.Disable();
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

        if (isAttacking)
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
        isGrounded = CheckGrounded();

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            airJumpsRemaining = maxAirJumps;
        }
        else if (coyoteTimer > 0f)
            coyoteTimer -= Time.fixedDeltaTime;

        if (isAttacking)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (isDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            return;
        }

        ApplyHorizontalMovement();
        ApplyGravityModifiers();
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

        if (spum != null && spum.ATTACK_List.Count > 0)
        {
            attackIndex = attackIndex % spum.ATTACK_List.Count;
            spum.PlayAnimation(PlayerState.ATTACK, attackIndex);
            attackIndex++;
        }

        yield return new WaitForSeconds(attackDuration);
        isAttacking = false;
        currentAnimState = PlayerState.IDLE;
    }

    void UpdateAnimation()
    {
        if (spum == null || isAttacking || isDashing)
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

    void OnDrawGizmosSelected()
    {
        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider2D>();
        if (bodyCollider == null)
            return;

        var bounds = bodyCollider.bounds;
        var origin = new Vector2(bounds.center.x, bounds.min.y + 0.05f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + Vector2.down * groundCheckDistance);
    }
}
