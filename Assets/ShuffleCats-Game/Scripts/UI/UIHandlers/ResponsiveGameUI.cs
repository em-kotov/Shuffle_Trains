using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(200)]
[RequireComponent(typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler))]
public class ResponsiveGameUI : MonoBehaviour
{
    private RectTransform _safeRoot;
    private UnityEngine.UI.CanvasScaler _scaler;
    private CameraFitter _cameraFitter;
    private Station[] _stations;
    private readonly Dictionary<string, RectTransform> _rects = new();
    private readonly Dictionary<RectTransform, RectState> _original = new();
    private int _width;
    private int _height;
    private Rect _safeArea;
    private TMP_Text _settingsTitle, _shopTitle, _shopTitleShadow;

    public static void Attach(Canvas canvas)
    {
        if (canvas != null && canvas.GetComponent<ResponsiveGameUI>() == null)
            canvas.gameObject.AddComponent<ResponsiveGameUI>();
    }

    public RectTransform SafeRoot => _safeRoot;

    public void RegisterLeaderboardButton(RectTransform button)
    {
        _rects["LeaderboardButton"] = button;
        _original[button] = new RectState(button);
        _width = _height = 0;
    }

    public void RegisterHintButton(RectTransform button)
    {
        _rects["HintButton"] = button;
        _original[button] = new RectState(button);
        // Defer fitting until camera/station Awake methods have captured their authored transforms.
        _width = _height = 0;
    }

    private void Awake()
    {
        _scaler = GetComponent<UnityEngine.UI.CanvasScaler>();

        if (Camera.main != null)
        {
            _cameraFitter = Camera.main.GetComponent<CameraFitter>();
        }
        else
        {
            _cameraFitter = null;
        }

        _stations = FindObjectsOfType<Station>(true);

        var children = new List<Transform>();

        foreach (Transform child in transform)
            children.Add(child);

        _safeRoot = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
        _safeRoot.SetParent(transform, false);
        Stretch(_safeRoot);

        foreach (Transform child in children)
            child.SetParent(_safeRoot, false);

        // Restore the authored layout first so repeated rotations do not accumulate offsets or scale.
        string[] paths = {
            "LevelCounter", "CatsCounterPanel/Counter", "WalletPanel/Total", "WalletPanel/Bonus",
            "Options", "Options/OptionsButton", "Options/OptionsButtonShadow",
            "Shop", "Shop/ShopButton", "Shop/ShopButtonShadow",
            "ShopPanel/BgImage", "ShopPanel/BgImageShadow", "ShopPanel/Title", "ShopPanel/TitleShadow",
            "ShopPanel/ShopIcon", "ShopPanel/ShopIconShadow", "ShopPanel/CloseButton", "ShopPanel/CloseButtonShadow",
            "ShopPanel/Scroll View", "ShopPanel/Scroll View/Viewport",
            "OptionsPanel/CloseButton", "OptionsPanel/CloseButtonShadow"
        };

        foreach (string path in paths)
        {
            var rect = _safeRoot.Find(path) as RectTransform;

            if (rect == null)
                continue;

            _rects.Add(path, rect);
            _original.Add(rect, new RectState(rect));
        }

        StyleHudBadge("LevelCounter", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170, 44));
        StyleHudBadge("CatsCounterPanel/Counter", new Vector2(0.5f, 1),
            new Vector2(0, -34.4f), new Vector2(218, 54));
        StyleHudBadge("WalletPanel/Total", Vector2.one, new Vector2(-95, -35), new Vector2(170, 44));
        StyleHudBadge("WalletPanel/Bonus", Vector2.one, new Vector2(-59, -75), new Vector2(96, 24), true);

        LanguageCardGraphic.WindowBackground(Get("ShopPanel/BgImage"));
        LanguageCardGraphic.WindowBackground(Get("ShopPanel/BgImageShadow"), true);

        foreach (string panel in new[] { "WinPanel", "LoosePanel", "LastLevelPanel" })
        {
            LanguageCardGraphic.WindowBackground(_safeRoot.Find(panel + "/Bg-Image") as RectTransform);
            LanguageCardGraphic.WindowBackground(_safeRoot.Find(panel + "/Bg-ImageShadow") as RectTransform, true);
        }

        _settingsTitle = _safeRoot.Find("OptionsPanel/Title")?.GetComponent<TMP_Text>();
        _shopTitle = Get("ShopPanel/Title")?.GetComponent<TMP_Text>();
        _shopTitleShadow = Get("ShopPanel/TitleShadow")?.GetComponent<TMP_Text>();

