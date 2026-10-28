using System;
using TMPro;
using UnityEngine;

public sealed class LeaderboardUI : UIPanel
{
    private static readonly Color Ink = new Color32(92, 62, 47, 255);
    private RectTransform _root;
    private RectTransform _panel;
    private RectTransform _card;
    private RectTransform _content;
    private RectTransform _refreshIcon;
    private UnityEngine.UI.Button _open;
    private UnityEngine.UI.Button _signIn;
    private UnityEngine.UI.Button _refresh;
    private UnityEngine.UI.ScrollRect _scroll;
    private TMP_Text _status;
    private TMP_Text _summary;
    private TMP_Text _refreshText;
    private TMP_Text _title;
    private TMP_Text _titleShadow;
    private TMP_Text _settingsTitle;
    private TMP_FontAsset _font;
    private Sprite _panelSprite;
    private int _requestVersion;
    private bool _busy;
    private bool _isOpen;
    private float _refreshAt;
    private YandexLeaderboard.Result _lastResult;
    private bool _authorizing;

    private void OnDestroy()
    {
        Localization.Changed -= RefreshLanguage;
        _requestVersion++;

        // The child CanvasGroup may already be destroyed during scene teardown.
        if (_isOpen)
            YandexSdk.SetLeaderboardOpen(false);
    }

