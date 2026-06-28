using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DieZone : MonoBehaviour
{
    void Awake()
    {
        var collider = GetComponent<Collider2D>();
        collider.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        var stats = other.GetComponentInParent<CharacterStats>();
        if (stats == null || stats.CurrentHealth <= 0)
            return;

        stats.TakeDamage(stats.CurrentHealth);
    }
}
