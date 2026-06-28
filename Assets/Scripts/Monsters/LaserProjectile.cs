using UnityEngine;

/// <summary>
/// Лазерный снаряд Stone Golem'а. Летит в заданном направлении, наносит урон при столкновении.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class LaserProjectile : MonoBehaviour
{
    public float speed = 350f;
    [SerializeField] int damage = 20;
    [SerializeField] float lifetime = 3f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    float timer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    /// <summary>
    /// Запустить лазер в направлении к цели.
    /// </summary>
    public void Fire(Vector2 direction)
    {
        rb.linearVelocity = direction.normalized * speed;

        // Поворот спрайта по направлению
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var stats = other.GetComponent<CharacterStats>();
            if (stats != null)
                stats.TakeDamage(damage);

            Destroy(gameObject);
        }
    }
}
