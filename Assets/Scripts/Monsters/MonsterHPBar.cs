using UnityEngine;

public class MonsterHPBar : MonoBehaviour
{
    [SerializeField] MonsterStats stats;
    [SerializeField] float barWidth = 1f;
    [SerializeField] float barHeight = 0.075f;
    [SerializeField] float yOffset = 0.45f;

    SpriteRenderer bgSr;
    SpriteRenderer fillSr;
    TextMesh hpText;
    bool barCreated;

    void Awake()
    {
        if (stats == null) stats = GetComponent<MonsterStats>();
        if (stats == null) return;
        CreateBar();
    }

    void Start()
    {
        // В Start статы уже точно инициализированы
        if (stats != null)
            Refresh(stats);
    }

    void CreateBar()
    {
        var col = GetComponent<Collider2D>();
        Vector3 barWorldPos = transform.position;
        if (col != null)
            barWorldPos = new Vector3(col.bounds.center.x, col.bounds.max.y + yOffset, 0f);

        var tex = CreateTex();

        // BG
        var bg = new GameObject("HP_BG");
        bg.transform.SetParent(transform, false);
        bg.transform.position = barWorldPos;
        bg.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = tex;
        bgSr.color = new Color(0.1f, 0.02f, 0.02f, 0.85f);
        bgSr.sortingOrder = 100;

        // Fill — anchored left
        var fill = new GameObject("HP_Fill");
        fill.transform.SetParent(bg.transform, false);
        fill.transform.localPosition = new Vector3(-0.5f, 0f, -0.01f);
        fill.transform.localScale = Vector3.one;
        fillSr = fill.AddComponent<SpriteRenderer>();
        fillSr.sprite = tex;
        fillSr.color = new Color(1f, 0.1f, 0.1f, 1f);
        fillSr.sortingOrder = 101;

        // Текст
        var textObj = new GameObject("HP_Text");
        textObj.transform.SetParent(bg.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        hpText = textObj.AddComponent<TextMesh>();
        hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpText.fontSize = 24;
        hpText.anchor = TextAnchor.MiddleCenter;
        hpText.alignment = TextAlignment.Center;
        hpText.color = Color.white;
        hpText.fontStyle = FontStyle.Bold;
        hpText.characterSize = 0.008f;
        textObj.GetComponent<MeshRenderer>().sortingOrder = 102;

        stats.OnHealthChanged += Refresh;
        stats.OnDeath += _ => { bgSr.enabled = false; fillSr.enabled = false; hpText.text = ""; };
        barCreated = true;
    }

    void Refresh(MonsterStats s)
    {
        if (!fillSr || !barCreated) return;
        float ratio = Mathf.Clamp01(s.HealthRatio);
        fillSr.transform.localScale = new Vector3(ratio, 1f, 1f);
        fillSr.transform.localPosition = new Vector3(-0.5f + ratio * 0.5f, 0f, -0.01f);
        if (hpText) hpText.text = $"{s.CurrentHealth}/{s.MaxHealth}";
    }

    void OnDestroy()
    {
        if (stats) stats.OnHealthChanged -= Refresh;
    }

    static Sprite CreateTex()
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, Color.white);
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
