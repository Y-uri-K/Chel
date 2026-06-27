using System;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("ОЗ")]
    [SerializeField] int maxHealth = 100;

    [Header("Боевые характеристики (%)")]
    [SerializeField] float physicalAttackPercent = 100f;
    [SerializeField] float magicalAttackPercent = 100f;
    [SerializeField] float attackSpeedPercent = 100f;
    [SerializeField] float moveSpeedPercent = 100f;
    [SerializeField] float critChancePercent = 5f;
    [SerializeField] float critDamagePercent = 150f;

    int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float HealthRatio => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    public float PhysicalAttackPercent => physicalAttackPercent;
    public float MagicalAttackPercent => magicalAttackPercent;
    public float AttackSpeedPercent => attackSpeedPercent;
    public float MoveSpeedPercent => moveSpeedPercent;
    public float CritChancePercent => critChancePercent;
    public float CritDamagePercent => critDamagePercent;

    public float MoveSpeedMultiplier => moveSpeedPercent / 100f;
    public float AttackSpeedMultiplier => attackSpeedPercent / 100f;
    public float PhysicalAttackMultiplier => physicalAttackPercent / 100f;
    public float MagicalAttackMultiplier => magicalAttackPercent / 100f;
    public float CritDamageMultiplier => critDamagePercent / 100f;

    public event Action<CharacterStats> OnHealthChanged;
    public event Action<CharacterStats> OnDeath;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || currentHealth <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke(this);

        if (currentHealth <= 0)
            OnDeath?.Invoke(this);
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || currentHealth <= 0)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(this);
    }

    public void SetMaxHealth(int value, bool fillToMax = false)
    {
        maxHealth = Mathf.Max(1, value);
        if (fillToMax)
            currentHealth = maxHealth;

        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(this);
    }

    public bool RollCrit()
    {
        return UnityEngine.Random.value < critChancePercent / 100f;
    }

    public void ResetToFullHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(this);
    }
}
