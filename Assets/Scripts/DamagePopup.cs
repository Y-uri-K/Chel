using System.Collections;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    static TMP_FontAsset _font;

    public static void Show(Vector3 worldPos, int damage, Color color)
    {
        if (_font == null)
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (_font == null)
                _font = TMP_Settings.defaultFontAsset;
            if (_font == null)
            {
                Debug.LogError("[DamagePopup] TMP font not found!");
                return;
            }
        }

        var go = new GameObject("Dmg" + damage);
        go.transform.position = worldPos + (Vector3)(Random.insideUnitCircle * 0.5f);
        go.transform.localScale = Vector3.one * 45f;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = "-" + damage;
        tmp.fontSize = 8;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.font = _font;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = color.grayscale > 0.85f ? Color.black : Color.white;

        // Рендеринг без пиксельности
        var mr = go.GetComponent<MeshRenderer>();
        mr.sortingOrder = 100;

        var popup = go.AddComponent<DamagePopup>();
        popup.StartCoroutine(popup.Animate(tmp));
    }

    IEnumerator Animate(TextMeshPro tmp)
    {
        float start = Time.time;
        float dur = 1.2f;
        Color c = tmp.color;

        while (Time.time - start < dur)
        {
            float t = (Time.time - start) / dur;
            transform.position += Vector3.up * 3f * Time.deltaTime;
            float a = t < 0.2f ? 1f : 1f - (t - 0.2f) / 0.8f;
            tmp.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
            yield return null;
        }
        Destroy(gameObject);
    }
}
