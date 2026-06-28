using System;
using UnityEngine;

/// <summary>
/// Характеристики монстра с масштабированием от уровня.
/// </summary>
public class MonsterStats : MonoBehaviour
{
    [Header("База (уровень 1)")]
    [SerializeField] int baseMaxHealth = 30;
    [SerializeField] int baseDamage = 8;
    [SerializeField] float baseMoveSpeed = 1.8f;
    [SerializeField] float baseAttackRange = 1.5f;
    [SerializeField] float baseAttackCooldown = 1.2f;
    [SerializeField] float baseChaseRange = 6f;
    [SerializeField] float basePatrolRange = 4f;

    [Header("Масштабирование")]
    [SerializeField] float healthPerLevel = 0.25f;
    [SerializeField] float damagePerLevel = 0.18f;
    [SerializeField] float speedPerLevel = 0.05f;
    [SerializeField] float rangePerLevel = 0.03f;

    [Header("Уровень")]
    [SerializeField] int level = 1;

    [Header("Immune-механика")]
    public int hitsToActivateImmune = 999;
    public int hitsToDeactivateImmune = 999;

    int currentHealth;
    bool isDead;
    int hitCount;
    bool isImmune;
    int immuneHitsReceived;

    public int MaxHealth         => Mathf.RoundToInt(baseMaxHealth * LevelMultiplier(healthPerLevel));
    public int Damage            => Mathf.RoundToInt(baseDamage * LevelMultiplier(damagePerLevel));
    public float MoveSpeed       => baseMoveSpeed * LevelMultiplier(speedPerLevel);
    public float AttackRange     => baseAttackRange * LevelMultiplier(rangePerLevel);
    public float AttackCooldown  => baseAttackCooldown;
    public float ChaseRange      => baseChaseRange * LevelMultiplier(rangePerLevel);
    public float PatrolRange     => basePatrolRange;
    public int Level             => level;

    public int CurrentHealth     => currentHealth;
    public bool IsDead           => isDead;
    public bool IsImmune         => isImmune;
    public float HealthRatio     => MaxHealth > 0 ? (float)currentHealth / MaxHealth : 0f;
    public int HitsToDeactivate  => hitsToDeactivateImmune;
    public int ImmuneHitsReceived => immuneHitsReceived;

    public event Action<MonsterStats> OnHealthChanged;
    public event Action<MonsterStats> OnDeath;
    public event Action<MonsterStats> OnImmuneActivated;
    public event Action<MonsterStats> OnImmuneDeactivated;

    float LevelMultiplier(float perLevel) => 1f + perLevel * (level - 1);

    void Awake() { currentHealth = MaxHealth; }

    public void SetLevel(int newLevel, bool fillHealth = true)
    {
        level = Mathf.Max(1, newLevel);
        if (fillHealth) currentHealth = MaxHealth;
        else currentHealth = Mathf.Min(currentHealth, MaxHealth);
        OnHealthChanged?.Invoke(this);
    }

    public bool TakeDamage(int amount)
    {
        if (amount <= 0 || isDead) return false;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke(this);

        if (currentHealth <= 0)
        {
            isDead = true;
            OnDeath?.Invoke(this);
            return true;
        }
        return false;
    }

    public void ResetToFullHealth()
    {
        isDead = false;
        isImmune = false;
        hitCount = 0;
        immuneHitsReceived = 0;
        currentHealth = MaxHealth;
        OnHealthChanged?.Invoke(this);
    }

    public void SetBaseStats(int maxHealth, int damage, float moveSpeed,
        float attackRange, float attackCooldown, float chaseRange, float patrolRange)
    {
        baseMaxHealth = maxHealth; baseDamage = damage; baseMoveSpeed = moveSpeed;
        baseAttackRange = attackRange; baseAttackCooldown = attackCooldown;
        baseChaseRange = chaseRange; basePatrolRange = patrolRange;
    }
#if UNITY_EDITOR
    void OnValidate() { if (!Application.isPlaying) level = Mathf.Max(1, level); }
#endif
}