        foreach (string path in new[] { "LevelCounter", "CatsCounterPanel/Counter", "WalletPanel/Total", "WalletPanel/Bonus" })
        {
            var badge = Get(path);

            if (badge == null)
                continue;

            foreach (var graphic in badge.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                graphic.raycastTarget = false;
        }
    }

    private void StyleHudBadge(string path, Vector2 anchor, Vector2 position, Vector2 size,
        bool bonus = false)
    {
        var badge = Get(path);

        if (badge == null)
            return;

        var contour = badge.Find("BgIconContour")?.GetComponent<UnityEngine.UI.Image>();

        if (contour != null)
            contour.enabled = false;

        string backgroundName;

        if (bonus)
        {
            backgroundName = "BonusBgIconSecondary";
        }
        else
        {
            backgroundName = "BgIconPrimary";
        }

        StyleHudLayer(badge.Find(backgroundName) as RectTransform,
            anchor, position, size, false, bonus);

        if (bonus == false)
            StyleHudLayer(badge.Find("BgIconShadow") as RectTransform,
                anchor, position + new Vector2(0, -3), size, true, false);
    }

    private static void StyleHudLayer(RectTransform rect, Vector2 anchor, Vector2 position,
        Vector2 size, bool shadow, bool bonus)
    {
        if (rect == null)
            return;

        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = Vector2.one * 0.5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var card = LanguageCardGraphic.WindowBackground(rect, shadow);
        card.Radius = size.y * 0.5f;

        if (shadow)
        {
            card.BorderWidth = 0;
        }
        else if (bonus)
        {
            card.BorderWidth = 1.25f;
        }
        else
        {
            card.BorderWidth = 2;
        }

        if (bonus)
        {
            card.color = new Color32(240, 246, 229, 255);
            card.BorderColor = new Color32(163, 184, 139, 255);
        }

        card.SetVerticesDirty();
    }

    private void LateUpdate()
    {
        if (_width == Screen.width && _height == Screen.height && _safeArea == Screen.safeArea)
            return;

        RefreshLayout();
    }

