using UnityEngine;
using UnityEngine.UI;

public class OptionsUI : UIPanel
{
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Toggle _musicToggle;
    [SerializeField] private Toggle _sfxToggle;
    [SerializeField] private UnityEngine.UI.Slider _musicSlider;
    [SerializeField] private UnityEngine.UI.Slider _sfxSlider;

    private TMPro.TMP_Text _musicVolumeLabel;
    private TMPro.TMP_Text _sfxVolumeLabel;
    private UnityEngine.UI.Toggle _hintsToggle;
    private HintLampGraphic _hintsLamp;

    public UnityEngine.UI.Button OpenButton => _optionsButton;
    public UnityEngine.UI.Button RestartButton => _restartButton;

    private void Awake()
    {
        Transform panel = _closeButton.transform.parent;
        LanguageSelectorUI.Attach(panel);
        BuildHintsControl(panel);

        if (_musicSlider == null)
            _musicSlider = panel.Find("MusicSlider")?.GetComponent<UnityEngine.UI.Slider>();

        if (_sfxSlider == null)
            _sfxSlider = panel.Find("SoundSlider")?.GetComponent<UnityEngine.UI.Slider>();

        if (_musicSlider != null)
            _musicVolumeLabel = _musicSlider.GetComponentInChildren<TMPro.TMP_Text>(true);

        if (_sfxSlider != null)
            _sfxVolumeLabel = _sfxSlider.GetComponentInChildren<TMPro.TMP_Text>(true);
    }

    private void OnEnable()
    {
        _optionsButton.onClick.AddListener(Open);
        _closeButton.onClick.AddListener(Close);
        _restartButton.onClick.AddListener(Restarter.ReloadScene);

        _optionsButton.onClick.AddListener(AudioManager.PlayUIClickStatic);
        _closeButton.onClick.AddListener(AudioManager.PlayUIClickStatic);
        _restartButton.onClick.AddListener(AudioManager.PlayUIClickStatic);

        if (_musicToggle != null)
        {
            _musicToggle.onValueChanged.AddListener(OnMusicChanged);
        }

        if (_sfxToggle != null)
        {
            _sfxToggle.onValueChanged.AddListener(OnSfxChanged);
        }

        if (_musicSlider != null)
            _musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (_sfxSlider != null)
            _sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

        if (_hintsToggle != null)
            _hintsToggle.onValueChanged.AddListener(OnHintsChanged);

        SyncControls();
        Close();
    }

    private void OnDisable()
    {
        _optionsButton.onClick.RemoveListener(Open);
        _closeButton.onClick.RemoveListener(Close);
        _restartButton.onClick.RemoveListener(Restarter.ReloadScene);

        _optionsButton.onClick.RemoveListener(AudioManager.PlayUIClickStatic);
        _closeButton.onClick.RemoveListener(AudioManager.PlayUIClickStatic);
        _restartButton.onClick.RemoveListener(AudioManager.PlayUIClickStatic);

        if (_musicToggle != null)
        {
            _musicToggle.onValueChanged.RemoveListener(OnMusicChanged);
        }

        if (_sfxToggle != null)
        {
            _sfxToggle.onValueChanged.RemoveListener(OnSfxChanged);
        }

        if (_musicSlider != null)
            _musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

        if (_sfxSlider != null)
            _sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);

        if (_hintsToggle != null)
            _hintsToggle.onValueChanged.RemoveListener(OnHintsChanged);

