using UnityEngine;
using UnityEngine.UI;

public class CharacteristicsPanelView : MonoBehaviour
{
    [SerializeField] CharacterStats stats;
    [SerializeField] Text statsText;

    void Awake()
    {
        if (statsText == null)
            statsText = GetComponent<Text>();

        if (stats == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                stats = player.GetComponent<CharacterStats>();
        }
    }

    void OnEnable()
    {
        if (stats != null)
        {
            stats.OnHealthChanged += HandleStatsChanged;
            stats.OnStatsChanged += HandleStatsChanged;
        }

        PlayerProgress.DiamondsChanged += HandleDiamondsChanged;
        Refresh();
    }

    void OnDisable()
    {
        if (stats != null)
        {
            stats.OnHealthChanged -= HandleStatsChanged;
            stats.OnStatsChanged -= HandleStatsChanged;
        }

        PlayerProgress.DiamondsChanged -= HandleDiamondsChanged;
    }

    void HandleStatsChanged(CharacterStats changedStats)
    {
        Refresh();
    }

    void HandleDiamondsChanged(int _)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (statsText == null)
            return;

        if (stats == null)
        {
            statsText.text = "Характеристики\n\nИгрок не найден";
            return;
        }

        statsText.text = BuildStatsText(stats);
    }

    static string BuildStatsText(CharacterStats characterStats)
    {
        return
            "Характеристики\n\n" +
            $"ОЗ: {characterStats.CurrentHealth}/{characterStats.MaxHealth}\n" +
            $"Физ. атака: {FormatPercent(characterStats.PhysicalAttackPercent)}\n" +
            $"Скорость движения: {FormatPercent(characterStats.MoveSpeedPercent)}\n" +
            $"Шанс крита: {FormatPercent(characterStats.CritChancePercent)}\n" +
            $"Множитель дохода: X{FormatMultiplier(characterStats.IncomeMultiplier)}\n" +
            $"Алмазы: {PlayerProgress.Diamonds}";
    }

    static string FormatMultiplier(float value)
    {
        return value % 1f == 0f ? $"{value:0}" : $"{value:0.#}";
    }

    static string FormatPercent(float value)
    {
        return value % 1f == 0f ? $"{value:0}%" : $"{value:0.#}%";
    }
}
