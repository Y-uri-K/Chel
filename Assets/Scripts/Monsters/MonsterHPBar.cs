using UnityEngine;

public class MonsterHPBar : MonoBehaviour
{
    [SerializeField] MonsterStats stats;
    [SerializeField] float worldBarWidth = 250f;
    [SerializeField] float worldBarHeight = 18f;
    [SerializeField] float worldOffsetY = 200f;

    SpriteRenderer fillRenderer;
    TextMesh hpText;
    SpriteRenderer bgSr;

    void Awake()
    {
        if (stats == null) stats = GetComponent<MonsterStats>();
        if (stats == null) return;
        CreateBar();
    }

    void CreateBar()
    {
        float ps = transform.lossyScale.x;
        float inv = 1f / ps;
        var tex = CreateTex();

        var root = new GameObject("HP_Root");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, worldOffsetY * inv, 0f);
        root.transform.localScale = new Vector3(inv, inv, 1f);

        var bg = new GameObject("HP_BG");
        bg.transform.SetParent(root.transform, false);
        bg.transform.localPosition = new Vector3(-worldBarWidth * 0.5f, 0f, 0f);
        bg.transform.localScale = new Vector3(worldBarWidth, worldBarHeight, 1f);
        bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = tex;
        bgSr.color = new Color(0.15f, 0.03f, 0.03f, 0.8f);
        bgSr.sortingOrder = 25;

        var fill = new GameObject("HP_Fill");
        fill.transform.SetParent(bg.transform, false);
        fill.transform.localPosition = new Vector3(0.5f, 0f, -0.01f);
        fill.transform.localScale = Vector3.one;
        fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = tex;
        fillRenderer.color = new Color(1f, 0.08f, 0.08f, 1f);
        fillRenderer.sortingOrder = 26;

        var textGo = new GameObject("HP_Text");
        textGo.transform.SetParent(root.transform, false);
        textGo.transform.localPosition = new Vector3(0f, worldBarHeight * 0.5f + 40f, -0.02f);
        textGo.transform.localScale = Vector3.one * 160f;
        hpText = textGo.AddComponent<TextMesh>();
        hpText.fontSize = 42;
        hpText.anchor = TextAnchor.MiddleCenter;
        hpText.color = Color.white;
        hpText.fontStyle = FontStyle.Bold;
        hpText.characterSize = 0.04f;
        textGo.GetComponent<MeshRenderer>().sortingOrder = 27;

        stats.OnHealthChanged += Refresh;
        stats.OnDeath += _ => { bgSr.enabled = false; fillRenderer.enabled = false; hpText.text = ""; };
        Refresh(stats);
    }

    void Refresh(MonsterStats s)
    {
        if (!fillRenderer) return;
        float r = Mathf.Clamp01(s.HealthRatio);
        fillRenderer.transform.localScale = new Vector3(r, 1f, 1f);
        fillRenderer.enabled = r > 0f;
        if (hpText) hpText.text = $"{s.CurrentHealth}/{s.MaxHealth}";
    }

    void OnDestroy() { if (stats) stats.OnHealthChanged -= Refresh; }

    static Sprite CreateTex()
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, Color.white);
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
