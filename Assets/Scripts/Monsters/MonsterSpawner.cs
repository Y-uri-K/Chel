using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Тип монстра для спавна.
/// </summary>
public enum MonsterType
{
    FlyingEye,
    Goblin,
    Mushroom,
    Skeleton,
    StoneGolem,
    DemonSlime,
    Minotaur,
    FlyingDemon
}

/// <summary>
/// Конфигурация одной точки спавна.
/// </summary>
[Serializable]
public class SpawnPoint
{
    [Tooltip("Позиция спавна (мировые координаты)")]
    public Vector2 position;

    [Tooltip("Тип монстра")]
    public MonsterType monsterType = MonsterType.Goblin;

    [Tooltip("Уровень (1+). Статы масштабируются от уровня.")]
    [Range(1, 20)]
    public int level = 1;

    [Tooltip("Время до респавна после смерти (сек). 0 = без респавна.")]
    public float respawnDelay = 10f;

    [Tooltip("Макс. одновременных монстров с этой точки")]
    [Range(1, 5)]
    public int maxInstances = 1;

    [Tooltip("Радиус активации: спавн при входе игрока")]
    public float activationRadius = 12f;

    [Tooltip("Спавнить сразу при загрузке сцены (игнорирует радиус)")]
    public bool spawnImmediately;

    // Runtime
    [HideInInspector] public List<MonsterAI> aliveMonsters = new List<MonsterAI>();
    [HideInInspector] public float respawnTimer;
    public bool initialSpawnDone;
}

/// <summary>
/// Система спавна монстров. Управляет точками, создаёт/респавнит монстров.
/// </summary>
public class MonsterSpawner : MonoBehaviour
{
    [Header("Точки спавна")]
    [SerializeField] SpawnPoint[] spawnPoints;

    [Header("Префабы монстров (перетащи сюда из Assets/Prefabs/Monsters/)")]
    [SerializeField] GameObject flyingEyePrefab;
    [SerializeField] GameObject goblinPrefab;
    [SerializeField] GameObject mushroomPrefab;
    [SerializeField] GameObject skeletonPrefab;
    [SerializeField] GameObject stoneGolemPrefab;
    [SerializeField] GameObject demonSlimePrefab;
    [SerializeField] GameObject minotaurPrefab;
    [SerializeField] GameObject flyingDemonPrefab;

    [Header("Контейнер (авто-создаётся, если пусто)")]
    [SerializeField] Transform monstersContainer;

    [Header("Диагностика")]
    [SerializeField] bool verboseLogging = true;

    Transform player;
    bool playerFound;
    bool diagnosticsShown;

