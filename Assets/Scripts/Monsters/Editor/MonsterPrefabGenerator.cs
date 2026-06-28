using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor-утилита: создаёт префабы для всех типов монстров.
/// Tools → Monsters → Generate All Monster Prefabs
/// </summary>
public class MonsterPrefabGenerator : EditorWindow
{
    [MenuItem("Tools/Monsters/Generate All Monster Prefabs")]
    public static void GenerateAll()
    {
        GenerateMonsterPrefab("FlyingEye", "Assets/monsters/Sprites/Flying eye",
            new[] { "Flight" }, new[] { "Flight" }, new[] { "Flight" },
            new[] { "Attack1", "Attack2" }, new[] { "Take Hit" }, new[] { "Death" });

        GenerateMonsterPrefab("Goblin", "Assets/monsters/Sprites/Goblin",
            new[] { "Idle" }, new[] { "Run" }, new[] { "Run" },
            new[] { "Attack1", "Attack2" }, new[] { "Take Hit" }, new[] { "Death" });

        GenerateMonsterPrefab("Mushroom", "Assets/monsters/Sprites/Mushroom",
            new[] { "Idle" }, new[] { "Run" }, new[] { "Run" },
            new[] { "Attack1", "Attack2" }, new[] { "Take Hit" }, new[] { "Death" });

        GenerateMonsterPrefab("Skeleton", "Assets/monsters/Sprites/Skeleton",
            new[] { "Idle" }, new[] { "Walk" }, new[] { "Walk" },
            new[] { "Attack1", "Attack2" }, new[] { "Take Hit" }, new[] { "Death" });

        GenerateStoneGolem();
        GenerateDemonSlime();
        GenerateMinotaur();
        GenerateFlyingDemon();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MonsterPrefabGenerator] Готово: 8 префабов в Assets/Prefabs/Monsters/");
    }

    static void GenerateStoneGolem()
    {
        // Создаём с нуля (не от ASE-префаба — из-за бага с сохранением статов)
        var go = new GameObject("StoneGolem");
        go.transform.localScale = new Vector3(500f, 500f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 11;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 10f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Грузим спрайты из ASE (высокое качество)
        string asePath = "Assets/monsters/Mecha-stone Golem 0.1/ASE files/flatten_character.aseprite";
        var allSprites = new List<Sprite>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(asePath))
            if (asset is Sprite s) allSprites.Add(s);

        // Числовая сортировка по последнему числу в имени (кадры 0,1,2...10,11...)
        allSprites.Sort((a, b) =>
        {
            int Num(string n)
            {
                int lastUs = n.LastIndexOf('_');
                return lastUs >= 0 && int.TryParse(n.Substring(lastUs + 1), out int x) ? x : int.MaxValue;
            }
            return Num(a.name).CompareTo(Num(b.name));
        });

        Debug.Log($"Stone Golem: {allSprites.Count} ASE-спрайтов. Первые 5: {string.Join(", ", allSprites.GetRange(0, Mathf.Min(5, allSprites.Count)).ConvertAll(s => s.name))}");

        // Разбиваем по ASE-тегам в имени спрайта
        Sprite[] ByTag(string tag) => allSprites.FindAll(s => s.name.ToLower().Contains(tag)).ToArray();

        var idle   = ByTag("idle");
        var walk   = ByTag("walk");
        var attack = ByTag("attack");
        var hurt   = ByTag("hurt");
        var death  = ByTag("death");
        Sprite[] rangedAttack = null;
        Sprite[] immune = null;

        // Всегда числовое разбиение — ASE-теги ломают ByTag
        {
            int C(int v) => Mathf.Min(v, allSprites.Count - 1);
            Sprite[] S(int s, int e) => allSprites.GetRange(C(s), C(e) - C(s) + 1).ToArray();

            var idleAll = S(0, 11);
            idle = idleAll; walk = idleAll;
            attack = S(29, 35);
            var immuneFrames = S(21, 28);
            hurt = immuneFrames;
            immune = immuneFrames;
            death = S(53, 66);
            rangedAttack = S(36, 42);
        }

        // Детальный лог
        void LogGroup(string label, Sprite[] sprites)
        {
            if (sprites.Length == 0) { Debug.Log($"  {label}: (пусто)"); return; }
            Debug.Log($"  {label} [{sprites.Length}]: {sprites[0].name} … {sprites[sprites.Length-1].name}");
        }
        Debug.Log("Stone Golem — распределение спрайтов по состояниям:");
        LogGroup("Idle", idle);
        LogGroup("Walk/Patrol", walk);
        LogGroup("Melee", attack);
        LogGroup("Ranged", rangedAttack);
        LogGroup("Hurt", hurt);
        LogGroup("Death", death);

        // Создаём префаб статичного лазерного луча (кадры 0–13, не летит)
        string laserBeamPath = CreateLaserBeamPrefab();

        var anim = go.AddComponent<MonsterAnimation>();
        anim.idleSprites = idle;
        anim.patrolSprites = walk;
        anim.chaseSprites = walk;
        anim.attackSprites = attack;
        anim.rangedAttackSprites = rangedAttack;
        anim.immuneSprites = immune;
        anim.hurtSprites = hurt;
        anim.deathSprites = death;

        var ai = go.AddComponent<MonsterAI>();
        ai.rangedAttackRange = 1200f;
        ai.rangedAttackCooldown = 10f;
        ai.laserDistanceTrigger = 500f;
        if (!string.IsNullOrEmpty(laserBeamPath))
            ai.laserBeamPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(laserBeamPath);

        // Лазер спавнится у головы (верх коллайдера)
        ai.laserSpawnOffset = new Vector2(15f, -110f);

        if (idle.Length > 0 && sr != null) sr.sprite = idle[0];

        var col = go.AddComponent<PolygonCollider2D>();
        if (sr != null && sr.sprite != null) FitColliderToSprite(col, sr.sprite);

        var stats = go.AddComponent<MonsterStats>();
        go.AddComponent<MonsterHPBar>();
        go.AddComponent<StoneGolemInitializer>(); // статы применит в Awake

        // Статы напрямую через SerializedObject (надёжнее SetBaseStats)
        var so = new SerializedObject(stats);
        so.FindProperty("baseMaxHealth").intValue = 250;
        so.FindProperty("baseDamage").intValue = 30;
        so.FindProperty("baseMoveSpeed").floatValue = 220f;
        so.FindProperty("baseAttackRange").floatValue = 300f;
        so.FindProperty("baseAttackCooldown").floatValue = 2f;
        so.FindProperty("baseChaseRange").floatValue = 50000f;
        so.FindProperty("basePatrolRange").floatValue = 1200f;
        so.ApplyModifiedProperties();
        Debug.Log($"  StoneGolem STATS: HP=250 spd=220 chsRng=50000");

        string dir = "Assets/Prefabs/Monsters";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        PrefabUtility.SaveAsPrefabAsset(go, $"{dir}/StoneGolem.prefab");
        DestroyImmediate(go);

        Debug.Log($"  StoneGolem.prefab (всего:{allSprites.Count} idle:{idle.Length} atk:{attack.Length} death:{death.Length} scale:500)");
    }

    static void GenerateMonsterPrefab(string name, string folder,
        string[] idleN, string[] patrolN, string[] chaseN,
        string[] attackN, string[] hurtN, string[] deathN)
    {
        GenerateMonsterPrefabInternal(name, 300f,
            LoadSprites(folder, idleN), LoadSprites(folder, patrolN), LoadSprites(folder, chaseN),
            LoadSprites(folder, attackN), LoadSprites(folder, hurtN), LoadSprites(folder, deathN));
    }

    static void GenerateMonsterPrefabInternal(string name, float scale,
        Sprite[] idle, Sprite[] patrol, Sprite[] chase,
        Sprite[] attack, Sprite[] hurt, Sprite[] death)
    {
        var go = new GameObject(name);
        go.transform.localScale = new Vector3(scale, scale, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 11;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 10f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var stats = go.AddComponent<MonsterStats>();
        var anim = go.AddComponent<MonsterAnimation>();
        go.AddComponent<MonsterAI>();
        go.AddComponent<MonsterHPBar>();

        anim.idleSprites = idle; anim.patrolSprites = patrol; anim.chaseSprites = chase;
        anim.attackSprites = attack; anim.hurtSprites = hurt; anim.deathSprites = death;

        if (idle != null && idle.Length > 0) sr.sprite = idle[0];

        var col = go.AddComponent<PolygonCollider2D>();
        if (sr.sprite != null) FitColliderToSprite(col, sr.sprite);

        ApplyBaseStats(stats, name);

        string dir = "Assets/Prefabs/Monsters";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        PrefabUtility.SaveAsPrefabAsset(go, $"{dir}/{name}.prefab");
        DestroyImmediate(go);

        Debug.Log($"  {name}.prefab (idle:{idle?.Length ?? 0} atk:{attack?.Length ?? 0} scale:{scale})");
    }

    static Sprite[] LoadSprites(string folder, string[] names)
    {
        var list = new List<Sprite>();
        foreach (var n in names)
        {
            foreach (var guid in AssetDatabase.FindAssets($"{n} t:Texture", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is Sprite s && !list.Contains(s)) list.Add(s);
                break;
            }
        }
        return list.ToArray();
    }

    static void FitColliderToSprite(PolygonCollider2D col, Sprite sprite)
    {
        int n = sprite.GetPhysicsShapeCount();
        if (n > 0)
        {
            col.pathCount = n;
            var shape = new List<Vector2>();
            for (int i = 0; i < n; i++) { sprite.GetPhysicsShape(i, shape); col.SetPath(i, shape.ToArray()); shape.Clear(); }
        }
        else
        {
            var b = sprite.bounds;
            col.pathCount = 1;
            col.SetPath(0, new[] { new Vector2(b.min.x, b.min.y), new Vector2(b.min.x, b.max.y),
                                   new Vector2(b.max.x, b.max.y), new Vector2(b.max.x, b.min.y) });
        }
    }

    /// <summary>
    /// Создаёт LaserBeam.prefab — статичный луч: кадры 0–13 из laser.ase, проигрываются один раз.
    /// </summary>
    static string CreateLaserBeamPrefab()
    {
        string laserAsePath = "Assets/monsters/Mecha-stone Golem 0.1/ASE files/laser.ase";
        var allSprites = new List<Sprite>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(laserAsePath))
            if (asset is Sprite s) allSprites.Add(s);

        if (allSprites.Count == 0) { Debug.LogWarning("LaserBeam: ASE-спрайты не найдены"); return null; }

        // Числовая сортировка по последнему числу в имени
        allSprites.Sort((a, b) =>
        {
            int Num(string n) { int u = n.LastIndexOf('_'); return u >= 0 && int.TryParse(n[(u+1)..], out int x) ? x : int.MaxValue; }
            return Num(a.name).CompareTo(Num(b.name));
        });

        // Берём только кадры 0–13
        int frameCount = Mathf.Min(14, allSprites.Count);
        var beamFrames = allSprites.GetRange(0, frameCount).ToArray();
        Debug.Log($"LaserBeam: кадры 0–{frameCount-1} из {allSprites.Count}, первый={beamFrames[0]?.name}, последний={beamFrames[frameCount-1]?.name}");

        var go = new GameObject("LaserBeam");
        go.transform.localScale = new Vector3(500f, 500f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = beamFrames[0];
        sr.sortingOrder = 20;

        var anim = go.AddComponent<LaserAnimation>();
        anim.frames = beamFrames;
        anim.frameTime = 0.04f;
        anim.loop = false; // проиграть один раз и удалиться

        string dir = "Assets/Prefabs/Monsters";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string path = $"{dir}/LaserBeam.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        AssetDatabase.Refresh();
        Debug.Log($"  LaserBeam.prefab ({frameCount} кадров, loop=false)");
        return path;
    }

    /// <summary>
    /// Создаёт LaserProjectile.prefab из Laser_sheet.png (35 кадров).
    /// </summary>
    static string CreateLaserPrefab()
    {
        // Грузим спрайты из laser.ase (качественная ASE-анимация)
        string laserAsePath = "Assets/monsters/Mecha-stone Golem 0.1/ASE files/laser.ase";
        var sprites = new List<Sprite>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(laserAsePath))
            if (asset is Sprite s) sprites.Add(s);

        if (sprites.Count == 0) { Debug.LogWarning("Laser: ASE-спрайты не найдены"); return null; }
        sprites.Sort((a, b) => a.name.CompareTo(b.name));

        // Проджектайл: Laser_sheet.png (35 кадров)
        string projPath = "Assets/monsters/Mecha-stone Golem 0.1/weapon PNG/Laser_sheet.png";
        var projSprites = new List<Sprite>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(projPath))
            if (asset is Sprite s) projSprites.Add(s);
        projSprites.Sort((a, b) =>
        {
            int Num(string n) { int u = n.LastIndexOf('_'); return u >= 0 && int.TryParse(n[(u+1)..], out int x) ? x : int.MaxValue; }
            return Num(a.name).CompareTo(Num(b.name));
        });

        var go = new GameObject("LaserProjectile");
        go.transform.localScale = new Vector3(500f, 500f, 1f);

        // Проджектайл (Laser_sheet.png) — на корне
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = projSprites.Count > 0 ? projSprites[0] : sprites[0];
        sr.sortingOrder = 15;
        var projAnim = go.AddComponent<LaserAnimation>();
        projAnim.frames = projSprites.Count > 0 ? projSprites.ToArray() : sprites.ToArray();
        projAnim.frameTime = 0.05f;

        // Луч лазера (laser.ase) — дочерний объект
        var beamGo = new GameObject("LaserBeam");
        beamGo.transform.SetParent(go.transform, false);
        beamGo.transform.localPosition = Vector3.zero;
        beamGo.transform.localScale = Vector3.one;
        var beamSr = beamGo.AddComponent<SpriteRenderer>();
        beamSr.sortingOrder = 14;
        var beamAnim = beamGo.AddComponent<LaserAnimation>();
        beamAnim.frames = sprites.ToArray();
        beamAnim.frameTime = 0.04f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 0.5f);

        go.AddComponent<LaserProjectile>();

        string dir = "Assets/Prefabs/Monsters";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string path = $"{dir}/LaserProjectile.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        Debug.Log($"  LaserProjectile.prefab (лазер:{sprites.Count} кадров, проджектайл:{projSprites.Count} кадров)");
        return path;
    }

    static AudioClip LoadAttackSound(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/music/{fileName}.mp3");
    }

    static void ApplyBaseStats(MonsterStats stats, string name)
    {
        switch (name)
        {
            case "FlyingEye":  stats.SetBaseStats(60, 10, 240f, 140f, 1.0f, 500f, 250f); stats.SetDiamondReward(5); break;
            case "Goblin":     stats.SetBaseStats(90, 12, 200f, 120f, 1.3f, 420f, 200f); stats.SetDiamondReward(8); stats.SetAttackSound(LoadAttackSound("goblin hit")); break;
            case "Mushroom":   stats.SetBaseStats(130, 18, 140f, 110f, 1.6f, 350f, 170f); stats.SetDiamondReward(10); stats.SetAttackSound(LoadAttackSound("slap")); break;
            case "Skeleton":   stats.SetBaseStats(80, 14, 160f, 130f, 1.1f, 450f, 220f); stats.SetDiamondReward(7); break;
            case "StoneGolem": stats.SetBaseStats(250, 30, 220f, 300f, 2.0f, 2400f, 1200f); stats.SetDiamondReward(25); break;
            case "DemonSlime": stats.SetBaseStats(200, 22, 180f, 250f, 1.5f, 600f, 300f); stats.SetDiamondReward(20); break;
            case "Minotaur":   stats.SetBaseStats(180, 25, 160f, 160f, 1.8f, 500f, 250f); stats.SetDiamondReward(15); break;
            case "FlyingDemon": stats.SetBaseStats(120, 15, 150f, 275f, 1.5f, 700f, 350f); stats.SetDiamondReward(12); break;
        }
    }

    static Sprite[] LoadAllSpritesFromFolder(string folder)
    {
        var list = new List<Sprite>();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture", new[] { folder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite s && !list.Contains(s)) list.Add(s);
        }
        // Сортировка по числу в имени файла
        list.Sort((a, b) =>
        {
            int Num(string n)
            {
                int lastUs = n.LastIndexOf('_');
                return lastUs >= 0 && int.TryParse(n.Substring(lastUs + 1).Split('.')[0], out int x) ? x : int.MaxValue;
            }
            return Num(a.name).CompareTo(Num(b.name));
        });
        return list.ToArray();
    }

    static void GenerateDemonSlime()
    {
        string asePath = "Assets/monsters/boss_demon_slime_FREE_v1.0/aseprite/demon_slime_FREE_v1.0.aseprite";
        var allSprites = new List<Sprite>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(asePath))
            if (asset is Sprite s) allSprites.Add(s);

        if (allSprites.Count == 0)
        {
            Debug.LogError("DemonSlime: ASE-спрайты не найдены! Путь: " + asePath);
            return;
        }

        // Сортировка: извлекаем ВСЕ числа из имени и сравниваем лексикографически
        allSprites.Sort((a, b) =>
        {
            var numsA = ExtractNumbers(a.name);
            var numsB = ExtractNumbers(b.name);
            int count = Mathf.Min(numsA.Count, numsB.Count);
            for (int i = 0; i < count; i++)
            {
                if (numsA[i] != numsB[i])
                    return numsA[i].CompareTo(numsB[i]);
            }
            return numsA.Count.CompareTo(numsB.Count);
        });

        Debug.Log($"DemonSlime ASE: всего {allSprites.Count} спрайтов. Первые 5: {string.Join(", ", allSprites.GetRange(0, Mathf.Min(5, allSprites.Count)).ConvertAll(s => s.name))}");

        // Разбивка по диапазонам кадров (как в ASE)
        Sprite[] Range(int from, int to)
        {
            from = Mathf.Clamp(from, 0, allSprites.Count - 1);
            to = Mathf.Clamp(to, from, allSprites.Count - 1);
            return allSprites.GetRange(from, to - from + 1).ToArray();
        }

        var idle   = Range(0, 5);
        var walk   = Range(6, 17);
        var cleave = Range(18, 32);
        var hurt   = Range(33, 37);
        var death  = Range(38, 59);

        Debug.Log($"DemonSlime: idle={idle.Length} walk={walk.Length} cleave={cleave.Length} hurt={hurt.Length} death={death.Length}");

        var go = new GameObject("DemonSlime");
        go.transform.localScale = new Vector3(300f, 300f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 11;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 10f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var stats = go.AddComponent<MonsterStats>();
        var anim = go.AddComponent<MonsterAnimation>();
        anim.spriteDefaultFacesRight = false; // спрайты слайма смотрят влево
        go.AddComponent<MonsterAI>();
        go.AddComponent<MonsterHPBar>();

        anim.idleSprites = idle;
        anim.patrolSprites = walk;
        anim.chaseSprites = walk;
        anim.attackSprites = cleave;
        anim.hurtSprites = hurt;
        anim.deathSprites = death;

        if (idle.Length > 0) sr.sprite = idle[0];

        var col = go.AddComponent<PolygonCollider2D>();
        if (sr.sprite != null) FitColliderToSprite(col, sr.sprite);

        stats.SetBaseStats(200, 22, 180f, 250f, 1.5f, 600f, 300f);

        string dir = "Assets/Prefabs/Monsters";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        PrefabUtility.SaveAsPrefabAsset(go, $"{dir}/DemonSlime.prefab");
        DestroyImmediate(go);

        Debug.Log($"  DemonSlime.prefab (idle:{idle.Length} atk:{cleave.Length} death:{death.Length} scale:300)");
    }

    static List<int> ExtractNumbers(string name)
    {
        var nums = new List<int>();
        int i = 0;
        while (i < name.Length)
        {
            if (char.IsDigit(name[i]))
            {
                int start = i;
                while (i < name.Length && char.IsDigit(name[i])) i++;
                nums.Add(int.Parse(name.Substring(start, i - start)));
            }
            else i++;
        }
        return nums;
    }

    static void GenerateMinotaur()
    {
        string asePath = "Assets/monsters/mino_v1.1_free/minotaur.aseprite";
        var allSprites = new List<Sprite>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(asePath))
            if (asset is Sprite s) allSprites.Add(s);

        if (allSprites.Count == 0)
        {
            Debug.LogError("Minotaur: ASE-спрайты не найдены! Путь: " + asePath);
            return;
        }

        allSprites.Sort((a, b) =>
        {
            var numsA = ExtractNumbers(a.name);
            var numsB = ExtractNumbers(b.name);
            int count = Mathf.Min(numsA.Count, numsB.Count);
            for (int i = 0; i < count; i++)
                if (numsA[i] != numsB[i]) return numsA[i].CompareTo(numsB[i]);
            return numsA.Count.CompareTo(numsB.Count);
        });

        Debug.Log($"Minotaur ASE: всего {allSprites.Count} спрайтов.");

        Sprite[] Range(int from, int to)
        {
            from = Mathf.Clamp(from, 0, allSprites.Count - 1);
            to = Mathf.Clamp(to, from, allSprites.Count - 1);
            return allSprites.GetRange(from, to - from + 1).ToArray();
        }

        var idle  = Range(0, 15);
        var walk  = Range(16, 27);
        var atk   = Range(28, 43);
        var hurt  = Range(0, 3);   // fallback: первые кадры idle
        var death = Range(4, 7);   // fallback: кадры idle

        Debug.Log($"Minotaur: idle={idle.Length} walk={walk.Length} atk={atk.Length}");

        var go = new GameObject("Minotaur");
        go.transform.localScale = new Vector3(350f, 350f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 11;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 10f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var stats = go.AddComponent<MonsterStats>();
        var anim = go.AddComponent<MonsterAnimation>();
        anim.spriteDefaultFacesRight = false; // спрайты минотавра смотрят влево
        go.AddComponent<MonsterAI>();
        go.AddComponent<MonsterHPBar>();

        anim.idleSprites = idle;
        anim.patrolSprites = walk;
        anim.chaseSprites = walk;
        anim.attackSprites = atk;
        anim.hurtSprites = hurt;
        anim.deathSprites = death;

        if (idle.Length > 0) sr.sprite = idle[0];

        var col = go.AddComponent<PolygonCollider2D>();
        if (sr.sprite != null) FitColliderToSprite(col, sr.sprite);

        stats.SetBaseStats(180, 25, 160f, 160f, 1.8f, 500f, 250f);

        string dir = "Assets/Prefabs/Monsters";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        PrefabUtility.SaveAsPrefabAsset(go, $"{dir}/Minotaur.prefab");
        DestroyImmediate(go);

        Debug.Log($"  Minotaur.prefab (idle:{idle.Length} walk:{walk.Length} atk:{atk.Length} scale:350)");
    }

    static string CreateFireballPrefab()
    {
        string projPath = "Assets/monsters/Flying Demon 2D Pixel Art/Sprites/projectile.png";
        var sprites = new List<Sprite>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(projPath))
            if (asset is Sprite s) sprites.Add(s);

        if (sprites.Count == 0) { Debug.LogWarning("Fireball: спрайт не найден"); return null; }

        var go = new GameObject("Fireball");
        go.transform.localScale = new Vector3(200f, 200f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprites[0];
        sr.sortingOrder = 20;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        // Коллайдер по размеру спрайта
        if (sprites.Count > 0 && sr.sprite != null)
        {
            var b = sr.sprite.bounds;
            col.size = new Vector2(b.size.x, b.size.y);
            col.offset = b.center;
        }
        else
        {
            col.size = new Vector2(0.8f, 0.8f);
        }

        var lp = go.AddComponent<LaserProjectile>();
        lp.speed = 525f; // +50% от базовых 350

        string dir = "Assets/Prefabs/Monsters";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string path = $"{dir}/Fireball.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        AssetDatabase.Refresh();
        Debug.Log("  Fireball.prefab создан");
        return path;
    }

    static void GenerateFlyingDemon()
    {
        string basePath = "Assets/monsters/Flying Demon 2D Pixel Art/Sprites/with_outline";

        Sprite[] LoadSheet(string name)
        {
            var path = $"{basePath}/{name}.png";
            var list = new List<Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite s) list.Add(s);
            // Сортировка по числу в имени (IDLE_0, IDLE_1, ...)
            list.Sort((a, b) =>
            {
                var na = ExtractNumbers(a.name);
                var nb = ExtractNumbers(b.name);
                int c = Mathf.Min(na.Count, nb.Count);
                for (int i = 0; i < c; i++)
                    if (na[i] != nb[i]) return na[i].CompareTo(nb[i]);
                return na.Count.CompareTo(nb.Count);
            });
            return list.ToArray();
        }

        var idle  = LoadSheet("IDLE");
        var fly   = LoadSheet("FLYING");
        var atk   = LoadSheet("ATTACK");
        var hurt  = LoadSheet("HURT");
        var death = LoadSheet("DEATH");

        Debug.Log($"FlyingDemon: idle={idle.Length} fly={fly.Length} atk={atk.Length} hurt={hurt.Length} death={death.Length}");

        // Создаём файрбол
        string fireballPath = CreateFireballPrefab();

        var go = new GameObject("FlyingDemon");
        go.transform.localScale = new Vector3(170f, 170f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 11;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 0f; // летает
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var stats = go.AddComponent<MonsterStats>();
        var anim = go.AddComponent<MonsterAnimation>();
        anim.spriteDefaultFacesRight = false; // смотрит влево
        var ai = go.AddComponent<MonsterAI>();
        go.AddComponent<MonsterHPBar>();

        anim.idleSprites = idle;
        anim.patrolSprites = fly;
        anim.chaseSprites = fly;
        anim.attackSprites = atk;
        anim.hurtSprites = hurt;
        anim.deathSprites = death;

        ai.rangedAttackRange = 800f;
        ai.rangedAttackCooldown = 3f;
        ai.laserSpawnOffset = new Vector2(0f, 0f);
        if (!string.IsNullOrEmpty(fireballPath))
            ai.rangedProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fireballPath);

        if (idle.Length > 0) sr.sprite = idle[0];

        var col = go.AddComponent<PolygonCollider2D>();
        if (sr.sprite != null) FitColliderToSprite(col, sr.sprite);

        stats.SetBaseStats(120, 15, 150f, 275f, 1.5f, 700f, 350f);

        string dir = "Assets/Prefabs/Monsters";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        PrefabUtility.SaveAsPrefabAsset(go, $"{dir}/FlyingDemon.prefab");
        DestroyImmediate(go);

        Debug.Log($"  FlyingDemon.prefab (idle:{idle.Length} fly:{fly.Length} atk:{atk.Length} scale:170)");
    }
}
