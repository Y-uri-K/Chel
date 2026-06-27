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

        var deathController = LevelDeathController.Instance;
        if (deathController != null)
            deathController.HandlePlayerDeath();
    }
}
