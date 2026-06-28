using System;
using UnityEngine;

public static class PlayerProgress
{
    public const string DiamondsKey = "PlayerDiamonds";
    private const string ChestOpenedKeyPrefix = "ChestOpened_";

    public static event Action<int> DiamondsChanged;

    public static int Diamonds
    {
        get => PlayerPrefs.GetInt(DiamondsKey, 0);
        private set
        {
            PlayerPrefs.SetInt(DiamondsKey, value);
            PlayerPrefs.Save();
            DiamondsChanged?.Invoke(value);
        }
    }

    public static void AddDiamonds(int amount)
    {
        AddDiamonds(amount, GetIncomeMultiplier());
    }

    public static void AddDiamonds(int amount, float incomeMultiplier)
    {
        if (amount <= 0)
            return;

        int finalAmount = Mathf.Max(0, Mathf.RoundToInt(amount * Mathf.Max(0f, incomeMultiplier)));
        if (finalAmount <= 0)
            return;

        Diamonds = Diamonds + finalAmount;
    }

    public static bool TrySpendDiamonds(int amount)
    {
        if (amount <= 0 || Diamonds < amount)
            return false;

        Diamonds = Diamonds - amount;
        return true;
    }

    public static void ResetDiamonds()
    {
        Diamonds = 0;
    }

    public static int GetShopUpgradeLevel(ShopUpgradeType type)
    {
        return PlayerPrefs.GetInt(ShopUpgradeKey(type), 0);
    }

    public static void SetShopUpgradeLevel(ShopUpgradeType type, int level)
    {
        PlayerPrefs.SetInt(ShopUpgradeKey(type), Mathf.Clamp(level, 0, 5));
        PlayerPrefs.Save();
    }

    static string ShopUpgradeKey(ShopUpgradeType type) => "ShopUpgrade_" + type;

    public static bool HasAnyShopUpgrades()
    {
        foreach (ShopUpgradeType type in Enum.GetValues(typeof(ShopUpgradeType)))
        {
            if (GetShopUpgradeLevel(type) > 0)
                return true;
        }

        return false;
    }

    public static int ResetShopUpgradesAndRefund()
    {
        int refund = ShopUpgradeItem.GetTotalSpentOnAllUpgrades();
        if (refund <= 0)
            return 0;

        foreach (ShopUpgradeType type in Enum.GetValues(typeof(ShopUpgradeType)))
            SetShopUpgradeLevel(type, 0);

        Diamonds = Diamonds + refund;
        return refund;
    }

    static float GetIncomeMultiplier()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return 1f;

        var stats = player.GetComponent<CharacterStats>();
        return stats != null ? stats.IncomeMultiplier : 1f;
    }

    public static bool IsChestOpened(string chestId)
    {
        if (string.IsNullOrEmpty(chestId))
            return false;

        return PlayerPrefs.GetInt(ChestOpenedKeyPrefix + chestId, 0) == 1;
    }

    public static void MarkChestOpened(string chestId)
    {
        if (string.IsNullOrEmpty(chestId))
            return;

        PlayerPrefs.SetInt(ChestOpenedKeyPrefix + chestId, 1);
        PlayerPrefs.Save();
    }
}
