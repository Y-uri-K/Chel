using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuController : MonoBehaviour
{
    [Header("Optional manual UI references")]
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;
    [SerializeField] Slider resolutionSlider;
    [SerializeField] TextMeshProUGUI resolutionLabel;

    bool uiBuilt;

    void Awake()
    {
        if (masterVolumeSlider == null)
            BuildSettingsUI();

        BindSliders();
        RefreshUI();
        GameSettings.SettingsChanged += RefreshUI;
    }

    void OnDestroy()
    {
        GameSettings.SettingsChanged -= RefreshUI;
    }

    void BindSliders()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        if (resolutionSlider != null)
        {
            resolutionSlider.onValueChanged.RemoveListener(OnResolutionChanged);
            resolutionSlider.onValueChanged.AddListener(OnResolutionChanged);
        }
    }

    void RefreshUI()
    {
        SetSliderValue(masterVolumeSlider, GameSettings.MasterVolume);
        SetSliderValue(musicVolumeSlider, GameSettings.MusicVolume);
        SetSliderValue(sfxVolumeSlider, GameSettings.SfxVolume);
        SetSliderValue(resolutionSlider, GameSettings.ResolutionIndex);
        UpdateResolutionLabel(GameSettings.ResolutionIndex);
    }

    void OnMasterVolumeChanged(float value)
    {
        GameSettings.SetMasterVolume(value);
    }

    void OnMusicVolumeChanged(float value)
    {
        GameSettings.SetMusicVolume(value);
    }

    void OnSfxVolumeChanged(float value)
    {
        GameSettings.SetSfxVolume(value);
    }

    void OnResolutionChanged(float value)
    {
        int index = Mathf.RoundToInt(value);
        GameSettings.SetResolutionIndex(index);
        UpdateResolutionLabel(index);
    }

    void UpdateResolutionLabel(float value)
    {
        if (resolutionLabel != null)
            resolutionLabel.text = GameSettings.GetResolutionLabel(Mathf.RoundToInt(value));
    }

    static void SetSliderValue(Slider slider, float value)
    {
        if (slider == null)
            return;

        slider.SetValueWithoutNotify(value);
    }

    void BuildSettingsUI()
    {
        if (uiBuilt)
            return;

        uiBuilt = true;

        var content = CreateRect("SettingsContent", transform, new Vector2(900f, 620f), Vector2.zero);
        CreateTitle(content, "Настройки", new Vector2(0f, 240f));

        masterVolumeSlider = CreateSliderRow(content, "Общая громкость", new Vector2(0f, 130f), out _);
        musicVolumeSlider = CreateSliderRow(content, "Музыка", new Vector2(0f, 40f), out _);
        sfxVolumeSlider = CreateSliderRow(content, "Эффекты", new Vector2(0f, -50f), out _);
        resolutionSlider = CreateSliderRow(content, "Разрешение", new Vector2(0f, -140f), out resolutionLabel);

        resolutionSlider.minValue = 0f;
        resolutionSlider.maxValue = GameSettings.Resolutions.Length - 1;
        resolutionSlider.wholeNumbers = true;
    }

    static void CreateTitle(RectTransform parent, string title, Vector2 position)
    {
        var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(parent, false);

        var rect = titleObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(700f, 80f);
        rect.anchoredPosition = position;

        var text = titleObject.GetComponent<TextMeshProUGUI>();
        text.text = title;
        text.fontSize = 64;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
    }

    Slider CreateSliderRow(RectTransform parent, string label, Vector2 position, out TextMeshProUGUI valueLabel)
    {
        const float labelWidth = 260f;
        const float labelSliderGap = 32f;
        const float sliderRightPadding = 24f;
        const float valueLabelWidth = 190f;
        const float valueLabelGap = 28f;
        bool showValueLabel = label == "Разрешение";

        var row = CreateRect(label + "Row", parent, new Vector2(820f, 70f), position);

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(row, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, 0f);
        labelRect.sizeDelta = new Vector2(labelWidth, 60f);

        var labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 34;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.overflowMode = TextOverflowModes.Ellipsis;
        labelText.color = Color.white;

        var sliderObject = DefaultControls.CreateSlider(GetUiResources());
        sliderObject.name = label + "Slider";
        sliderObject.transform.SetParent(row, false);

        float sliderLeft = labelWidth + labelSliderGap;
        float sliderRight = showValueLabel
            ? valueLabelWidth + valueLabelGap + sliderRightPadding
            : sliderRightPadding;

        var sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.offsetMin = new Vector2(sliderLeft, -18f);
        sliderRect.offsetMax = new Vector2(-sliderRight, 18f);

        var slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;

        valueLabel = null;
        if (showValueLabel)
        {
            var valueObject = new GameObject("ValueLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            valueObject.transform.SetParent(row, false);

            var valueRect = valueObject.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(1f, 0.5f);
            valueRect.anchorMax = new Vector2(1f, 0.5f);
            valueRect.pivot = new Vector2(1f, 0.5f);
            valueRect.anchoredPosition = new Vector2(-sliderRightPadding, 0f);
            valueRect.sizeDelta = new Vector2(valueLabelWidth, 60f);

            valueLabel = valueObject.GetComponent<TextMeshProUGUI>();
            valueLabel.fontSize = 28;
            valueLabel.alignment = TextAlignmentOptions.MidlineRight;
            valueLabel.overflowMode = TextOverflowModes.Ellipsis;
            valueLabel.color = Color.white;
        }

        return slider;
    }

    static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);

        var rect = rectObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    static DefaultControls.Resources GetUiResources()
    {
        return new DefaultControls.Resources
        {
            standard = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd"),
            background = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd"),
            knob = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd"),
        };
    }
}