    void Awake()
    {
        if (monstersContainer == null)
        {
            var containerGo = new GameObject("MonstersContainer");
            monstersContainer = containerGo.transform;
            monstersContainer.SetParent(transform);
        }

        // Проверяем префабы сразу
        ValidatePrefabs();

        // Проверяем точки спавна
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[MonsterSpawner] Нет ни одной точки спавна! Добавь точки в массив Spawn Points в инспекторе.");
        }
        else
        {
            Debug.Log($"[MonsterSpawner] Загружено {spawnPoints.Length} точек спавна.");
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                var sp = spawnPoints[i];
                Debug.Log($"  Точка[{i}]: {sp.monsterType} Lv.{sp.level} pos={sp.position} radius={sp.activationRadius}");
            }
        }

        StartCoroutine(FindPlayerRoutine());
    }

    void ValidatePrefabs()
    {
        if (flyingEyePrefab == null) Debug.LogWarning("[MonsterSpawner] FlyingEye Prefab не назначен!");
        if (goblinPrefab == null) Debug.LogWarning("[MonsterSpawner] Goblin Prefab не назначен!");
        if (mushroomPrefab == null) Debug.LogWarning("[MonsterSpawner] Mushroom Prefab не назначен!");
        if (skeletonPrefab == null) Debug.LogWarning("[MonsterSpawner] Skeleton Prefab не назначен!");
        if (stoneGolemPrefab == null) Debug.LogWarning("[MonsterSpawner] StoneGolem Prefab не назначен!");
        if (demonSlimePrefab == null) Debug.LogWarning("[MonsterSpawner] DemonSlime Prefab не назначен!");
        if (minotaurPrefab == null) Debug.LogWarning("[MonsterSpawner] Minotaur Prefab не назначен!");
        if (flyingDemonPrefab == null) Debug.LogWarning("[MonsterSpawner] FlyingDemon Prefab не назначен!");

        bool anyAssigned = flyingEyePrefab != null || goblinPrefab != null
            || mushroomPrefab != null || skeletonPrefab != null || stoneGolemPrefab != null
            || demonSlimePrefab != null || minotaurPrefab != null || flyingDemonPrefab != null;
        if (!anyAssigned)
        {
            Debug.LogError("[MonsterSpawner] Не назначен НИ ОДИН префаб! " +
                "Сгенерируй их через Tools > Monsters > Generate All Monster Prefabs, затем перетащи в поля инспектора.");
        }
    }

    IEnumerator FindPlayerRoutine()
    {
        Debug.Log("[MonsterSpawner] Ищу игрока (тег 'Player')...");
        int attempts = 0;
        while (!playerFound)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerFound = true;
                Debug.Log($"[MonsterSpawner] ✓ Игрок найден: {player.name} на позиции {player.position}");
            }
            else
            {
                attempts++;
                if (attempts == 1)
                    Debug.LogWarning("[MonsterSpawner] Игрок с тегом 'Player' не найден. Убедись, что на префабе/объекте игрока стоит тег 'Player'.");
                if (attempts % 10 == 0)
                    Debug.LogWarning($"[MonsterSpawner] Всё ещё ищу игрока... (попытка {attempts})");
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    void Update()
    {
        if (!playerFound || player == null)
        {
            if (!diagnosticsShown && Time.timeSinceLevelLoad > 3f)
            {
                diagnosticsShown = true;
                Debug.LogError("[MonsterSpawner] Игрок не найден спустя 3 секунды. " +
                    "Проверь: 1) тег 'Player' на объекте игрока, " +
                    "2) игрок существует на сцене.");
            }
            return;
        }

        // Режим однократной диагностики
        if (!diagnosticsShown)
        {
            diagnosticsShown = true;
            Debug.Log($"[MonsterSpawner] Начинаю спавн. Игрок: {player.position}, точек: {spawnPoints.Length}");
        }

        if (spawnPoints == null)
            return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            var sp = spawnPoints[i];
            if (sp == null)
                continue;

            // Чистим мёртвых
            for (int j = sp.aliveMonsters.Count - 1; j >= 0; j--)
            {
                if (sp.aliveMonsters[j] == null)
                {
                    sp.aliveMonsters.RemoveAt(j);
                }
                else
                {
                    var st = sp.aliveMonsters[j].GetComponent<MonsterStats>();
                    if (st != null && st.IsDead)
                        sp.aliveMonsters.RemoveAt(j);
                }
            }

            // Таймер респавна
            if (sp.respawnTimer > 0f)
            {
                sp.respawnTimer -= Time.deltaTime;
                continue;
            }

            // Нужен ли спавн?
            int needCount = sp.maxInstances - sp.aliveMonsters.Count;
            if (needCount <= 0)
                continue;

            // Расстояние до игрока
            float distToPlayer = Vector2.Distance(player.position, sp.position);
            bool inRange = sp.spawnImmediately || distToPlayer <= sp.activationRadius;

            if (verboseLogging && !sp.initialSpawnDone && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[MonsterSpawner] Точка[{i}] {sp.monsterType}: дист={distToPlayer:F1}, " +
                    $"в_радиусе={inRange}, нужно={needCount}, живо={sp.aliveMonsters.Count}/{sp.maxInstances}");
            }

            if (inRange)
            {
                Debug.Log($"[MonsterSpawner] ✔ Спавн точки[{i}]: {sp.monsterType} Lv{sp.level} (immediate={sp.spawnImmediately} dist={distToPlayer:F1})");
                sp.initialSpawnDone = true;
                SpawnMonster(sp, i);
            }
        }
    }

    void SpawnMonster(SpawnPoint sp, int pointIndex)
    {
        GameObject prefab = GetPrefab(sp.monsterType);
        if (prefab == null)
        {
            Debug.LogError($"[MonsterSpawner] Нет префаба для {sp.monsterType} (точка {pointIndex}). Назначь его в инспекторе!");
            return;
        }

        var instance = Instantiate(prefab, sp.position, Quaternion.identity, monstersContainer);
        instance.name = $"{sp.monsterType}_Lv{sp.level}";

        var stats = instance.GetComponent<MonsterStats>();
        if (stats == null)
        {
            Debug.LogError($"[MonsterSpawner] Префаб {sp.monsterType} НЕ содержит MonsterStats! Пересоздай префаб через Tools > Monsters.");
            Destroy(instance);
            return;
        }

        stats.SetLevel(sp.level, fillHealth: true);

        var ai = instance.GetComponent<MonsterAI>();
        if (ai != null)
            sp.aliveMonsters.Add(ai);

        // Респавн при смерти
        stats.OnDeath += (s) =>
        {
            if (sp.respawnDelay > 0f)
                sp.respawnTimer = sp.respawnDelay;
        };

        // Диагностика видимости
        var sr = instance.GetComponent<SpriteRenderer>();
        var anim = instance.GetComponent<MonsterAnimation>();
        string spriteInfo = sr == null ? "НЕТ SpriteRenderer!" :
            sr.sprite == null ? "SpriteRenderer ЕСТЬ, но sprite=NULL" :
            $"спрайт='{sr.sprite.name}' цвет={sr.color} слой={sr.sortingLayerName}:{sr.sortingOrder}";

        string animInfo = "";
        if (anim != null)
        {
            animInfo = $"\n  Анимации: idle={anim.idleSprites?.Length ?? 0}, patrol={anim.patrolSprites?.Length ?? 0}, chase={anim.chaseSprites?.Length ?? 0}, attack={anim.attackSprites?.Length ?? 0}, hurt={anim.hurtSprites?.Length ?? 0}, death={anim.deathSprites?.Length ?? 0}";
        }

        Debug.Log($"[MonsterSpawner] ✔ Заспавнен {instance.name} в точке {pointIndex}\n" +
            $"  pos={sp.position}, HP={stats.MaxHealth}, урон={stats.Damage}\n" +
            $"  {spriteInfo}{animInfo}");
    }

    GameObject GetPrefab(MonsterType type)
    {
        return type switch
        {
            MonsterType.FlyingEye => flyingEyePrefab,
            MonsterType.Goblin    => goblinPrefab,
            MonsterType.Mushroom  => mushroomPrefab,
            MonsterType.Skeleton  => skeletonPrefab,
            MonsterType.StoneGolem => stoneGolemPrefab,
            MonsterType.DemonSlime => demonSlimePrefab,
            MonsterType.Minotaur   => minotaurPrefab,
            MonsterType.FlyingDemon => flyingDemonPrefab,
            _ => null
        };
    }

    #region Gizmos

    void OnDrawGizmos()
    {
        if (spawnPoints == null)
            return;

        foreach (var sp in spawnPoints)
        {
            if (sp == null)
                continue;

            // Activation radius
            Gizmos.color = new Color(0.4f, 0.6f, 1f, 0.12f);
            Gizmos.DrawSphere(sp.position, sp.activationRadius);

            // Spawn position
            Color typeColor = sp.monsterType switch
            {
                MonsterType.FlyingEye => new Color(0.7f, 0.3f, 1f),
                MonsterType.Goblin    => new Color(0.2f, 0.8f, 0.3f),
                MonsterType.Mushroom  => new Color(1f, 0.5f, 0.2f),
                MonsterType.Skeleton   => new Color(0.7f, 0.7f, 0.7f),
                MonsterType.StoneGolem => new Color(0.5f, 0.4f, 0.3f),
                MonsterType.DemonSlime => new Color(0.8f, 0.2f, 0.8f),
                MonsterType.Minotaur   => new Color(0.8f, 0.5f, 0.2f),
                MonsterType.FlyingDemon => new Color(1f, 0.2f, 0.2f),
                _ => Color.white
            };

            Gizmos.color = typeColor;
            Gizmos.DrawWireSphere(sp.position, 0.5f);
            Gizmos.DrawSphere(sp.position, 0.15f);

#if UNITY_EDITOR
            var style = new GUIStyle();
            style.normal.textColor = typeColor;
            style.fontSize = 10;
            style.alignment = TextAnchor.MiddleCenter;
            Handles.Label(sp.position + Vector2.up * 0.7f,
                $"{sp.monsterType} Lv.{sp.level}", style);
#endif
        }
    }

    #endregion
}