    public void RefreshLayout()
    {
        _width = Screen.width;
        _height = Screen.height;
        _safeArea = Screen.safeArea;

        if (_width <= 0 || _height <= 0)
            return;

        Rect safe;

        if (_safeArea.width > 0 && _safeArea.height > 0)
        {
            safe = _safeArea;
        }
        else
        {
            safe = new Rect(0, 0, _width, _height);
        }

        bool portrait;

        if (safe.height > safe.width)
        {
            portrait = true;
        }
        else
        {
            portrait = false;
        }

        foreach (var pair in _original)
            pair.Value.Restore(pair.Key);

        _scaler.referenceResolution = new Vector2(480, 450);
        _scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;

        _safeRoot.anchorMin = new Vector2(safe.xMin / _width, safe.yMin / _height);
        _safeRoot.anchorMax = new Vector2(safe.xMax / _width, safe.yMax / _height);
        _safeRoot.offsetMin = _safeRoot.offsetMax = Vector2.zero;

        // CanvasScaler updates later; calculate the scale now to avoid one frame at the old orientation.
        float scale = Mathf.Min(_width / _scaler.referenceResolution.x,
            _height / _scaler.referenceResolution.y);
        float logicalWidth = safe.width / scale;
        float logicalHeight = safe.height / scale;
        bool stackedHeader;

        if (logicalWidth < 640)
        {
            stackedHeader = true;
        }
        else
        {
            stackedHeader = false;
        }

        float trackTop;

        if (stackedHeader)
        {
            trackTop = 68;
        }
        else
        {
            trackTop = 4;
        }

        Place("LevelCounter", new Vector2(0, 1), new Vector2(100, -38), new Vector2(100, 100));
        Place("WalletPanel/Total", new Vector2(1, 1), new Vector2(-12, -8), new Vector2(100, 100), new Vector2(1, 1));
        Place("WalletPanel/Bonus", new Vector2(1, 1), new Vector2(-12, -8), new Vector2(100, 100), new Vector2(1, 1));
        Place("CatsCounterPanel/Counter", new Vector2(0.5f, 1), new Vector2(0, -trackTop),
            new Vector2(100, 100), new Vector2(0.5f, 1));

        Stretch(Get("Options"));
        Stretch(Get("Shop"));
        Vector2 menuAnchor;

        if (portrait)
        {
            menuAnchor = Vector2.zero;
        }
        else
        {
            menuAnchor = new Vector2(0, 1);
        }

        Vector2 optionsPosition;

        if (portrait)
        {
            optionsPosition = new Vector2(20, 20);
        }
        else
        {
            optionsPosition = new Vector2(24, -90);
        }

        Vector2 shopPosition;

        if (portrait)
        {
            shopPosition = new Vector2(92, 20);
        }
        else
        {
            shopPosition = new Vector2(24, -164);
        }

        Vector2 menuSize = Vector2.one * 60;
        Place("Options/OptionsButton", menuAnchor, optionsPosition, menuSize, menuAnchor);
        Place("Options/OptionsButtonShadow", menuAnchor, optionsPosition + new Vector2(1, -2), menuSize, menuAnchor);
        Place("Shop/ShopButton", menuAnchor, shopPosition, menuSize, menuAnchor);
        Place("Shop/ShopButtonShadow", menuAnchor, shopPosition + new Vector2(1, -2), menuSize, menuAnchor);
        Vector2 hintAnchor;

        if (portrait)
        {
            hintAnchor = new Vector2(1, 0);
        }
        else
        {
            hintAnchor = new Vector2(0, 1);
        }

        Vector2 hintPosition;

        if (portrait)
        {
            hintPosition = new Vector2(-20, 20);
        }
        else
        {
            hintPosition = new Vector2(24, -238);
        }

        Place("HintButton", hintAnchor, hintPosition, menuSize, hintAnchor);
        Vector2 leaderboardAnchor;

        if (portrait)
        {
            leaderboardAnchor = Vector2.zero;
        }
        else
        {
            leaderboardAnchor = new Vector2(1, 1);
        }

        Vector2 leaderboardPosition;

        if (portrait)
        {
            leaderboardPosition = new Vector2(164, 20);
        }
        else
        {
            leaderboardPosition = new Vector2(-24, -112);
        }

        Place("LeaderboardButton", leaderboardAnchor, leaderboardPosition, menuSize, leaderboardAnchor);
        Place("OptionsPanel/CloseButton", Vector2.one * 0.5f, new Vector2(145, 177), new Vector2(50, 50));
        Place("OptionsPanel/CloseButtonShadow", Vector2.one * 0.5f, new Vector2(145, 174), new Vector2(50, 50));

        float shopWidth = Mathf.Min(500, logicalWidth - 24);
        float shopHeight;

        if (portrait)
        {
            shopHeight = Mathf.Min(600, logicalHeight - 72);
        }
        else
        {
            shopHeight = Mathf.Min(380, logicalHeight - 24);
        }

        LayoutShop(shopWidth, shopHeight, 50);

        Rect gameplay = safe;
        gameplay.yMax -= (trackTop + 76) * scale;

        if (portrait)
        {
            gameplay.yMin += 96 * scale;
        }

        foreach (Station station in _stations)
            if (station != null)
                station.SetPortraitPresentation(portrait);

        if (_cameraFitter != null)
        {
            Rect gameplayArea = new Rect(gameplay.x / _width, gameplay.y / _height,
                gameplay.width / _width, Mathf.Max(1, gameplay.height) / _height);
            float verticalAlignment;

            if (portrait)
            {
                verticalAlignment = 0.15f;
            }
            else
            {
                verticalAlignment = 0.5f;
            }

            _cameraFitter.SetGameplayArea(gameplayArea, verticalAlignment);
        }
    }

