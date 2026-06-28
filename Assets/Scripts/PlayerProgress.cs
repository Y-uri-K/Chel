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
        if (amount <= 0)
            return;

        Diamonds = Diamonds + amount;
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
