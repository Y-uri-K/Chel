using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MonsterStats), typeof(MonsterAnimation), typeof(Rigidbody2D))]
public class MonsterAI : MonoBehaviour
{
    [SerializeField] MonsterStats stats;
    [SerializeField] MonsterAnimation anim;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] Collider2D bodyCollider;

    [Header("Патруль")]
    [SerializeField] float patrolWaitTime = 1.5f;
    [SerializeField] float patrolPointRadius = 8f;

    [Header("Атака")]
    [SerializeField] float attackHitMoment = 0.4f;
    [SerializeField] float rangedAttackHitMoment = 0.5f;

    [Header("Ranged")]
    public GameObject rangedProjectilePrefab;
    public GameObject laserBeamPrefab;
    public float rangedAttackRange = 2000f;
    public float rangedAttackCooldown = 3f;
    public float laserDistanceTrigger = 500f;
    public Vector2 laserSpawnOffset = new Vector2(0f, 0f);

    Transform player;
    CharacterStats playerStats;
    Vector2 spawnPoint;
    MonsterState state;
    float attackCooldownTimer;
    float rangedCooldownTimer;
    bool facingRight = true;
    bool isDead;
    float lastPlayerDistance;

    Vector2 patrolLeftPoint, patrolRightPoint, currentPatrolTarget;
    float patrolWaitTimer;
    bool isWaitingAtPatrolPoint;
    Coroutine hurtRoutine;
    Coroutine attackRoutine;

    static bool _layerSetupDone;
    const string MonsterLayer = "Mobs";

    void Awake()
    {
        SetupMonsterLayer();
        int layer = LayerMask.NameToLayer(MonsterLayer);
        if (layer >= 0) gameObject.layer = layer;

        stats = GetComponent<MonsterStats>();
        anim = GetComponent<MonsterAnimation>();
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    static void SetupMonsterLayer()
    {
        if (_layerSetupDone) return;
        _layerSetupDone = true;
        int layer = LayerMask.NameToLayer(MonsterLayer);
        if (layer < 0) { Debug.LogWarning("[MonsterAI] Layer 'Monster' не найден — создай его в Project Settings > Tags and Layers"); return; }
        Physics2D.IgnoreLayerCollision(layer, layer, true);
    }

    void Start()
    {
        spawnPoint = transform.position;
        CalculatePatrolPoints();
        FindPlayer();
        if (player) FacePlayer();
        IgnorePlayerCollisions();
        stats.OnDeath += HandleDeath;
        stats.OnHealthChanged += HandleHealthChanged;
        SetState(MonsterState.Idle);
    }

    void IgnorePlayerCollisions()
    {
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        if (bodyCollider == null || !player) return;

        // Отключаем коллизии со ВСЕМИ коллайдерами игрока
        foreach (var pc in player.GetComponentsInChildren<Collider2D>(true))
        {
            if (pc != null && !pc.isTrigger)
                Physics2D.IgnoreCollision(bodyCollider, pc, true);
        }
    }

    void OnDestroy()
    {
        if (stats)
        {
            stats.OnDeath -= HandleDeath;
            stats.OnHealthChanged -= HandleHealthChanged;
        }
    }

    void FindPlayer()
    {
        var obj = GameObject.FindGameObjectWithTag("Player");
        if (!obj) return;
        player = obj.transform;
        playerStats = obj.GetComponent<CharacterStats>();
    }

    void CalculatePatrolPoints()
    {
        float r = stats.PatrolRange;
        patrolLeftPoint = spawnPoint + Vector2.left * r;
        patrolRightPoint = spawnPoint + Vector2.right * r;
        currentPatrolTarget = patrolRightPoint;
    }

    void Update()
    {
        if (isDead) return;
        if (!player) { FindPlayer(); return; }

        attackCooldownTimer -= Time.deltaTime;
        rangedCooldownTimer -= Time.deltaTime;

        switch (state)
        {
            case MonsterState.Idle:   UpdateIdle();   break;
            case MonsterState.Patrol: UpdatePatrol(); break;
            case MonsterState.Chase:  UpdateChase();  break;
        }

        TryRangedAttack();
        FacePlayer();
    }

    void TryRangedAttack()
    {
        if (!laserBeamPrefab && !rangedProjectilePrefab) return;
        if (state == MonsterState.Attack || state == MonsterState.RangedAttack ||
            state == MonsterState.Hurt || state == MonsterState.Death) return;

        float d = Dist();

        // Условие 1: кулдаун готов и игрок в зоне лазера (attackRange < d ≤ rangedAttackRange)
        bool cooldownReady = rangedCooldownTimer <= 0f;
        bool inLaserRange = d > stats.AttackRange && d <= rangedAttackRange;

        // Условие 2: игрок пересёк порог laserDistanceTrigger (был ближе → стал дальше)
        bool crossedThreshold = lastPlayerDistance > 0f
            && lastPlayerDistance <= laserDistanceTrigger
            && d > laserDistanceTrigger;

        lastPlayerDistance = d;

        if (inLaserRange && (cooldownReady || crossedThreshold))
            StartRangedAttack();
    }

    void UpdateIdle()
    {
        float d = Dist();
        if (d <= stats.AttackRange && attackCooldownTimer <= 0f) StartAttack();
        else if (d <= stats.ChaseRange) SetState(MonsterState.Chase);
        else SetState(MonsterState.Patrol);
    }

    void UpdatePatrol()
    {
        float d = Dist();
        if (d <= stats.AttackRange && attackCooldownTimer <= 0f) { StartAttack(); return; }
        if (d <= stats.ChaseRange) { SetState(MonsterState.Chase); return; }

        if (isWaitingAtPatrolPoint)
        {
            patrolWaitTimer -= Time.deltaTime;
            StopMoving();
            if (patrolWaitTimer <= 0f)
            {
                isWaitingAtPatrolPoint = false;
                currentPatrolTarget = currentPatrolTarget == patrolLeftPoint ? patrolRightPoint : patrolLeftPoint;
                anim.PlayState(MonsterState.Patrol);
            }
            return;
        }

        if (Vector2.Distance(transform.position, currentPatrolTarget) <= patrolPointRadius)
        {
            isWaitingAtPatrolPoint = true;
            patrolWaitTimer = patrolWaitTime;
            anim.PlayState(MonsterState.Idle);
            return;
        }

        MoveTowards(currentPatrolTarget, stats.MoveSpeed * 0.5f);
        anim.PlayState(MonsterState.Patrol);
    }

    void UpdateChase()
    {
        float d = Dist();
        if (d <= stats.AttackRange && attackCooldownTimer <= 0f) { StartAttack(); return; }
        if (d > stats.ChaseRange * 1.5f) { SetState(MonsterState.Patrol); return; }
        MoveTowards(player.position, stats.MoveSpeed);
        anim.PlayState(MonsterState.Chase);
    }

    void StartAttack()
    {
        SetState(MonsterState.Attack);
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        StopMoving();
        float dur = anim.GetClipDuration(MonsterState.Attack);
        float hitTime = dur * 0.5f; // середина анимации

        if (hitTime > 0f)
            yield return new WaitForSeconds(hitTime);
        DoAttackHit();
        float rem = dur - hitTime;
        if (rem > 0f) yield return new WaitForSeconds(rem);
        attackCooldownTimer = stats.AttackCooldown;
        anim.UnlockAnimation();
        attackRoutine = null;
        float d = Dist();
        SetState(d <= stats.AttackRange ? MonsterState.Idle : d <= stats.ChaseRange ? MonsterState.Chase : MonsterState.Patrol);
    }

    void DoAttackHit()
    {
        if (isDead) return;

        stats.PlayAttackSound();

        if (!playerStats) return;
        if (Dist() <= stats.AttackRange * 1.4f)
        {
            playerStats.TakeDamage(stats.Damage);
            DamagePopup.Show(player.position, stats.Damage, Color.yellow);
        }
    }

    void StartRangedAttack()
    {
        SetState(MonsterState.RangedAttack);
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(RangedAttackRoutine());
    }

    IEnumerator RangedAttackRoutine()
    {
        StopMoving();
        float dur = anim.GetClipDuration(MonsterState.RangedAttack);
        yield return new WaitForSeconds(dur * rangedAttackHitMoment);
        SpawnLaser();
        float rem = dur * (1f - rangedAttackHitMoment);
        if (rem > 0f) yield return new WaitForSeconds(rem);
        rangedCooldownTimer = rangedAttackCooldown;
        anim.UnlockAnimation();
        attackRoutine = null;
        float d = Dist();
        SetState(d <= stats.AttackRange ? MonsterState.Idle : d <= stats.ChaseRange ? MonsterState.Chase : MonsterState.Patrol);
    }

    void SpawnLaser()
    {
        if (!player) return;

        if (rangedProjectilePrefab != null)
        {
            // Проджектайл (файрбол): летит в игрока
            Vector2 spawnPos = transform.position;
            if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
            if (bodyCollider != null)
                spawnPos = bodyCollider.bounds.center + (Vector3)laserSpawnOffset;
            else
                spawnPos += laserSpawnOffset;

            var proj = Instantiate(rangedProjectilePrefab, spawnPos, Quaternion.identity);
            float dirX = facingRight ? 1f : -1f;
            proj.GetComponent<LaserProjectile>()?.Fire(new Vector2(dirX, 0f));
            // Флип спрайта: вместо rotation используем flipX
            proj.transform.rotation = Quaternion.identity;
            var sr = proj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.flipX = (dirX > 0f);
            return;
        }

        if (laserBeamPrefab != null)
        {
            // Статичный луч (StoneGolem): спавн у головы
            if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();

            Vector2 spawnPos = transform.position;
            if (bodyCollider != null)
            {
                spawnPos = new Vector2(
                    bodyCollider.bounds.center.x + laserSpawnOffset.x,
                    bodyCollider.bounds.max.y + laserSpawnOffset.y);
            }
            else
            {
                spawnPos += laserSpawnOffset;
            }

            var beam = Instantiate(laserBeamPrefab, spawnPos, Quaternion.identity);

            float dirX = player.position.x > transform.position.x ? 1f : -1f;
            var scale = beam.transform.localScale;
            beam.transform.localScale = new Vector3(Mathf.Abs(scale.x) * dirX, scale.y, scale.z);

            var beamSr = beam.GetComponent<SpriteRenderer>();
            if (beamSr != null && beamSr.sprite != null)
            {
                var spriteBounds = beamSr.sprite.bounds;
                beam.transform.position = new Vector3(
                    beam.transform.position.x - spriteBounds.center.x * beam.transform.localScale.x,
                    beam.transform.position.y - spriteBounds.center.y * beam.transform.localScale.y,
                    beam.transform.position.z);
            }

            // Наносим урон, если игрок в зоне поражения лазера
            if (playerStats != null && !isDead && stats != null)
            {
                float dist = Vector2.Distance(transform.position, player.position);
                if (dist <= rangedAttackRange)
                {
                    playerStats.TakeDamage(stats.Damage);
                    DamagePopup.Show(player.position, stats.Damage, Color.yellow);
                }
            }
        }
    }

    int hurtCounter;

    void HandleHealthChanged(MonsterStats s)
    {
        if (isDead || state == MonsterState.Death || state == MonsterState.Hurt) return;

        // Каждый удар — красная вспышка
        if (hurtRoutine != null) StopCoroutine(hurtRoutine);
        hurtRoutine = StartCoroutine(HurtFlash());

        // Каждый 3-й удар — анимация Take Hit (если не в атаке)
        hurtCounter++;
        if (hurtCounter >= 3)
        {
            hurtCounter = 0;
            if (state != MonsterState.Attack && state != MonsterState.RangedAttack)
            {
                if (attackRoutine != null) { StopCoroutine(attackRoutine); attackRoutine = null; }
                if (hurtRoutine != null) StopCoroutine(hurtRoutine);
                hurtRoutine = StartCoroutine(HurtRoutine());
            }
        }
    }

    IEnumerator HurtFlash()
    {
        anim.SetColor(new Color(1f, 0.3f, 0.3f, 1f));
        yield return new WaitForSeconds(0.15f);
        anim.SetColor(Color.white);
        hurtRoutine = null;
    }

    IEnumerator HurtRoutine()
    {
        SetState(MonsterState.Hurt);
        StopMoving();
        anim.SetColor(new Color(1f, 0.3f, 0.3f, 1f));
        yield return new WaitForSeconds(anim.GetClipDuration(MonsterState.Hurt));
        anim.SetColor(Color.white);
        anim.UnlockAnimation();
        hurtRoutine = null;
        if (!isDead)
            SetState(Dist() <= stats.ChaseRange ? MonsterState.Chase : MonsterState.Idle);
    }

    void HandleDeath(MonsterStats s)
    {
        if (isDead) return;
        isDead = true;
        if (hurtRoutine != null) StopCoroutine(hurtRoutine);
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        StopMoving();
        if (bodyCollider) bodyCollider.enabled = false;
        rb.simulated = false;
        state = MonsterState.Death;
        anim.PlayState(MonsterState.Death, force: true);
        StartCoroutine(DestroyAfterDeath());
    }

    IEnumerator DestroyAfterDeath()
    {
        float dur = anim.GetClipDuration(MonsterState.Death);
        if (dur <= 0f) dur = 1f; // минимум 1 сек даже если нет спрайтов
        yield return new WaitForSeconds(dur);
        Destroy(gameObject);
    }

    void MoveTowards(Vector2 t, float s)
    {
        var d = (t - (Vector2)transform.position).normalized;
        rb.linearVelocity = new Vector2(d.x * s, rb.linearVelocity.y);
        bool fr = d.x > 0.01f;
        if (fr != facingRight) { facingRight = fr; anim.SetFacing(fr); }
    }

    void StopMoving() { if (rb) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); }
    float Dist() => player ? Vector2.Distance(transform.position, player.position) : float.MaxValue;

    void SetState(MonsterState ns)
    {
        if (state == ns) return;
        state = ns;
        anim.PlayState(ns);
        if (ns != MonsterState.Chase && ns != MonsterState.Patrol) StopMoving();
    }

    void FacePlayer()
    {
        if (!player) return;
        bool fr = player.position.x > transform.position.x;
        if (fr != facingRight) { facingRight = fr; anim.SetFacing(fr); }
    }

    void OnDrawGizmosSelected()
    {
        if (!stats) stats = GetComponent<MonsterStats>();
        if (!stats) return;
        Vector2 p = Application.isPlaying ? spawnPoint : (Vector2)transform.position;
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.25f); Gizmos.DrawWireSphere(p, stats.ChaseRange);
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f); Gizmos.DrawWireSphere(p, stats.AttackRange);

        // Точка спавна лазера
        if (laserBeamPrefab != null)
        {
            if (!bodyCollider) bodyCollider = GetComponent<Collider2D>();
            Vector2 laserPos = (Vector2)transform.position;
            if (bodyCollider != null)
            {
                laserPos = new Vector2(
                    bodyCollider.bounds.center.x + laserSpawnOffset.x,
                    bodyCollider.bounds.max.y + laserSpawnOffset.y);
            }
            else
            {
                laserPos += laserSpawnOffset;
            }

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(laserPos, 15f);
            Gizmos.DrawLine(laserPos, laserPos + Vector2.right * 80f);
            Gizmos.DrawLine(laserPos, laserPos + Vector2.left * 80f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(laserPos + Vector2.up * 20f, "Laser");
#endif
        }
    }
}
