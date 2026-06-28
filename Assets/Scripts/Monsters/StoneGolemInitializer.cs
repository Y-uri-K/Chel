using UnityEngine;

/// <summary>
/// Принудительно выставляет статы Stone Golem при старте.
/// Решает проблему с генератором, который не сохраняет значения в префаб.
/// </summary>
public class StoneGolemInitializer : MonoBehaviour
{
    void Awake()
    {
        var stats = GetComponent<MonsterStats>();
        if (stats == null) return;

        stats.SetBaseStats(
            maxHealth: 250,
            damage: 30,
            moveSpeed: 220f,
            attackRange: 145f,
            attackCooldown: 2f,
            chaseRange: 50000f,
            patrolRange: 1200f
        );

        Destroy(this); // одноразовый
    }
}
