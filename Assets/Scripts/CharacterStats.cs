using System;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("ОЗ")]
    [SerializeField] int baseMaxHealth = 100;

    [Header("Боевые характеристики (%)")]
    [SerializeField] float basePhysicalAttackPercent = 100f;
    [SerializeField] float baseMagicalAttackPercent = 100f;
    [SerializeField] float baseAttackSpeedPercent = 100f;
    [SerializeField] float baseMoveSpeedPercent = 100f;
    [SerializeField] float baseCritChancePercent = 5f;
    [SerializeField] float baseCritDamagePercent = 150f;
    [SerializeField] float baseIncomeMultiplier = 1f;

    int maxHealth;
    float physicalAttackPercent;
    float magicalAttackPercent;
    float attackSpeedPercent;
    float moveSpeedPercent;
    float critChancePercent;
    float critDamagePercent;
    float incomeMultiplier;
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
    public float IncomeMultiplier => incomeMultiplier;

    public float MoveSpeedMultiplier => moveSpeedPercent / 100f;
    public float AttackSpeedMultiplier => attackSpeedPercent / 100f;
    public float PhysicalAttackMultiplier => physicalAttackPercent / 100f;
    public float MagicalAttackMultiplier => magicalAttackPercent / 100f;
    public float CritDamageMultiplier => critDamagePercent / 100f;

    public event Action<CharacterStats> OnHealthChanged;
    public event Action<CharacterStats> OnStatsChanged;
    public event Action<CharacterStats> OnDeath;

    void Awake()
    {
        maxHealth = baseMaxHealth;
        physicalAttackPercent = basePhysicalAttackPercent;
        magicalAttackPercent = baseMagicalAttackPercent;
        attackSpeedPercent = baseAttackSpeedPercent;
        moveSpeedPercent = baseMoveSpeedPercent;
        critChancePercent = baseCritChancePercent;
        critDamagePercent = baseCritDamagePercent;
        incomeMultiplier = baseIncomeMultiplier;
        currentHealth = maxHealth;
    }

    void Start()
    {
        RecalculateFromShop();
    }

    public void RecalculateFromShop()
    {
        int previousMaxHealth = maxHealth > 0 ? maxHealth : baseMaxHealth;
        float healthRatio = previousMaxHealth > 0 ? (float)currentHealth / previousMaxHealth : 1f;

        maxHealth = baseMaxHealth + Mathf.RoundToInt(ShopUpgradeItem.GetTotalBonus(ShopUpgradeType.Health, PlayerProgress.GetShopUpgradeLevel(ShopUpgradeType.Health)));
        physicalAttackPercent = basePhysicalAttackPercent + ShopUpgradeItem.GetTotalBonus(ShopUpgradeType.Damage, PlayerProgress.GetShopUpgradeLevel(ShopUpgradeType.Damage));
        critChancePercent = baseCritChancePercent + ShopUpgradeItem.GetTotalBonus(ShopUpgradeType.CritChance, PlayerProgress.GetShopUpgradeLevel(ShopUpgradeType.CritChance));
        moveSpeedPercent = baseMoveSpeedPercent + ShopUpgradeItem.GetTotalBonus(ShopUpgradeType.Speed, PlayerProgress.GetShopUpgradeLevel(ShopUpgradeType.Speed));
        incomeMultiplier = baseIncomeMultiplier + ShopUpgradeItem.GetTotalBonus(ShopUpgradeType.MultMoney, PlayerProgress.GetShopUpgradeLevel(ShopUpgradeType.MultMoney));

        magicalAttackPercent = baseMagicalAttackPercent;
        attackSpeedPercent = baseAttackSpeedPercent;
        critDamagePercent = baseCritDamagePercent;

        currentHealth = Mathf.Clamp(Mathf.RoundToInt(maxHealth * healthRatio), 1, maxHealth);

        OnStatsChanged?.Invoke(this);
        OnHealthChanged?.Invoke(this);
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

    public int CalculatePhysicalDamage(int baseDamage, out bool isCritical)
    {
        float damage = baseDamage * PhysicalAttackMultiplier;
        isCritical = RollCrit();
        if (isCritical)
            damage *= CritDamageMultiplier;

        return Mathf.Max(1, Mathf.RoundToInt(damage));
    }

    public void ResetToFullHealth()
    {
        RecalculateFromShop();
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(this);
    }
}
