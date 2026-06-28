using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUpgradeItem : MonoBehaviour
{
    const int MaxLevel = 5;

    static readonly Dictionary<ShopUpgradeType, ShopUpgradeItem> Configs = new();
    static readonly Dictionary<ShopUpgradeType, UpgradeConfigData> ConfigData = new();

    struct UpgradeConfigData
    {
        public int initialCost;
        public float costGrowth;
        public float initialBonus;
        public float bonusGrowth;
    }

    [SerializeField] ShopUpgradeType upgradeType;
    [SerializeField] int initialCost = 10;
    [SerializeField] float costGrowth = 2f;
    [SerializeField] float initialBonus = 5f;
    [SerializeField] float bonusGrowth = 2f;

    Button buyButton;
    TMP_Text costText;
    Text legacyCostText;
    TMP_Text bonusText;
    Text legacyBonusText;
    readonly GameObject[] levelIndicators = new GameObject[MaxLevel];
    CharacterStats characterStats;

    public ShopUpgradeType UpgradeType => upgradeType;

    void Awake()
    {
        AutoFindReferences();
        RegisterConfig();
    }

    void RegisterConfig()
    {
        Configs[upgradeType] = this;
        ConfigData[upgradeType] = new UpgradeConfigData
        {
            initialCost = initialCost,
            costGrowth = costGrowth,
            initialBonus = initialBonus,
            bonusGrowth = bonusGrowth
        };
    }

    void OnDestroy()
    {
        if (Configs.TryGetValue(upgradeType, out var existing) && existing == this)
            Configs.Remove(upgradeType);
    }

    public void Initialize(CharacterStats stats)
    {
        RegisterConfig();
        characterStats = stats;
        BindBuyButton();
        Refresh();
    }

    void OnEnable()
    {
        PlayerProgress.DiamondsChanged += HandleDiamondsChanged;
        BindBuyButton();
        Refresh();
    }

    void BindBuyButton()
    {
        if (buyButton == null)
            return;

        buyButton.onClick.RemoveListener(TryPurchase);
        buyButton.onClick.AddListener(TryPurchase);
    }

    void OnDisable()
    {
        PlayerProgress.DiamondsChanged -= HandleDiamondsChanged;
    }

    void HandleDiamondsChanged(int _)
    {
        Refresh();
    }

    void AutoFindReferences()
    {
        buyButton = FindButtonInChildren(transform, "Button");

        var costTransform = FindTransformInChildren(transform, "cost");
        if (costTransform != null)
        {
            costText = costTransform.GetComponent<TMP_Text>();
            legacyCostText = costTransform.GetComponent<Text>();
            DisableRaycast(costText);
            DisableRaycast(legacyCostText);
        }

        for (int i = 0; i < MaxLevel; i++)
            levelIndicators[i] = FindTransformInChildren(transform, $"level {i + 1}")?.gameObject;

        var bonusTransform = FindTransformInChildrenIgnoreCase(transform, "text");
        if (bonusTransform != null)
        {
            bonusText = bonusTransform.GetComponent<TMP_Text>();
            legacyBonusText = bonusTransform.GetComponent<Text>();
            DisableRaycast(bonusText);
            DisableRaycast(legacyBonusText);
        }
    }

    static void DisableRaycast(TMP_Text text)
    {
        if (text != null)
            text.raycastTarget = false;
    }

    static void DisableRaycast(Text text)
    {
        if (text != null)
            text.raycastTarget = false;
    }

    static Transform FindTransformInChildrenIgnoreCase(Transform root, string objectName)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(child.name, objectName, System.StringComparison.OrdinalIgnoreCase))
                return child;
        }

        return null;
    }

    static Transform FindTransformInChildren(Transform root, string objectName)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child;
        }

        return null;
    }

    static Button FindButtonInChildren(Transform root, string objectName)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child.GetComponent<Button>();
        }

        return null;
    }

    public static float GetTotalBonus(ShopUpgradeType type, int level)
    {
        if (level <= 0)
            return 0f;

        EnsureConfigData(type);
        if (!ConfigData.TryGetValue(type, out var data))
            return 0f;

        return CalculateTotalBonus(data.initialBonus, data.bonusGrowth, level);
    }

    public static int GetUpgradeCost(ShopUpgradeType type, int currentLevel)
    {
        if (currentLevel >= MaxLevel)
            return 0;

        EnsureConfigData(type);
        if (!ConfigData.TryGetValue(type, out var data))
            return 0;

        return Mathf.Max(1, Mathf.RoundToInt(data.initialCost * Mathf.Pow(data.costGrowth, currentLevel)));
    }

    public static int GetTotalSpent(ShopUpgradeType type, int level)
    {
        if (level <= 0)
            return 0;

        int total = 0;
        for (int i = 0; i < level; i++)
            total += GetUpgradeCost(type, i);

        return total;
    }

    public static int GetTotalSpentOnAllUpgrades()
    {
        int total = 0;
        foreach (ShopUpgradeType type in System.Enum.GetValues(typeof(ShopUpgradeType)))
        {
            int level = PlayerProgress.GetShopUpgradeLevel(type);
            total += GetTotalSpent(type, level);
        }

        return total;
    }

    static void EnsureConfigData(ShopUpgradeType type)
    {
        if (ConfigData.ContainsKey(type))
            return;

        var items = FindObjectsByType<ShopUpgradeItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in items)
            item.RegisterConfig();
    }

    static float CalculateTotalBonus(float bonus, float growth, int level)
    {
        float total = 0f;
        for (int i = 0; i < level; i++)
            total += bonus * Mathf.Pow(growth, i);

        return total;
    }

    public int GetUpgradeCost(int currentLevel)
    {
        if (currentLevel >= MaxLevel)
            return 0;

        return Mathf.Max(1, Mathf.RoundToInt(initialCost * Mathf.Pow(costGrowth, currentLevel)));
    }

    float CalculateTotalBonus(int level)
    {
        return CalculateTotalBonus(initialBonus, bonusGrowth, level);
    }

    public void Refresh()
    {
        int level = PlayerProgress.GetShopUpgradeLevel(upgradeType);
        UpdateLevelIndicators(level);
        UpdateBonusLabel(level);
        UpdateCostLabel(level);
        UpdateButtonState(level);
    }

    void UpdateBonusLabel(int level)
    {
        string label = FormatBonusLabel(CalculateTotalBonus(level));
        if (bonusText != null)
            bonusText.text = label;
        else if (legacyBonusText != null)
            legacyBonusText.text = label;
    }

    string FormatBonusLabel(float bonus)
    {
        switch (upgradeType)
        {
            case ShopUpgradeType.Damage:
                return $"Урон +{bonus:0}%";
            case ShopUpgradeType.Health:
                return $"Здоровье +{bonus:0}";
            case ShopUpgradeType.CritChance:
                return $"Шанс крита +{bonus:0}%";
            case ShopUpgradeType.Speed:
                return $"Скорость бега +{bonus:0}%";
            case ShopUpgradeType.MultMoney:
                return bonus > 0f ? $"Мн. дохода x{1f + bonus:0.##}" : "Мн. дохода x1";
            default:
                return $"+{bonus:0}";
        }
    }

    void UpdateLevelIndicators(int level)
    {
        for (int i = 0; i < MaxLevel; i++)
        {
            if (levelIndicators[i] != null)
                levelIndicators[i].SetActive(i < level);
        }
    }

    void UpdateCostLabel(int level)
    {
        string label;
        if (level >= MaxLevel)
            label = "MAX";
        else
            label = $"Стоимость: {GetUpgradeCost(level)}";

        if (costText != null)
            costText.text = label;
        else if (legacyCostText != null)
            legacyCostText.text = label;
    }

    void UpdateButtonState(int level)
    {
        if (buyButton == null)
            return;

        bool canBuy = level < MaxLevel && PlayerProgress.Diamonds >= GetUpgradeCost(level);
        buyButton.interactable = canBuy;
    }

    void TryPurchase()
    {
        int level = PlayerProgress.GetShopUpgradeLevel(upgradeType);
        if (level >= MaxLevel)
            return;

        int cost = GetUpgradeCost(level);
        if (!PlayerProgress.TrySpendDiamonds(cost))
            return;

        PlayerProgress.SetShopUpgradeLevel(upgradeType, level + 1);
        characterStats?.RecalculateFromShop();

        var panel = GetComponentInParent<ShopPanelController>();
        if (panel != null)
            panel.RefreshAll();
        else
            Refresh();
    }
}
