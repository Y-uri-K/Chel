using TMPro;
using UnityEngine;

public class DiamondsDisplay : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI diamondsText;

    void Awake()
    {
        if (diamondsText == null)
            diamondsText = FindDiamondsText();
    }

    void OnEnable()
    {
        PlayerProgress.DiamondsChanged += UpdateDisplay;
        UpdateDisplay(PlayerProgress.Diamonds);
    }

    void OnDisable()
    {
        PlayerProgress.DiamondsChanged -= UpdateDisplay;
    }

    static TextMeshProUGUI FindDiamondsText()
    {
        var allTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (var text in allTexts)
        {
            if (text.name != "DiamondsText" || text.hideFlags != HideFlags.None)
                continue;

            if (text.gameObject.scene.IsValid())
                return text;
        }

        return null;
    }

    void UpdateDisplay(int amount)
    {
        if (diamondsText != null)
            diamondsText.text = amount.ToString();
    }
}