    private void Update()
    {
        const int maximumCardWidth = 440;
        const int maximumCardHeight = 560;
        const int viewportPadding = 24;
        const int refreshRotationDegreesPerSecond = 180;

        if (IsVisible == false)
            return;

        var size = new Vector2(
            Mathf.Min(maximumCardWidth, _root.rect.width - viewportPadding),
            Mathf.Min(maximumCardHeight, _root.rect.height - viewportPadding));

        if (_card.sizeDelta != size)
        {
            _card.sizeDelta = size;
            ResponsiveGameUI.StyleWindowTitle(_title, _settingsTitle, size.x);
            ResponsiveGameUI.StyleWindowTitle(_titleShadow, _settingsTitle, size.x, true);
        }

        int seconds = Mathf.CeilToInt(_refreshAt - Time.realtimeSinceStartup);
        bool countingDown;

        if (_busy == false && seconds > 0)
        {
            countingDown = true;
        }
        else
        {
            countingDown = false;
        }

        if (_busy == false && countingDown == false)
        {
            _refresh.interactable = true;
        }
        else
        {
            _refresh.interactable = false;
        }

        if (countingDown)
        {
            _refreshText.text = seconds.ToString();
        }
        else
        {
            _refreshText.text = Localization.Get("Refresh");
        }

        // Gameplay is paused while the leaderboard is open; use unscaled time.
        if (countingDown)
            _refreshIcon.Rotate(0, 0, -refreshRotationDegreesPerSecond * Time.unscaledDeltaTime);
        else
            _refreshIcon.localRotation = Quaternion.identity;

        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public static void Attach(Canvas canvas)
    {
        if (canvas != null && canvas.GetComponent<LeaderboardUI>() == null)
            canvas.gameObject.AddComponent<LeaderboardUI>().Build(canvas);
    }

    public void Open()
    {
        if (IsVisible)
            return;

        foreach (UIPanel panel in FindObjectsOfType<UIPanel>(true))
        {
            if (panel != this && panel.BlocksGameplay)
                return;
        }

        AudioManager.PlayUIClickStatic();
        _panel.SetAsLastSibling();
        Show();
        _isOpen = true;
        _open.interactable = false;
        YandexSdk.SetLeaderboardOpen(true);
        Refresh();
    }

    public void Close()
    {
        if (IsVisible == false)
            return;

        AudioManager.PlayUIClickStatic();
        _requestVersion++;
        Hide();
        _refreshIcon.localRotation = Quaternion.identity;
        _isOpen = false;
        _open.interactable = true;
        YandexSdk.SetLeaderboardOpen(false);
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void TopBand(RectTransform rect, float left, float right, float bottom, float height)
    {
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, bottom + height);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void Build(Canvas canvas)
    {
        var responsive = canvas.GetComponent<ResponsiveGameUI>();

        if (responsive != null)
        {
            _root = responsive.SafeRoot;
        }
        else
        {
            _root = canvas.transform as RectTransform;
        }

        _font = Resources.Load<TMP_FontAsset>("Fonts/Eno Black SDF");
        var source = _root.Find("ShopPanel/BgImage")?.GetComponent<UnityEngine.UI.Image>();

        if (source != null)
            _panelSprite = source.sprite;

        _settingsTitle = _root.Find("OptionsPanel/Title")?.GetComponent<TMP_Text>();

        RectTransform cup = Rect("LeaderboardButton", _root);
        cup.SetAsFirstSibling();
        cup.sizeDelta = Vector2.one * 60;
        var cupGraphic = cup.gameObject.AddComponent<CupGraphic>();
        _open = cup.gameObject.AddComponent<UnityEngine.UI.Button>();
        _open.targetGraphic = cupGraphic;
        _open.onClick.AddListener(Open);

        if (responsive != null)
            responsive.RegisterLeaderboardButton(cup);

        _panel = Rect("LeaderboardPanel", _root);
        Stretch(_panel);
        var dimmer = _panel.gameObject.AddComponent<UnityEngine.UI.Image>();
        dimmer.color = new Color(0, 0, 0, 0.6f);
        BindPanel(_panel.gameObject.AddComponent<CanvasGroup>());

        _card = Rect("Card", _panel);
        _card.anchorMin = _card.anchorMax = Vector2.one * 0.5f;
        var background = LanguageCardGraphic.WindowBackground(_card);
        var shadow = background.gameObject.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = new Color32(72, 52, 36, 75);
        shadow.effectDistance = new Vector2(0, -5);

        RectTransform headerCup = Rect("Cup", _card);
        Place(headerCup, new Vector2(0, 1), new Vector2(36, -34), new Vector2(42, 42));
        headerCup.gameObject.AddComponent<CupGraphic>().raycastTarget = false;
        _titleShadow = Label("TitleShadow", _card, "LEADERBOARD", 42);
        TopBand(_titleShadow.rectTransform, 64, -58, -62, 50);
        _title = Label("Title", _card, "LEADERBOARD", 42);
        TopBand(_title.rectTransform, 64, -58, -59, 50);
        BuildCloseButton();
        TMP_Text metric = Label("Metric", _card, "Highest completed level", 14);
        TopBand(metric.rectTransform, 16, -16, -78, 26);

        var columns = Rect("Columns", _card);
        TopBand(columns, 16, -16, -101, 23);
        Columns(columns, "#", "Player", "Level", 13, true);

        RectTransform scrollRoot = Rect("Rankings", _card);
        Stretch(scrollRoot);
        scrollRoot.offsetMin = new Vector2(16, 128);
        scrollRoot.offsetMax = new Vector2(-16, -108);
        _scroll = scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        _scroll.horizontal = false;
        _scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 24;
        var viewport = Rect("Viewport", scrollRoot);
        Stretch(viewport);
        Image(viewport, Color.white, false);
        viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
        _content = Rect("Content", viewport);
        _content.anchorMin = new Vector2(0, 1);
        _content.anchorMax = Vector2.one;
        _content.pivot = new Vector2(0.5f, 1);
        _content.sizeDelta = Vector2.zero;
        var layout = _content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.spacing = 5;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fit = _content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fit.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        _scroll.viewport = viewport;
        _scroll.content = _content;

        _status = Label("Status", scrollRoot, "Loading rankings...", 18);
        Stretch(_status.rectTransform);
        _status.margin = new Vector4(12, 8, 12, 8);
        _summary = Label("Summary", _card, string.Empty, 14);
        _summary.rectTransform.anchorMin = new Vector2(0, 0);
        _summary.rectTransform.anchorMax = new Vector2(1, 0);
        _summary.rectTransform.anchoredPosition = new Vector2(0, 88);
        _summary.rectTransform.sizeDelta = new Vector2(-36, 64);

        _signIn = Button(
            "SignIn",
            _card,
            "Sign in with Yandex",
            Vector2.zero,
            new Vector2(132, 34),
            new Vector2(228, 42),
            SignIn);
        _refresh = Button(
            "Refresh",
            _card,
            "Refresh",
            new Vector2(1, 0),
            new Vector2(-72, 34),
            new Vector2(108, 42),
            Refresh,
            languageStyle: true);
        _refreshText = _refresh.GetComponentInChildren<TMP_Text>();
        BuildRefreshIcon();
        Localization.Changed += RefreshLanguage;
        Hide();
    }

    private void Refresh() => Request(false);

    private void SignIn() => Request(true);

    private void Request(bool authorize)
    {
        _authorizing = authorize;
        _lastResult = null;
        int version = ++_requestVersion;
        _busy = true;
        _signIn.gameObject.SetActive(false);
        _refresh.interactable = false;
        ClearRows();
        _status.gameObject.SetActive(true);
        string statusKey;

        if (authorize)
        {
            statusKey = "Waiting for sign-in...";
        }
        else
        {
            statusKey = "Loading rankings...";
        }

        _status.text = Localization.Get(statusKey);
        _summary.text = Localization.Format("Your best: {0}", LevelProgress.BestCompletedLevel);
        Action<YandexLeaderboard.Result> complete = result =>
        {
            if (this == null || version != _requestVersion || IsVisible == false)
                return;

            _busy = false;
            _refreshAt = Time.realtimeSinceStartup + result.RetryAfter;
            ShowResult(result);
        };

        if (authorize)
            YandexLeaderboard.Authorize(complete);
        else
            YandexLeaderboard.Read(complete);
    }

    private void ShowResult(YandexLeaderboard.Result result)
    {
        const int gapFontSize = 14;
        const int gapHeight = 20;
        const int highlightedRankCount = 3;
        const int rowHeight = 40;
        const int maximumPlayerNameLength = 80;
        const int rowFontSize = 17;

        _lastResult = result;
        bool canSignIn;

        if (result.AuthorizationKnown && result.Authorized == false && YandexSdk.IsInitialized)
        {
            canSignIn = true;
        }
        else
        {
            canSignIn = false;
        }

        _signIn.gameObject.SetActive(canSignIn);

        if (canSignIn)
        {
            _summary.text = Localization.Get("Sign in to add your result and see your rank.\nYou can keep playing as a guest.");
        }
        else
        {
            _summary.text = Localization.Format("Your best: {0}", Mathf.Max(LevelProgress.BestCompletedLevel, result.BestScore));
        }

        if (result.Authorized && result.UserRank > 0)
        {
            _summary.text = Localization.Format(
                "Your rank: #{0}   Best: {1}",
                result.UserRank,
                Mathf.Max(LevelProgress.BestCompletedLevel, result.BestScore));
        }

        if (result.Authorized && string.IsNullOrEmpty(result.ScoreStatus) == false)
            _summary.text += "\n" + Localization.Get("Your result is saved locally. Sync will retry.");

        RectTransform refreshRect = _refresh.GetComponent<RectTransform>();
        Vector2 refreshAnchor;

        if (canSignIn)
        {
            refreshAnchor = new Vector2(1, 0);
        }
        else
        {
            refreshAnchor = new Vector2(0.5f, 0);
        }

        Vector2 refreshPosition;

        if (canSignIn)
        {
            refreshPosition = new Vector2(-72, 34);
        }
        else
        {
            refreshPosition = new Vector2(0, 34);
        }

        Vector2 refreshSize;

        if (canSignIn)
        {
            refreshSize = new Vector2(108, 42);
        }
        else
        {
            refreshSize = new Vector2(220, 42);
        }

        Place(refreshRect, refreshAnchor, refreshPosition, refreshSize);

        _status.gameObject.SetActive(result.Succeeded == false || result.Entries.Length == 0);
        switch (result.Status)
        {
            case "ok":
                _status.text = Localization.Get("No scores yet. Complete a level to join!");
                break;
            case "unavailable":
                _status.text = Localization.Get("Rankings are available on Yandex Games.");
                break;
            case "notConfigured":
                _status.text = Localization.Get("The leaderboard is not available yet.");
                break;
            case "cancelled":
                _status.text = Localization.Get("Sign-in cancelled. You can keep playing.");
                break;
            case "rateLimited":
                _status.text = Localization.Get("Please wait a moment, then refresh.");
                break;
            default:
                _status.text = Localization.Get("Couldn't load rankings. Check your connection and retry.");
                break;
        }

        if (result.Succeeded == false)
            return;

        int previousRank = 0;

        foreach (YandexLeaderboard.Entry entry in result.Entries)
        {
            if (entry.Rank == previousRank)
                continue;

            if (previousRank > 0 && entry.Rank > previousRank + 1)
            {
                TMP_Text gap = Label("Gap", _content, "...", gapFontSize);
                gap.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = gapHeight;
            }

            previousRank = entry.Rank;
            RectTransform row = Rect("Rank" + entry.Rank, _content);
            Color rowColor;

            if (entry.IsUser)
            {
                rowColor = new Color32(255, 220, 141, 255);
            }
            else if (entry.Rank <= highlightedRankCount)
            {
                rowColor = new Color32(255, 237, 186, 255);
            }
            else
            {
                rowColor = new Color32(245, 229, 204, 255);
            }

            Image(row, rowColor);
            row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = rowHeight;
            string name;

            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                name = Localization.Get("Player");
            }
            else
            {
                name = entry.Name;
            }

            name = name.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');

            if (name.Length > maximumPlayerNameLength)
                name = name.Substring(0, maximumPlayerNameLength);

            Columns(row, entry.Rank.ToString(), name, entry.Score.ToString(), rowFontSize);
        }

        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        _scroll.verticalNormalizedPosition = 1;
    }

    private void ClearRows()
    {
        for (int i = _content.childCount - 1; i >= 0; i--)
        {
            GameObject child = _content.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    private void RefreshLanguage()
    {
        if (_lastResult != null)
        {
            ClearRows();
            ShowResult(_lastResult);
        }
        else if (_busy)
        {
            string statusKey;

            if (_authorizing)
            {
                statusKey = "Waiting for sign-in...";
            }
            else
            {
                statusKey = "Loading rankings...";
            }

            _status.text = Localization.Get(statusKey);
            _summary.text = Localization.Format("Your best: {0}", LevelProgress.BestCompletedLevel);
        }
    }

    private void Columns(RectTransform parent, string rank, string name, string score, float size, bool localizeName = false)
    {
        TMP_Text rankLabel = Label("Rank", parent, rank, size);
        Place(rankLabel.rectTransform, new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(48, 36));
        TMP_Text nameLabel = Label("Player", parent, name, size, localizeName);
        Stretch(nameLabel.rectTransform);
        nameLabel.rectTransform.offsetMin = new Vector2(60, 0);
        nameLabel.rectTransform.offsetMax = new Vector2(-74, 0);
        nameLabel.alignment = TextAlignmentOptions.MidlineLeft;
        nameLabel.enableWordWrapping = false;
        nameLabel.overflowMode = TextOverflowModes.Ellipsis;
        TMP_Text scoreLabel = Label("Score", parent, score, size);
        Place(scoreLabel.rectTransform, new Vector2(1, 0.5f), new Vector2(-38, 0), new Vector2(70, 36));
    }

    private TMP_Text Label(string name, Transform parent, string text, float size, bool localize = true)
    {
        var label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();

        if (_font != null)
            label.font = _font;

        if (localize)
        {
            label.text = Localization.Get(text);
        }
        else
        {
            label.text = text;
        }

        label.fontSize = size;
        label.color = Ink;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.richText = false;

        // The scene binder must not replace this live status with its initial loading message.
        if (localize == false || name == "Status")
            label.gameObject.AddComponent<LocalizationIgnore>();

        if (localize && name != "Status")
            LocalizationBinder.Set(label, text);

        return label;
    }

    private void BuildRefreshIcon()
    {
        const int iconSize = 20;
        const int horizontalPadding = 8;
        const int iconTextSpacing = 6;
        const int minimumFontSize = 12;
        const int maximumFontSize = 17;

        _refreshIcon = Rect("Icon", _refresh.transform);
        _refreshIcon.SetAsFirstSibling();
        _refreshIcon.sizeDelta = Vector2.one * iconSize;
        var icon = Image(_refreshIcon, Ink, false);
        icon.sprite = Resources.Load<Sprite>("Textures/Icons/restart_icon");
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        var iconLayout = _refreshIcon.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        iconLayout.minWidth = iconLayout.preferredWidth = iconSize;
        iconLayout.minHeight = iconLayout.preferredHeight = iconSize;

        var layout = _refresh.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.padding = new RectOffset(horizontalPadding, horizontalPadding, 0, 0);
        layout.spacing = iconTextSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        _refreshText.margin = Vector4.zero;
        _refreshText.enableWordWrapping = false;
        _refreshText.enableAutoSizing = true;
        _refreshText.fontSizeMin = minimumFontSize;
        _refreshText.fontSizeMax = maximumFontSize;
    }

    private void BuildCloseButton()
    {
        RectTransform rect = Rect("Close", _card);
        Place(rect, Vector2.one, new Vector2(-30, -32), new Vector2(40, 40));
        var background = Image(rect, new Color32(227, 101, 78, 255), false);
        background.sprite = Resources.Load<Sprite>("Textures/Icons/close-icon");
        background.preserveAspect = true;
        var shadow = rect.gameObject.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = new Color32(72, 72, 72, 112);
        shadow.effectDistance = new Vector2(0, -3);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = background;
        var source = _root.Find("OptionsPanel/CloseButton")?.GetComponent<UnityEngine.UI.Button>();

        if (source != null)
            button.colors = source.colors;

        button.onClick.AddListener(Close);
    }

    private UnityEngine.UI.Button Button(
        string name,
        Transform parent,
        string text,
        Vector2 anchor,
        Vector2 position,
        Vector2 size,
        UnityEngine.Events.UnityAction action,
        bool languageStyle = false)
    {
        const int languageButtonFontSize = 17;
        const int defaultFontSize = 15;
        const int horizontalTextMargin = 5;
        const int verticalTextMargin = 2;

        RectTransform rect = Rect(name, parent);
        Place(rect, anchor, position, size);
        UnityEngine.UI.Graphic background;

        if (languageStyle)
        {
            var face = rect.gameObject.AddComponent<LanguageCardGraphic>();
            face.color = new Color32(255, 247, 229, 255);
            face.BorderColor = new Color32(190, 162, 122, 255);
            background = face;
            var shadow = rect.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color32(108, 78, 48, 70);
            shadow.effectDistance = new Vector2(0, -3);
        }
        else
        {
            background = Image(rect, new Color32(241, 184, 112, 255));
        }

        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = background;

        if (languageStyle)
        {
            var colors = button.colors;
            colors.highlightedColor = new Color(1, 0.96f, 0.84f);
            colors.pressedColor = new Color(0.84f, 0.9f, 0.73f);
            button.colors = colors;
        }

        button.onClick.AddListener(action);
        float fontSize;

        if (languageStyle)
        {
            fontSize = languageButtonFontSize;
        }
        else
        {
            fontSize = defaultFontSize;
        }

        TMP_Text label = Label("Text", rect, text, fontSize);
        Stretch(label.rectTransform);
        label.margin = new Vector4(horizontalTextMargin, verticalTextMargin, horizontalTextMargin, verticalTextMargin);
        return button;
    }

    private UnityEngine.UI.Image Image(RectTransform rect, Color tint, bool rounded = true)
    {
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = tint;

        if (rounded && _panelSprite != null)
        {
            image.sprite = _panelSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
        }

        return image;
    }
}
