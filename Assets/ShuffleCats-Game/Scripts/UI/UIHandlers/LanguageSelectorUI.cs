using TMPro;
using UnityEngine;

public sealed class LanguageSelectorUI : MonoBehaviour
{
    private static readonly string[] Codes = { Localization.Auto, "ru", "en", "tr" };
    private static readonly string[] Names = { "AUTO", "Русский", "English", "Türkçe" };
    private static readonly Color Ink = new Color32(92, 62, 47, 255);
    private readonly LanguageCardGraphic[] _faces = new LanguageCardGraphic[4];
    private readonly GameObject[] _checks = new GameObject[4];
    private TMP_FontAsset _font;

    public static void Attach(Transform panel)
    {
        if (panel == null || panel.Find("LanguageSelector") != null)
            return;

        var root = Rect("LanguageSelector", panel, new Vector2(0, -59), new Vector2(310, 102));
        root.gameObject.AddComponent<LanguageSelectorUI>().Build(panel);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void Move(Transform panel, string name, float amount)
    {
        if (panel.Find(name) is RectTransform rect)
            rect.anchoredPosition += Vector2.up * amount;
    }

    private static void Resize(Transform panel, string name, Vector2 size)
    {
        if (panel.Find(name) is RectTransform rect)
            rect.sizeDelta = size;
    }

    private void OnEnable()
    {
        Localization.Changed += Refresh;
        Refresh();
    }

    private void OnDisable() => Localization.Changed -= Refresh;

    private void Build(Transform panel)
    {
        const int settingsContentOffset = 40;
        const int restartShadowVerticalOffset = 153;
        const int restartVerticalOffset = 150;
        const int labelFontSize = 17;
        const int languageNameVerticalOffset = 17;
        const int checkmarkSize = 17;

        _font = Resources.Load<TMP_FontAsset>("Fonts/Eno Black SDF");

        Resize(panel, "BgImage", new Vector2(360, 420));
        Resize(panel, "BgImageShadow", new Vector2(360, 420));
        LanguageCardGraphic.WindowBackground(panel.Find("BgImage") as RectTransform);
        LanguageCardGraphic.WindowBackground(panel.Find("BgImageShadow") as RectTransform, true);
        Move(panel, "Title", settingsContentOffset);
        Move(panel, "TitleShadow", settingsContentOffset);
        Move(panel, "SettingsIcon", settingsContentOffset);
        Move(panel, "SettingsIconShadow", settingsContentOffset);
        Move(panel, "MusicToggle", settingsContentOffset);
        Move(panel, "MusicSlider", settingsContentOffset);
        Move(panel, "SfxToggle", settingsContentOffset);
        Move(panel, "SoundSlider", settingsContentOffset);

        foreach (string name in new[] { "RestartButton", "RestartButtonShadow" })
        {
            var rect = panel.Find(name) as RectTransform;

            if (rect == null)
                continue;

            rect.sizeDelta = new Vector2(56, 56);
            float verticalPosition;

            if (name.EndsWith("Shadow"))
            {
                verticalPosition = -restartShadowVerticalOffset;
            }
            else
            {
                verticalPosition = -restartVerticalOffset;
            }

            rect.anchoredPosition = new Vector2(0, verticalPosition);

            foreach (TMP_Text text in rect.GetComponentsInChildren<TMP_Text>(true))
            {
                text.rectTransform.anchoredPosition = new Vector2(0, -39);
                text.rectTransform.sizeDelta = new Vector2(220, 27);
                text.fontSize = labelFontSize;
                text.margin = Vector4.zero;
                text.color = Ink;
                text.enableWordWrapping = false;
            }
        }

        var backing = Rect("Inset", transform, Vector2.zero, new Vector2(310, 102));
        Card(backing, new Color32(249, 235, 208, 255)).Radius = 12;
        var title = Label("Title", transform, "LANGUAGE", new Vector2(0, 32), new Vector2(280, 24), labelFontSize);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        LocalizationBinder.Set(title, "LANGUAGE");

        for (int i = 0; i < Codes.Length; i++)
        {
            string code = Codes[i];
            var rect = Rect(code, transform, new Vector2(-111 + (i * 74), -15), new Vector2(64, 58));
            var shadow = rect.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color32(108, 78, 48, 70);
            shadow.effectDistance = new Vector2(0, -3);
            _faces[i] = Card(rect, new Color32(255, 247, 229, 255), true);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = _faces[i];
            var colors = button.colors;
            colors.highlightedColor = new Color(1, 0.96f, 0.84f);
            colors.pressedColor = new Color(0.84f, 0.9f, 0.73f);
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                AudioManager.PlayUIClickStatic();
                Localization.Select(code);
            });

            if (code == Localization.Auto)
            {
                var auto = Label("Auto", rect, "AUTO", Vector2.zero, new Vector2(56, 34), 20);
                LocalizationBinder.Set(auto, "AUTO");
            }
            else
            {
                var flag = Rect("Flag", rect, new Vector2(0, 8), new Vector2(43.5f, 29));
                flag.gameObject.AddComponent<LanguageFlagGraphic>().Code = code;
                flag.GetComponent<LanguageFlagGraphic>().raycastTarget = false;
                Label("Name", rect, Names[i], new Vector2(0, -languageNameVerticalOffset), new Vector2(60, 18), 12);
            }

            var check = Rect("Selected", rect, new Vector2(24, 23), new Vector2(checkmarkSize, checkmarkSize));
            var checkSprite = panel.Find("MusicToggle/Background/Checkmark")?.GetComponent<UnityEngine.UI.Image>();
            var checkImage = check.gameObject.AddComponent<UnityEngine.UI.Image>();

            if (checkSprite != null)
            {
                checkImage.sprite = checkSprite.sprite;
            }
            else
            {
                checkImage.sprite = null;
            }

            checkImage.color = new Color32(76, 125, 62, 255);
            checkImage.raycastTarget = false;
            _checks[i] = check.gameObject;
        }

        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < Codes.Length; i++)
        {
            if (_faces[i] == null)
                continue;

            bool selected;

            if (Localization.Preference == Codes[i])
            {
                selected = true;
            }
            else
            {
                selected = false;
            }

            if (selected)
            {
                _faces[i].color = new Color32(214, 232, 176, 255);
            }
            else
            {
                _faces[i].color = new Color32(255, 247, 229, 255);
            }

            if (selected)
            {
                _faces[i].BorderColor = new Color32(108, 150, 81, 255);
            }
            else
            {
                _faces[i].BorderColor = new Color32(190, 162, 122, 255);
            }

            _faces[i].SetVerticesDirty();
            _checks[i].SetActive(selected);
        }
    }

    private LanguageCardGraphic Card(RectTransform rect, Color color, bool interactive = false)
    {
        var image = rect.gameObject.AddComponent<LanguageCardGraphic>();
        image.color = color;
        image.raycastTarget = interactive;
        return image;
    }

    private TMP_Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size, float fontSize)
    {
        var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.text = text;
        label.fontSize = fontSize;
        label.color = Ink;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.enableWordWrapping = false;
        return label;
    }
}