    private void LayoutShop(float width, float height, float closeSize)
    {
        height = Mathf.Max(280, height);
        Resize("ShopPanel/BgImage", new Vector2(width, height));
        Resize("ShopPanel/BgImageShadow", new Vector2(width, height));
        float top = height * 0.5f - 32;
        var titleSize = new Vector2(width - 144, 50);
        Place("ShopPanel/Title", Vector2.one * 0.5f, new Vector2(0, top), titleSize);
        Place("ShopPanel/TitleShadow", Vector2.one * 0.5f, new Vector2(0, top - 3), titleSize);
        StyleWindowTitle(_shopTitle, _settingsTitle, width);
        StyleWindowTitle(_shopTitleShadow, _settingsTitle, width, true);
        Place("ShopPanel/ShopIcon", Vector2.one * 0.5f, new Vector2(-width * 0.5f + 32, top), new Vector2(40, 40));
        Place("ShopPanel/ShopIconShadow", Vector2.one * 0.5f, new Vector2(-width * 0.5f + 32, top - 3), new Vector2(40, 40));
        Place("ShopPanel/CloseButton", Vector2.one * 0.5f, new Vector2(width * 0.5f - 36, top), Vector2.one * closeSize);
        Place("ShopPanel/CloseButtonShadow", Vector2.one * 0.5f, new Vector2(width * 0.5f - 36, top - 3), Vector2.one * closeSize);
        Place("ShopPanel/Scroll View", Vector2.one * 0.5f, new Vector2(0, -28), new Vector2(width - 36, height - 96));
        var viewport = Get("ShopPanel/Scroll View/Viewport");
        Stretch(viewport);
        var scroll = Get("ShopPanel/Scroll View")?.GetComponent<UnityEngine.UI.ScrollRect>();

        if (scroll != null)
        {
            scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.scrollSensitivity = 24;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;

            if (scroll.verticalScrollbar != null)
            {
                var bar = (RectTransform)scroll.verticalScrollbar.transform;
                bar.anchorMin = new Vector2(1, 0);
                bar.anchorMax = Vector2.one;
                bar.pivot = new Vector2(0, 0.5f);
                bar.offsetMin = new Vector2(5, 4);
                bar.offsetMax = new Vector2(11, -4);
                scroll.verticalScrollbar.interactable = true;
                var slidingArea = bar.Find("Sliding Area") as RectTransform;
                Stretch(slidingArea);
                Transform oldShadow;

                if (slidingArea != null)
                {
                    oldShadow = slidingArea.Find("HandleShadow");
                }
                else
                {
                    oldShadow = null;
                }

                if (oldShadow != null)
                    oldShadow.gameObject.SetActive(false);

                var track = bar.GetComponent<UnityEngine.UI.Image>();

                if (track != null)
                {
                    track.enabled = true;
                    track.sprite = null;
                    track.type = UnityEngine.UI.Image.Type.Simple;
                    track.color = new Color32(235, 219, 191, 255);
                }

                var handle = scroll.verticalScrollbar.handleRect;

                if (handle != null)
                {
                    handle.localScale = Vector3.one;
                    handle.sizeDelta = Vector2.zero;
                    handle.anchoredPosition = Vector2.zero;
                    var graphic = handle.GetComponent<UnityEngine.UI.Image>();

                    if (graphic != null)
                    {
                        graphic.enabled = true;
                        graphic.sprite = null;
                        graphic.type = UnityEngine.UI.Image.Type.Simple;
                        graphic.preserveAspect = false;
                        graphic.color = new Color32(190, 162, 122, 255);
                    }
                }
            }
        }
    }

    public static void StyleWindowTitle(TMP_Text title, TMP_Text settingsTitle, float windowWidth,
        bool shadow = false)
    {
        if (title == null)
            return;

        float settingsSize = 42;

        if (settingsTitle != null)
        {
            title.font = settingsTitle.font;
            // Hidden prefab text may retain a stale material after the font atlas is rebuilt.
            title.fontSharedMaterial = settingsTitle.font.material;
            title.fontStyle = settingsTitle.fontStyle;

            if (settingsTitle.enableAutoSizing)
            {
                settingsSize = settingsTitle.fontSizeMax;
            }
            else
            {
                settingsSize = settingsTitle.fontSize;
            }
        }

        if (shadow)
        {
            title.color = new Color32(72, 72, 72, 112);
        }
        else if (settingsTitle != null)
        {
            title.color = (Color32)settingsTitle.color;
        }
        else
        {
            title.color = new Color32(114, 194, 83, 255);
        }

        title.fontSizeMax = settingsSize * Mathf.Clamp(windowWidth / 360f, 0.85f, 1.1f);
        title.fontSizeMin = 18;
        title.fontSize = title.fontSizeMax;
        title.enableAutoSizing = true;
        title.enableWordWrapping = false;
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;
    }

    private RectTransform Get(string path)
    {
        if (_rects.TryGetValue(path, out var rect))
        {
            return rect;
        }
        else
        {
            return null;
        }
    }

    private void Resize(string path, Vector2 size)
    {
        var rect = Get(path);

        if (rect != null)
            rect.sizeDelta = size;
    }

    private void Place(string path, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        var rect = Get(path);

        if (rect == null)
            return;

        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot ?? Vector2.one * 0.5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one * 0.5f;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private readonly struct RectState
    {
        private readonly Vector2 _min, _max, _pivot, _position, _size;
        public RectState(RectTransform rect)
        {
            _min = rect.anchorMin; _max = rect.anchorMax; _pivot = rect.pivot;
            _position = rect.anchoredPosition; _size = rect.sizeDelta;
        }
        public void Restore(RectTransform rect)
        {
            rect.anchorMin = _min; rect.anchorMax = _max; rect.pivot = _pivot;
            rect.anchoredPosition = _position; rect.sizeDelta = _size;
        }
    }
}
