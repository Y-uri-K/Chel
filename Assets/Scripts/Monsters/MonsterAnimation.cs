using System;
using UnityEngine;

/// <summary>
/// Состояния монстра для анимации и AI.
/// </summary>
public enum MonsterState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    RangedAttack,
    Hurt,
    Immune,
    Death
}

/// <summary>
/// Кодовая покадровая анимация через SpriteRenderer.
/// Не зависит от Animator/AnimatorController — все кадры задаются в инспекторе.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class MonsterAnimation : MonoBehaviour
{
    [Header("Кадры анимаций")]
    public Sprite[] idleSprites;
    public Sprite[] patrolSprites;   // = walk/run
    public Sprite[] chaseSprites;    // = run fast (если пусто → patrolSprites)
    public Sprite[] attackSprites;
    public Sprite[] rangedAttackSprites;
    public Sprite[] immuneSprites;      // для Stone Golem: immune-фаза
    public Sprite[] hurtSprites;
    public Sprite[] deathSprites;

    [Header("Скорость (секунд на кадр)")]
    [SerializeField] float idleFrameTime = 0.18f;
    [SerializeField] float patrolFrameTime = 0.14f;
    [SerializeField] float chaseFrameTime = 0.1f;
    [SerializeField] float attackFrameTime = 0.12f;
    [SerializeField] float rangedAttackFrameTime = 0.1f;
    [SerializeField] float hurtFrameTime = 0.1f;
    [SerializeField] float deathFrameTime = 0.15f;

    [Header("Направление")]
    [Tooltip("Куда смотрит дефолтный спрайт? True = вправо, False = влево")]
    public bool spriteDefaultFacesRight = true;

    SpriteRenderer spriteRenderer;
    MonsterState currentState;
    Sprite[] currentSprites;
    float frameTime;
    float timer;
    int frameIndex;
    bool animationLocked; // true = не прерывать (attack/death)

    public MonsterState CurrentState => currentState;
    public bool AnimationFinished { get; private set; }

    public event Action<MonsterState> OnAnimationFinished;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // По умолчанию chase = patrol, если не задан
        if (chaseSprites == null || chaseSprites.Length == 0)
            chaseSprites = patrolSprites;
    }

    void Start()
    {
        PlayState(MonsterState.Idle, true);
    }

    void Update()
    {
        if (currentSprites == null || currentSprites.Length == 0)
            return;

        timer += Time.deltaTime;
        if (timer >= frameTime)
        {
            timer = 0f;
            frameIndex++;

            if (frameIndex >= currentSprites.Length)
            {
                if (animationLocked)
                {
                    // Не циклится — остаёмся на последнем кадре
                    frameIndex = currentSprites.Length - 1;
                    AnimationFinished = true;
                    OnAnimationFinished?.Invoke(currentState);
                }
                else
                {
                    frameIndex = 0;
                }
            }

            spriteRenderer.sprite = currentSprites[frameIndex];
        }
    }

    /// <summary>
    /// Проиграть состояние анимации.
    /// force = true — прервать даже заблокированную анимацию.
    /// </summary>
    public void PlayState(MonsterState state, bool force = false)
    {
        if (!force && animationLocked)
            return;

        if (state == currentState && !force)
            return;

        currentState = state;
        AnimationFinished = false;
        timer = 0f;
        frameIndex = 0;

        (currentSprites, frameTime, animationLocked) = state switch
        {
            MonsterState.Idle         => (idleSprites,         idleFrameTime,         false),
            MonsterState.Patrol       => (patrolSprites,       patrolFrameTime,       false),
            MonsterState.Chase        => (chaseSprites,        chaseFrameTime,        false),
            MonsterState.Attack       => (attackSprites,       attackFrameTime,       true),
            MonsterState.RangedAttack => (rangedAttackSprites, rangedAttackFrameTime, true),
            MonsterState.Hurt         => (hurtSprites,         hurtFrameTime,         true),
            MonsterState.Immune       => (immuneSprites,       idleFrameTime,         false), // циклится
            MonsterState.Death        => (deathSprites,        deathFrameTime,        true),
            _                         => (idleSprites,         idleFrameTime,         false)
        };

        if (currentSprites != null && currentSprites.Length > 0)
        {
            spriteRenderer.sprite = currentSprites[0];
            Debug.Log($"[{name}] PlayState: {state} sprite={currentSprites[0]?.name} frames={currentSprites.Length}");
        }
        else
        {
            Debug.LogWarning($"[{name}] PlayState: {state} — НЕТ спрайтов!");
        }
    }

    /// <summary>
    /// Разблокировать анимацию (вызывается после завершения Hurt, чтобы вернуться к Idle).
    /// </summary>
    public void UnlockAnimation()
    {
        animationLocked = false;
    }

    /// <summary>
    /// Длительность анимации в секундах для указанного состояния.
    /// </summary>
    public float GetClipDuration(MonsterState state)
    {
        var (sprites, ftime, _) = state switch
        {
            MonsterState.Idle   => (idleSprites,   idleFrameTime,   false),
            MonsterState.Patrol => (patrolSprites, patrolFrameTime, false),
            MonsterState.Chase  => (chaseSprites,  chaseFrameTime,  false),
            MonsterState.Attack       => (attackSprites,       attackFrameTime,       true),
            MonsterState.RangedAttack => (rangedAttackSprites, rangedAttackFrameTime, true),
            MonsterState.Hurt         => (hurtSprites,         hurtFrameTime,         true),
            MonsterState.Immune       => (immuneSprites,       idleFrameTime,         false),
            MonsterState.Death        => (deathSprites,        deathFrameTime,        true),
            _                         => (idleSprites,         idleFrameTime,         false)
        };
        return (sprites != null ? sprites.Length : 0) * ftime;
    }

    /// <summary>
    /// Время в секундах от начала анимации до указанного кадра (0-based).
    /// </summary>
    public float GetTimeToFrame(MonsterState state, int frameIndex)
    {
        var (sprites, ftime, _) = state switch
        {
            MonsterState.Idle   => (idleSprites,   idleFrameTime,   false),
            MonsterState.Patrol => (patrolSprites, patrolFrameTime, false),
            MonsterState.Chase  => (chaseSprites,  chaseFrameTime,  false),
            MonsterState.Attack       => (attackSprites,       attackFrameTime,       true),
            MonsterState.RangedAttack => (rangedAttackSprites, rangedAttackFrameTime, true),
            MonsterState.Hurt         => (hurtSprites,         hurtFrameTime,         true),
            MonsterState.Immune       => (immuneSprites,       idleFrameTime,         false),
            MonsterState.Death        => (deathSprites,        deathFrameTime,        true),
            _                         => (idleSprites,         idleFrameTime,         false)
        };
        if (sprites == null || sprites.Length == 0 || frameIndex <= 0) return 0f;
        return Mathf.Min(frameIndex, sprites.Length) * ftime;
    }

    /// <summary>
    /// Отразить спрайт по X (направление взгляда).
    /// </summary>
    public void SetFacing(bool facingRight)
    {
        // flipX = true когда спрайт должен быть отражён
        // Если дефолт вправо: facingRight → flip=false, facingLeft → flip=true
        // Если дефолт влево:  наоборот
        spriteRenderer.flipX = spriteDefaultFacesRight ? !facingRight : facingRight;
    }

    /// <summary>
    /// Задать цвет (для эффекта получения урона и т.п.).
    /// </summary>
    public void SetColor(Color color)
    {
        spriteRenderer.color = color;
    }
}
