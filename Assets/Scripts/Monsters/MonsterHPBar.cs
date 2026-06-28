using TMPro;
using UnityEngine;

public class MonsterHPBar : MonoBehaviour
{
    [SerializeField] MonsterStats stats;
    [SerializeField] float worldBarWidth = 250f;
    [SerializeField] float worldBarHeight = 18f;
    [SerializeField] float worldOffsetY = 200f;
    [SerializeField] float textWorldHeight = 14f;

    [Header("Order In Layer")]
    [SerializeField] bool useCustomOrderInLayer = true;
    [SerializeField] int orderInLayer = 25;

    GameObject barRoot;
    SpriteRenderer fillRenderer;
    TextMeshPro hpText;
    SpriteRenderer bgSr;

    void Start()
    {
        if (stats == null)
            stats = GetComponent<MonsterStats>();

        if (stats == null)
            return;

        CreateBar();
        Refresh(stats);
    }

    void CreateBar()
    {
        float ps = Mathf.Max(0.0001f, transform.lossyScale.x);
        float inv = 1f / ps;
        var texCenter = CreateTex(new Vector2(0.5f, 0.5f));
        var texLeft = CreateTex(new Vector2(0f, 0.5f));

        var monsterRenderer = GetComponent<SpriteRenderer>();
        int sortingLayerId = monsterRenderer != null ? monsterRenderer.sortingLayerID : 0;
        int bgOrder = ResolveBgSortingOrder(monsterRenderer);

        barRoot = new GameObject("HP_Root");
        barRoot.transform.SetParent(transform, false);
        barRoot.transform.localPosition = new Vector3(0f, worldOffsetY * inv, 0f);
        barRoot.transform.localScale = new Vector3(inv, inv, 1f);

        var bg = new GameObject("HP_BG");
        bg.transform.SetParent(barRoot.transform, false);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(worldBarWidth, worldBarHeight, 1f);
        bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = texCenter;
        bgSr.color = new Color(0.15f, 0.03f, 0.03f, 0.85f);
        ApplySorting(bgSr, sortingLayerId, bgOrder);

        var fill = new GameObject("HP_Fill");
        fill.transform.SetParent(barRoot.transform, false);
        fill.transform.localPosition = new Vector3(-worldBarWidth * 0.5f, 0f, -0.01f);
        fill.transform.localScale = new Vector3(worldBarWidth, worldBarHeight, 1f);
        fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = texLeft;
        fillRenderer.color = new Color(0.95f, 0.15f, 0.15f, 1f);
        ApplySorting(fillRenderer, sortingLayerId, bgOrder + 1);

        var textGo = new GameObject("HP_Text");
        textGo.transform.SetParent(barRoot.transform, false);
        textGo.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        textGo.transform.localScale = Vector3.one;
        hpText = textGo.AddComponent<TextMeshPro>();
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.verticalAlignment = VerticalAlignmentOptions.Middle;
        hpText.fontSize = textWorldHeight;
        hpText.enableAutoSizing = false;
        hpText.overflowMode = TextOverflowModes.Overflow;
        hpText.color = new Color(1f, 0.98f, 0.92f, 1f);
        hpText.fontStyle = FontStyles.Bold;
        hpText.rectTransform.sizeDelta = new Vector2(worldBarWidth, worldBarHeight);
        ApplySorting(hpText.renderer, sortingLayerId, bgOrder + 2);

        stats.OnHealthChanged += Refresh;
        stats.OnDeath += HandleDeath;
    }

    int ResolveBgSortingOrder(SpriteRenderer monsterRenderer)
    {
        if (useCustomOrderInLayer)
            return orderInLayer;

        return (monsterRenderer != null ? monsterRenderer.sortingOrder : 10) + 1;
    }

    void HandleDeath(MonsterStats _)
    {
        if (barRoot != null)
            barRoot.SetActive(false);
    }

    void Refresh(MonsterStats s)
    {
        if (fillRenderer == null || s == null)
            return;

        float ratio = s.MaxHealth > 0
            ? Mathf.Clamp01((float)s.CurrentHealth / s.MaxHealth)
            : 0f;

        fillRenderer.transform.localScale = new Vector3(worldBarWidth * ratio, worldBarHeight, 1f);
        fillRenderer.enabled = ratio > 0f;

        if (hpText != null)
            hpText.text = $"{s.CurrentHealth}/{s.MaxHealth}";
    }

    void OnDestroy()
    {
        if (stats != null)
        {
            stats.OnHealthChanged -= Refresh;
            stats.OnDeath -= HandleDeath;
        }
    }

    static void ApplySorting(SpriteRenderer renderer, int sortingLayerId, int sortingOrder)
    {
        if (renderer == null)
            return;

        renderer.sortingLayerID = sortingLayerId;
        renderer.sortingOrder = sortingOrder;
    }

    static void ApplySorting(Renderer renderer, int sortingLayerId, int sortingOrder)
    {
        if (renderer == null)
            return;

        renderer.sortingLayerID = sortingLayerId;
        renderer.sortingOrder = sortingOrder;
    }

    static Sprite CreateTex(Vector2 pivot)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), pivot, 1f);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        orderInLayer = Mathf.Clamp(orderInLayer, -32768, 32767);
    }
#endif
}
