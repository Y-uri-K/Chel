using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ShopPanelController : MonoBehaviour
{
    ShopUpgradeItem[] items;
    CharacterStats characterStats;
    Button resetShopButton;

    void Awake()
    {
        items = GetComponentsInChildren<ShopUpgradeItem>(true);

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            characterStats = player.GetComponent<CharacterStats>();

        foreach (var item in items)
            item.Initialize(characterStats);

        BindResetButton();
    }

    void OnEnable()
    {
        characterStats?.RecalculateFromShop();
        RefreshAll();
    }

    void OnDestroy()
    {
        if (resetShopButton != null)
            resetShopButton.onClick.RemoveListener(ResetShop);
    }

    void BindResetButton()
    {
        resetShopButton = FindButtonInShop("ResetShopButton");
        if (resetShopButton == null)
            return;

        resetShopButton.onClick.RemoveListener(ResetShop);
        resetShopButton.onClick.AddListener(ResetShop);
        UpdateResetButtonState();
    }

    Button FindButtonInShop(string objectName)
    {
        var searchRoot = transform.parent != null ? transform.parent : transform;
        foreach (var child in searchRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != objectName)
                continue;

            return child.GetComponent<Button>();
        }

        return null;
    }

    void ResetShop()
    {
        if (!PlayerProgress.HasAnyShopUpgrades())
            return;

        PlayerProgress.ResetShopUpgradesAndRefund();
        characterStats?.RecalculateFromShop();
        RefreshAll();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void RefreshAll()
    {
        if (items == null)
            return;

        foreach (var item in items)
            item.Refresh();

        UpdateResetButtonState();
    }

    void UpdateResetButtonState()
    {
        if (resetShopButton == null)
            return;

        bool canReset = PlayerProgress.HasAnyShopUpgrades();
        resetShopButton.interactable = canReset;
        SetResetButtonRaycast(canReset);
    }

    void SetResetButtonRaycast(bool canReset)
    {
        foreach (var graphic in resetShopButton.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic == resetShopButton.targetGraphic)
                graphic.raycastTarget = canReset;
            else
                graphic.raycastTarget = false;
        }

        foreach (var tmp in resetShopButton.GetComponentsInChildren<TMP_Text>(true))
            tmp.raycastTarget = false;
    }
}