        // Restart unloads this handler without calling Close.
        YandexSdk.SetOptionsOpen(false);
    }

    private void Open()
    {
        _optionsButton.interactable = false;
        SyncControls();
        Show();
        YandexSdk.SetOptionsOpen(true);
    }

    private void Close()
    {
        Hide();
        _optionsButton.interactable = true;
        YandexSdk.SetOptionsOpen(false);
    }

    private void SyncControls()
    {
        if (_hintsToggle != null)
            _hintsToggle.SetIsOnWithoutNotify(HintSettings.Enabled);

        RefreshHintsLamp();
        AudioManager audio = AudioManager.Instance;

        if (audio == null)
        {
            return;
        }

        if (_musicToggle != null)
        {
            _musicToggle.SetIsOnWithoutNotify(audio.MusicOn);
        }

        if (_sfxToggle != null)
        {
            _sfxToggle.SetIsOnWithoutNotify(audio.SfxOn);
        }

        if (_musicSlider != null)
            _musicSlider.SetValueWithoutNotify(audio.MusicVolume);

        if (_sfxSlider != null)
            _sfxSlider.SetValueWithoutNotify(audio.SfxVolume);

        UpdateVolumeLabel(_musicVolumeLabel, audio.MusicVolume);
        UpdateVolumeLabel(_sfxVolumeLabel, audio.SfxVolume);
    }

    private void BuildHintsControl(Transform panel)
    {
        Toggle source;

        if (_musicToggle != null)
        {
            source = _musicToggle;
        }
        else
        {
            source = _sfxToggle;
        }

        if (source == null)
            return;

        RectTransform root = NewHintsRect("HintsToggle", panel, new Vector2(-112, -155), new Vector2(112, 86));
        var hitArea = root.gameObject.AddComponent<UnityEngine.UI.Image>();
        hitArea.color = Color.clear;
        _hintsToggle = root.gameObject.AddComponent<UnityEngine.UI.Toggle>();
        _hintsToggle.colors = source.colors;

        RectTransform lamp = NewHintsRect("Lamp", root, new Vector2(0, 5), Vector2.one * 56);
        _hintsLamp = lamp.gameObject.AddComponent<HintLampGraphic>();
        _hintsLamp.raycastTarget = false;
        RefreshHintsLamp();

        RectTransform box = NewHintsRect("Background", root, new Vector2(23, 23), Vector2.one * 18);
        var background = box.gameObject.AddComponent<UnityEngine.UI.Image>();

        if (source.targetGraphic is UnityEngine.UI.Image sourceBackground)
        {
            background.sprite = sourceBackground.sprite;
            background.type = sourceBackground.type;
            background.color = sourceBackground.color;
        }

        background.raycastTarget = false;
        RectTransform check = NewHintsRect("Checkmark", box, Vector2.zero, Vector2.one * 18);
        var checkmark = check.gameObject.AddComponent<UnityEngine.UI.Image>();

        if (source.graphic is UnityEngine.UI.Image sourceCheck)
        {
            checkmark.sprite = sourceCheck.sprite;
            checkmark.color = sourceCheck.color;
        }

        checkmark.raycastTarget = false;
        _hintsToggle.targetGraphic = background;
        _hintsToggle.graphic = checkmark;
        _hintsToggle.SetIsOnWithoutNotify(HintSettings.Enabled);

        var label = NewHintsRect("Label", root, new Vector2(0, -34), new Vector2(112, 27))
            .gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        var sourceLabel = source.GetComponentInChildren<TMPro.TMP_Text>(true);

        if (sourceLabel != null)
        {
            label.font = sourceLabel.font;
        }
        else
        {
            label.font = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Eno Black SDF");
        }

        label.fontSize = 17;
        label.color = new Color32(92, 62, 47, 255);
        label.alignment = TMPro.TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        LocalizationBinder.Set(label, "Hints");

        foreach (string name in new[] { "RestartButton", "RestartButtonShadow" })
            if (panel.Find(name) is RectTransform rect)
                rect.anchoredPosition = new Vector2(62, rect.anchoredPosition.y);
    }

    private static RectTransform NewHintsRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private void OnHintsChanged(bool enabled)
    {
        HintSettings.Enabled = enabled;
        RefreshHintsLamp();
        AudioManager.PlayUIClickStatic();
    }

    private void RefreshHintsLamp()
    {
        if (_hintsLamp != null)
            if (HintSettings.Enabled)
            {
                _hintsLamp.color = Color.white;
            }
            else
            {
                _hintsLamp.color = HintLampGraphic.DisabledTint;
            }
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.MusicVolume = value;

        UpdateVolumeLabel(_musicVolumeLabel, value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SfxVolume = value;

        UpdateVolumeLabel(_sfxVolumeLabel, value);
    }

    private static void UpdateVolumeLabel(TMPro.TMP_Text label, float value)
    {
        if (label != null)
            label.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    private void OnMusicChanged(bool value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.MusicOn = value;
        }
    }

    private void OnSfxChanged(bool value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SfxOn = value;
        }
    }
}
