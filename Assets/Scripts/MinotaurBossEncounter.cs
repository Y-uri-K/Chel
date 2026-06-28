using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Камень блокирует проход, пока жив Minotaur. После убийства босса камень исчезает.
/// При респавне игрока на уровне камень и босс восстанавливаются.
/// </summary>
public class MinotaurBossEncounter : MonoBehaviour
{
    static readonly List<MinotaurBossEncounter> Active = new();

    [SerializeField] GameObject minotaurPrefab;
    [SerializeField] string bossName = "Minotaur";

    Vector3 stonePosition;
    Vector3 bossSpawnPosition;
    Quaternion bossSpawnRotation;
    bool bossSpawnCached;
    GameObject bossInstance;
    MonsterStats bossStats;

    void Awake()
    {
        stonePosition = transform.position;
        Active.Add(this);
    }

    void OnDestroy()
    {
        Active.Remove(this);
        UnbindBoss();
    }

    void Start()
    {
        CacheBossSpawnFromScene();
        BindBoss(FindBossInScene());
        UpdateStoneVisibility();
    }

    public static void ResetForNewRun()
    {
        foreach (var encounter in Active)
            encounter.RestoreForNewRun();
    }

    void CacheBossSpawnFromScene()
    {
        var boss = FindBossInScene();
        if (boss == null)
            return;

        bossSpawnPosition = boss.transform.position;
        bossSpawnRotation = boss.transform.rotation;
        bossSpawnCached = true;
    }

    GameObject FindBossInScene()
    {
        var boss = GameObject.Find(bossName);
        return boss;
    }

    void BindBoss(GameObject boss)
    {
        UnbindBoss();
        if (boss == null)
            return;

        bossInstance = boss;
        bossStats = boss.GetComponent<MonsterStats>();
        if (bossStats != null)
            bossStats.OnDeath += HandleBossDeath;
    }

    void UnbindBoss()
    {
        if (bossStats != null)
            bossStats.OnDeath -= HandleBossDeath;

        bossStats = null;
    }

    void HandleBossDeath(MonsterStats _)
    {
        SetStoneVisible(false);
    }

    void UpdateStoneVisibility()
    {
        bool bossAlive = bossStats != null && !bossStats.IsDead;
        SetStoneVisible(bossAlive);
    }

    void SetStoneVisible(bool visible)
    {
        transform.position = stonePosition;
        gameObject.SetActive(visible);
    }

    void RestoreForNewRun()
    {
        RespawnBoss();
        SetStoneVisible(true);
    }

    void RespawnBoss()
    {
        UnbindBoss();

        if (bossInstance != null)
        {
            Destroy(bossInstance);
            bossInstance = null;
        }

        if (!bossSpawnCached || minotaurPrefab == null)
            return;

        bossInstance = Instantiate(minotaurPrefab, bossSpawnPosition, bossSpawnRotation);
        bossInstance.name = bossName;
        BindBoss(bossInstance);
    }
}
