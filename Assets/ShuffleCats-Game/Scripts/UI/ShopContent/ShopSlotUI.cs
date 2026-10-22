using TMPro;
using UnityEngine;

public class ShopSlotUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image _icon;
    [SerializeField] private TextMeshProUGUI _price;
    [SerializeField] private TextMeshProUGUI _status;
    [SerializeField] private UnityEngine.UI.Button _actionButton;

    private static readonly Color Ink = new Color32(92, 62, 47, 255);
    private static readonly Color Cream = new Color32(255, 247, 229, 255);
    private static readonly Color Border = new Color32(190, 162, 122, 255);
    private static readonly Color Selected = new Color32(214, 232, 176, 255);
    private static readonly Color SelectedBorder = new Color32(108, 150, 81, 255);

    private Theme _theme;
    private Shop _shop;
    private LanguageCardGraphic _card, _buttonFace;
    private TMP_Text _name;
    private UnityEngine.UI.Image _board, _stationA, _stationB;
    private GameObject _check;
    private RectTransform _priceRow;

    public void Bind(Theme theme, Shop shop)
    {
        _theme = theme;
        _shop = shop;

        if (_card == null)
            BuildCard();

        LocalizationBinder.Set(_name, theme.DisplayName);

        if (theme.BackgroundSprite != null)
        {
            _icon.sprite = theme.BackgroundSprite;
        }
        else
        {
            _icon.sprite = theme.SlotIcon;
        }

        _board.sprite = theme.PlayAreaSprite;

        if (_board.sprite != null)
        {
            _board.enabled = true;
        }
        else
        {
            _board.enabled = false;
        }

        _stationA.sprite = theme.StationSpriteA;
        _stationB.sprite = theme.StationSpriteB;

        if (_stationA.sprite != null)
        {
            _stationA.enabled = true;
        }
        else
        {
            _stationA.enabled = false;
        }

        if (_stationB.sprite != null)
        {
            _stationB.enabled = true;
        }
        else
        {
            _stationB.enabled = false;
        }

        _actionButton.onClick.RemoveAllListeners();
        _actionButton.onClick.AddListener(OnClick);
        _actionButton.onClick.AddListener(AudioManager.PlayUIClickStatic);

        Refresh();
    }

    public void Refresh()
    {
        if (_theme == null || _shop == null)
        {
            return;
        }

        SlotStatus status = _shop.GetStatus(_theme);
        bool equipped;

        if (status == SlotStatus.Equipped)
        {
            equipped = true;
        }
        else
        {
            equipped = false;
        }

        bool locked;

        if (status == SlotStatus.Locked)
        {
            locked = true;
        }
        else
        {
            locked = false;
        }

        if (equipped)
        {
            _card.color = Selected;
        }
        else
        {
            _card.color = Cream;
        }

        if (equipped)
        {
            _card.BorderColor = SelectedBorder;
        }
        else
        {
            _card.BorderColor = Border;
        }

        _card.SetVerticesDirty();

        if (equipped)
        {
            _buttonFace.color = Selected;
        }
        else
        {
            _buttonFace.color = Cream;
        }

        if (equipped)
        {
            _buttonFace.BorderColor = SelectedBorder;
        }
        else
        {
            _buttonFace.BorderColor = Border;
        }

        _buttonFace.SetVerticesDirty();
        _check.SetActive(equipped);
        _priceRow.gameObject.SetActive(locked);
        _status.color = Ink;
        float statusBottom;

        if (locked)
        {
            statusBottom = 22;
        }
        else
        {
            statusBottom = 0;
        }

        float statusHeight;

        if (locked)
        {
            statusHeight = 20;
        }
        else
        {
            statusHeight = 44;
        }

        float statusLeft;

        if (equipped)
        {
            statusLeft = 25;
        }
        else
        {
            statusLeft = 4;
        }

        Band(_status.rectTransform, statusBottom, statusHeight, statusLeft, 4);

        switch (status)
        {
            case SlotStatus.Locked:
                LocalizationBinder.Set(_status, "Buy");
                _price.text = _theme.Price.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
                _actionButton.interactable = true;
                break;
            case SlotStatus.Owned:
                LocalizationBinder.Set(_status, "Equip");
                _price.text = string.Empty;
                _actionButton.interactable = true;
                break;
            case SlotStatus.Equipped:
                LocalizationBinder.Set(_status, "Equipped");
                _price.text = string.Empty;
                _actionButton.interactable = false;
                break;
        }
    }

    private void BuildCard()
    {
        var oldBackground = GetComponent<UnityEngine.UI.Image>();

        if (oldBackground != null)
            oldBackground.enabled = false;

        RectTransform face = Rect("CardFace", transform);
        Fill(face);
        face.SetAsFirstSibling();
        _card = Card(face, true);
        _card.Radius = 10;
        Shadow(face);

        _name = Rect("ThemeName", transform).gameObject.AddComponent<TextMeshProUGUI>();
        StyleText(_name, 19);
        _name.rectTransform.anchorMin = new Vector2(0, 1);
        _name.rectTransform.anchorMax = Vector2.one;
        _name.rectTransform.pivot = new Vector2(0.5f, 1);
        _name.rectTransform.anchoredPosition = new Vector2(0, -8);
        _name.rectTransform.sizeDelta = new Vector2(-16, 28);

        RectTransform preview = Rect("Preview", transform);
        preview.anchorMin = new Vector2(0, 1);
        preview.anchorMax = Vector2.one;
        preview.pivot = new Vector2(0.5f, 1);
        preview.anchoredPosition = new Vector2(0, -42);
        preview.sizeDelta = new Vector2(-20, 98);
        Card(preview, false);
        RectTransform crop = Rect("Crop", preview);
        Fill(crop, 2);
        var maskFace = Card(crop, false);
        maskFace.BorderWidth = 0;
        maskFace.Radius = 6;
        crop.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
        _icon.rectTransform.SetParent(crop, false);
        Fill(_icon.rectTransform);
        _icon.type = UnityEngine.UI.Image.Type.Simple;
        _icon.color = Color.white;
        _icon.preserveAspect = false;
        _icon.raycastTarget = false;
        _board = PreviewImage("Board", crop, Vector2.zero, new Vector2(80, 80));
        _stationA = PreviewImage("StationA", crop, new Vector2(-42, -24), new Vector2(28, 28));
        _stationB = PreviewImage("StationB", crop, new Vector2(42, 24), new Vector2(28, 28));

        var buttonRect = (RectTransform)_actionButton.transform;
        Band(buttonRect, 10, 44, 10, 10);
        var oldFace = _actionButton.GetComponent<UnityEngine.UI.Image>();

        if (oldFace != null)
            oldFace.enabled = false;

        foreach (TMP_Text unused in _actionButton.GetComponentsInChildren<TMP_Text>(true))
            unused.gameObject.SetActive(false);

        RectTransform buttonFace = Rect("Face", buttonRect);
        Fill(buttonFace);
        buttonFace.SetAsFirstSibling();
        _buttonFace = Card(buttonFace, true);
        Shadow(buttonFace);
        _actionButton.targetGraphic = _buttonFace;
        var colors = _actionButton.colors;
        colors.normalColor = colors.selectedColor = colors.disabledColor = Color.white;
        colors.highlightedColor = new Color(1, 0.96f, 0.84f);
        colors.pressedColor = new Color(0.84f, 0.9f, 0.73f);
        _actionButton.colors = colors;
        _status.rectTransform.SetParent(buttonRect, false);
        StyleText(_status, 17);
        _priceRow = Rect("PriceRow", buttonRect);
        Band(_priceRow, 2, 22, 4, 4);
        var priceLayout = _priceRow.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        priceLayout.childAlignment = TextAnchor.MiddleCenter;
        priceLayout.spacing = 4;
        priceLayout.childControlWidth = true;
        priceLayout.childControlHeight = false;
        priceLayout.childForceExpandWidth = priceLayout.childForceExpandHeight = false;
        _price.rectTransform.SetParent(_priceRow, false);
        StyleText(_price, 15);
        _price.rectTransform.sizeDelta = new Vector2(0, 22);
        var coin = _price.transform.Find("CoinIcon") as RectTransform;

        if (coin != null)
        {
            coin.SetParent(_priceRow, false);
            coin.SetAsFirstSibling();
            coin.pivot = Vector2.one * 0.5f;
            coin.localRotation = Quaternion.identity;
            coin.localScale = Vector3.one;
            coin.sizeDelta = Vector2.one * 14;
            var coinLayout = coin.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            coinLayout.minWidth = coinLayout.preferredWidth = 14;
            coinLayout.flexibleWidth = 0;
            var coinImage = coin.GetComponent<UnityEngine.UI.Image>();
            coinImage.preserveAspect = true;
            coinImage.raycastTarget = false;
        }

        var coinShadow = _price.transform.Find("CoinIconShadow");

        if (coinShadow != null)
            coinShadow.gameObject.SetActive(false);

        var check = PreviewImage("Selected", buttonRect, Vector2.zero, Vector2.one * 18);
        check.rectTransform.anchorMin = check.rectTransform.anchorMax = new Vector2(0, 0.5f);
        check.rectTransform.anchoredPosition = new Vector2(16, 0);
        var canvas = GetComponentInParent<Canvas>();
        ResponsiveGameUI responsive;

        if (canvas != null)
        {
            responsive = canvas.GetComponent<ResponsiveGameUI>();
        }
        else
        {
            responsive = null;
        }

        Transform root;

        if (responsive != null)
        {
            root = responsive.SafeRoot;
        }
        else if (canvas != null)
        {
            root = canvas.transform;
        }
        else
        {
            root = null;
        }

        Transform source;

        if (root != null)
        {
            source = root.Find("OptionsPanel/MusicToggle/Background/Checkmark");
        }
        else
        {
            source = null;
        }

        if (source != null)
        {
            check.sprite = source.GetComponent<UnityEngine.UI.Image>()?.sprite;
        }
        else
        {
            check.sprite = null;
        }

        check.color = new Color32(76, 125, 62, 255);
        _check = check.gameObject;
        buttonRect.SetAsLastSibling();
    }

    private static void StyleText(TMP_Text text, float size)
    {
        var font = Resources.Load<TMP_FontAsset>("Fonts/Eno Black SDF");

        if (font != null)
        {
            text.font = font;
            text.fontSharedMaterial = font.material;
        }

        text.fontStyle = FontStyles.Normal;
        text.fontSize = text.fontSizeMax = size;
        text.fontSizeMin = 12;
        text.enableAutoSizing = true;
        text.enableWordWrapping = false;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Ink;
        text.margin = Vector4.zero;
        text.raycastTarget = false;
    }

    private static LanguageCardGraphic Card(RectTransform rect, bool interactive)
    {
        var card = rect.gameObject.AddComponent<LanguageCardGraphic>();
        card.color = Cream;
        card.BorderColor = Border;
        card.raycastTarget = interactive;
        return card;
    }

    private static void Shadow(RectTransform rect)
    {
        var shadow = rect.gameObject.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = new Color32(108, 78, 48, 70);
        shadow.effectDistance = new Vector2(0, -3);
    }

    private static UnityEngine.UI.Image PreviewImage(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = Rect(name, parent);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        return rect;
    }

    private static void Fill(RectTransform rect, float inset = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = Vector2.one * -inset;
    }

    private static void Band(RectTransform rect, float bottom, float height, float left, float right)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.right;
        rect.pivot = new Vector2(0.5f, 0);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, bottom + height);
    }

    private void OnClick()
    {
        if (_theme == null || _shop == null)
        {
            return;
        }

        SlotStatus status = _shop.GetStatus(_theme);

        if (status == SlotStatus.Locked)
        {
            _shop.TryBuy(_theme);
        }
        else if (status == SlotStatus.Owned)
        {
            _shop.Equip(_theme);
        }
    }
}
