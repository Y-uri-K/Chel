using TMPro;
using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] CharacterStats stats;
    [SerializeField] Transform fillTransform;
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] float barWidth = 3.19f;

    Vector3 initialScale;
    Vector3 initialPosition;

    void Awake()
    {
        if (stats == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                stats = player.GetComponent<CharacterStats>();
        }

        if (fillTransform == null)
        {
            var fill = transform.Find("UI_StatusBar_Fill_HP");
            if (fill != null)
                fillTransform = fill;
        }

        if (hpText == null)
        {
            var textObject = GameObject.Find("HP_ValueText");
            if (textObject != null)
                hpText = textObject.GetComponent<TextMeshProUGUI>();
        }

        if (fillTransform != null)
        {
            initialScale = fillTransform.localScale;
            initialPosition = fillTransform.localPosition;
        }
    }

    void OnEnable()
    {
        if (stats != null)
        {
            stats.OnHealthChanged += HandleHealthChanged;
            Refresh(stats);
        }
    }

    void OnDisable()
    {
        if (stats != null)
            stats.OnHealthChanged -= HandleHealthChanged;
    }

    void Start()
    {
        if (stats != null)
            Refresh(stats);
    }

    void HandleHealthChanged(CharacterStats changedStats)
    {
        Refresh(changedStats);
    }

    void Refresh(CharacterStats changedStats)
    {
        UpdateFill(changedStats.HealthRatio);

        if (hpText != null)
            hpText.text = $"{changedStats.CurrentHealth}/{changedStats.MaxHealth}";
    }

    void UpdateFill(float ratio)
    {
        if (fillTransform == null)
            return;

        ratio = Mathf.Clamp01(ratio);
        float fullWidth = barWidth * initialScale.x;
        float scaledWidth = fullWidth * ratio;
        float leftEdgeX = initialPosition.x - fullWidth * 0.5f;

        fillTransform.localScale = new Vector3(initialScale.x * ratio, initialScale.y, initialScale.z);
        fillTransform.localPosition = new Vector3(
            leftEdgeX + scaledWidth * 0.5f,
            initialPosition.y,
            initialPosition.z);
    }
}
