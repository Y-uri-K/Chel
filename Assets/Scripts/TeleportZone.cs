using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TeleportZone : MonoBehaviour
{
    [SerializeField] string targetSceneName = "level 3 boss";

    bool isLoading;

    void Awake()
    {
        var collider = GetComponent<Collider2D>();
        collider.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isLoading || !other.CompareTag("Player"))
            return;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("[TeleportZone] Целевая сцена не задана.", this);
            return;
        }

        isLoading = true;
        SceneNav.LoadLevel(targetSceneName);
    }
}
