using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(260)]
public sealed class TutorialOverlay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    private readonly Vector3[] _corners = new Vector3[4];
    private readonly List<SpriteRenderer> _focusSprites = new();
    private readonly List<SpriteRenderer[]> _focusGroups = new();
    private readonly List<Rect> _holes = new();
    private readonly Dictionary<Sprite, Vector2[]> _vertices = new();
    private Canvas _canvas;
    private Subscriber[] _cats;
    private RectTransform _root;
    private RectTransform _card;
    private RectTransform _hand;
    private RectTransform _safeRoot;
    private TutorialSpotlightGraphic _spotlight;
    private TMP_Text _title;
    private TMP_Text _body;
    private TMP_Text _progress;
    private TMP_Text _skip;
    private TMP_Text _continueLabel;
    private UnityEngine.UI.Button _continueButton;
    private Subscriber _target;
    private RectTransform _focusUI;
    private UnityEngine.UI.Button _targetButton;
    private UnityEngine.UI.Button _pressedButton;
    private bool _pressed;
    private ClickDetector _pressedTarget;
    private int _pointerId;
    private int _releasedFrame = -1;
    private Rect _hole;
    private float _age;
    private bool _hasHole;
    private bool _handFromLeft;
    private bool _handSideways;
    private string _titleKey;
    private string _bodyKey;
    private int _step;
    private int _total;
    private bool _layoutDirty = true;
    private Rect _layoutSafe;
    private string _layoutTitle;
    private string _layoutBody;
    private string _continueKey = "Continue tutorial";

    public event Action SkipRequested;

    public event Action ContinueRequested;

    public event Action BeforeTargetButtonClick;

    public Func<bool> InteractionAllowed { private get; set; }

    public Rect SpotlightRect => _hole;

    public IReadOnlyList<Rect> SpotlightRects => _holes;

    public RectTransform Card => _card;

    private bool CanInteract => InteractionAllowed?.Invoke() ?? (YandexSdk.IsPaused == false && Time.timeScale > 0);

    private Camera UICamera
    {
        get
        {
            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }
            else
            {
                return _canvas.worldCamera;
            }
        }
    }

    private void OnEnable()
    {
        Localization.Changed += RefreshText;
        RefreshText();
    }

    private void OnDisable()
    {
        Localization.Changed -= RefreshText;
        _pressed = false;
        _pressedTarget = null;
        _pressedButton = null;
    }

    private void LateUpdate()
    {
        const int focusPadding = 6;
        const float handBounceAmplitude = 7f;
        const float handAnimationFrequency = 5f;
        const int handVerticalOffset = 10;
        const int handHorizontalOffset = 12;
        const float handBaseScale = 0.86f;
        const float handScalePulse = 0.04f;
        const int sidewaysHandAngleDegrees = 90;
        const int uprightHandAngleDegrees = 18;

        if (_canvas == null)
            return;

        if (_releasedFrame >= 0 && _releasedFrame <= Time.frameCount)
        {
            _pressed = false;
            _pressedTarget = null;
            _pressedButton = null;
        }

        _age += Time.unscaledDeltaTime;
        Rect safe = SafeRect();
        _holes.Clear();

        if (_focusUI != null && _focusUI.gameObject.activeInHierarchy)
        {
            _focusUI.GetWorldCorners(_corners);
            Vector2 min = _root.InverseTransformPoint(_corners[0]), max = _root.InverseTransformPoint(_corners[2]);
            _holes.Add(Rect.MinMaxRect(min.x - focusPadding, min.y - focusPadding, max.x + focusPadding, max.y + focusPadding));
        }

        foreach (SpriteRenderer renderer in _focusSprites)
        {
            if (TryGetSpriteRect(renderer, out Rect bounds))
                _holes.Add(bounds);
        }

        foreach (SpriteRenderer[] group in _focusGroups)
        {
            Rect combined = default;
            bool found = false;

            foreach (SpriteRenderer renderer in group)
            {
                if (TryGetSpriteRect(renderer, out Rect bounds) == false)
                    continue;

                if (found)
                {
                    combined = Rect.MinMaxRect(
                        Mathf.Min(combined.xMin, bounds.xMin),
                        Mathf.Min(combined.yMin, bounds.yMin),
                        Mathf.Max(combined.xMax, bounds.xMax),
                        Mathf.Max(combined.yMax, bounds.yMax));
                }
                else
                {
                    combined = bounds;
                }

                found = true;
            }

            if (found)
                _holes.Add(combined);
        }

        if (_layoutDirty || safe != _layoutSafe)
        {
            _layoutSafe = safe;
            _layoutDirty = false;
            _layoutTitle = null;
        }

        if (_layoutTitle != _title.text || _layoutBody != _body.text)
        {
            _layoutTitle = _title.text;
            _layoutBody = _body.text;
            LayoutCard(safe);
        }

        if (_holes.Count > 0)
        {
            _hasHole = true;
        }
        else
        {
            _hasHole = false;
        }

        if (_hasHole)
        {
            _hole = _holes[0];
        }
        else
        {
            _hole = default;
        }

        _spotlight.SetHoles(_holes);
        _hand.gameObject.SetActive(_hasHole);

        if (_hasHole)
        {
            float bounce = handBounceAmplitude * (0.5f + (0.5f * Mathf.Sin(_age * handAnimationFrequency)));
            float side;

            if (_handFromLeft)
            {
                side = -1;
            }
            else
            {
                side = 1;
            }

            float handHeight;

            if (_handSideways)
            {
                handHeight = 0;
            }
            else
            {
                handHeight = -handVerticalOffset - bounce;
            }

            _hand.anchoredPosition = _hole.center + new Vector2(side * (handHorizontalOffset + bounce), handHeight);
            _hand.localScale = new Vector3(side, 1, 1) * (handBaseScale + (handScalePulse * Mathf.Sin(_age * handAnimationFrequency)));
            float handAngle;

            if (_handSideways)
            {
                handAngle = sidewaysHandAngleDegrees;
            }
            else
            {
                handAngle = uprightHandAngleDegrees;
            }

            _hand.localRotation = Quaternion.Euler(0, 0, side * handAngle);
        }
    }

    public static TutorialOverlay Create(Canvas canvas)
    {
        RectTransform root = NewRect("TutorialOverlay", canvas.transform);
        Stretch(root);
        var overlay = root.gameObject.AddComponent<TutorialOverlay>();
        overlay.Build(canvas);
        return overlay;
    }

    public void ShowTarget(Subscriber target, string title, string body, int step, int total)
    {
        ClearTarget();
        _target = target;
        _focusSprites.Add(target.CatSprite);
        _age = 0;
        _pressedTarget = null;
        _continueButton.gameObject.SetActive(false);
        _layoutDirty = true;
        SetText(title, body, step, total);
    }

    public void ShowLesson(string title, string body, int step, int total, RectTransform focusUI = null, SpriteRenderer[] sprites = null, SpriteRenderer[][] groups = null)
    {
        ClearTarget();
        _continueButton.gameObject.SetActive(true);
        _continueButton.interactable = true;
        _continueKey = "Continue tutorial";
        _focusUI = focusUI;

        if (sprites != null)
            _focusSprites.AddRange(sprites);

        if (groups != null)
            _focusGroups.AddRange(groups);

        _layoutDirty = true;
        SetText(title, body, step, total);
    }

    public void ShowButton(UnityEngine.UI.Button button, string title, string body, int step, int total)
    {
        ClearTarget();
        _targetButton = button;
        _focusUI = button.transform as RectTransform;
        _continueButton.gameObject.SetActive(false);
        _layoutDirty = true;
        SetText(title, body, step, total);
    }

    public void ShowPending(string text)
    {
        ClearTarget();
        _continueKey = text;
        _continueButton.gameObject.SetActive(true);
        _continueButton.interactable = false;
        LocalizationBinder.Set(_continueLabel, text);
        _layoutDirty = true;
    }

    public void ClearTarget()
    {
        _target = null;
        _targetButton = null;
        _focusUI = null;
        _focusSprites.Clear();
        _focusGroups.Clear();
        _hasHole = false;
        _pressedTarget = null;
        _pressedButton = null;
        _pressed = false;
    }

    public void OnPointerDown(PointerEventData data)
    {
        if (_pressed || CanTap(data) == false)
            return;

        _pressed = true;

        if (_target != null)
        {
            _pressedTarget = _target.Clicks;
        }
        else
        {
            _pressedTarget = null;
        }

        _pressedButton = _targetButton;
        _pointerId = data.pointerId;
        _releasedFrame = -1;

        if (_pressedTarget != null)
            _pressedTarget.OnPointerDown(data);
    }

    public void OnPointerUp(PointerEventData data)
    {
        if (data.pointerId != _pointerId)
            return;

        // PointerClick follows PointerUp in this tick; defer releasing the latch until LateUpdate.
        _releasedFrame = Time.frameCount;

        if (CanTap(data) == false)
        {
            _pressed = false;
            _pressedTarget = null;
            _pressedButton = null;
        }
    }

    public void OnPointerClick(PointerEventData data)
    {
        if (data.pointerId != _pointerId || _pressed == false)
            return;

        ClickDetector pressed = _pressedTarget;
        UnityEngine.UI.Button button = _pressedButton;
        _pressed = false;
        _pressedTarget = null;
        _pressedButton = null;

        if (CanTap(data) == false)
            return;

        if (pressed != null && _target != null && _target.Clicks == pressed)
        {
            pressed.OnPointerClick(data);
        }
        else if (button != null && button == _targetButton)
        {
            BeforeTargetButtonClick?.Invoke();
            button.onClick.Invoke();
        }
        else if (_target == null && _targetButton == null)
        {
            ContinueRequested?.Invoke();
        }
    }

    private static float OverlapArea(Rect a, Rect b) =>
        Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin)) *
        Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));

    private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, float size, Color color)
    {
        var text = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();

        if (font != null)
            text.font = font;

        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void Build(Canvas canvas)
    {
        const int cardCornerRadius = 18;
        const int cardBorderWidth = 2;
        const int bodyLineSpacing = 3;
        const int buttonCornerRadius = 10;

        _canvas = canvas;
        _cats = FindObjectsOfType<Subscriber>();
        _root = (RectTransform)transform;
        _safeRoot = canvas.GetComponent<ResponsiveGameUI>()?.SafeRoot ?? (RectTransform)canvas.transform;
        _spotlight = gameObject.AddComponent<TutorialSpotlightGraphic>();
        _spotlight.raycastTarget = true;

        _hand = NewRect("AnimatedHand", transform);
        _hand.sizeDelta = new Vector2(80, 95);
        _hand.pivot = new Vector2(0, 1);
        _hand.gameObject.AddComponent<TutorialHandGraphic>().raycastTarget = false;

        _card = NewRect("Instruction", transform);
        var background = _card.gameObject.AddComponent<LanguageCardGraphic>();
        background.Radius = cardCornerRadius;
        background.BorderWidth = cardBorderWidth;
        background.BorderColor = new Color32(196, 161, 104, 255);
        background.color = new Color32(255, 247, 228, 255);
        background.raycastTarget = true;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/Eno Black SDF");
        _progress = Label("Progress", _card, font, 13, new Color32(143, 104, 64, 255));
        _progress.alignment = TextAlignmentOptions.MidlineLeft;
        _title = Label("Title", _card, font, 22, new Color32(82, 57, 43, 255));
        _body = Label("Explanation", _card, font, 18, new Color32(94, 70, 52, 255));
        _body.lineSpacing = bodyLineSpacing;

        RectTransform skipRect = NewRect("Skip", _card);
        var skipBackground = skipRect.gameObject.AddComponent<LanguageCardGraphic>();
        skipBackground.color = new Color32(242, 229, 202, 255);
        skipBackground.BorderWidth = 0;
        skipBackground.Radius = buttonCornerRadius;
        var skipButton = skipRect.gameObject.AddComponent<UnityEngine.UI.Button>();
        skipButton.targetGraphic = skipBackground;
        skipButton.onClick.AddListener(() =>
        {
            if (CanInteract)
                SkipRequested?.Invoke();
        });
        _skip = Label("Label", skipRect, font, 13, new Color32(118, 89, 64, 255));
        _skip.alignment = TextAlignmentOptions.Center;
        Stretch(_skip.rectTransform);

        // Prevent keyboard selection from dispatching a click through an invisible control.
        skipButton.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };

        RectTransform continueRect = NewRect("Continue", _card);
        var continueBackground = continueRect.gameObject.AddComponent<LanguageCardGraphic>();
        continueBackground.color = new Color32(246, 201, 111, 255);
        continueBackground.BorderWidth = 0;
        continueBackground.Radius = buttonCornerRadius;
        _continueButton = continueRect.gameObject.AddComponent<UnityEngine.UI.Button>();
        _continueButton.targetGraphic = continueBackground;
        _continueButton.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
        _continueButton.onClick.AddListener(() =>
        {
            if (_continueButton.interactable && CanInteract)
                ContinueRequested?.Invoke();
        });
        _continueLabel = Label("Label", continueRect, font, 17, new Color32(82, 57, 43, 255));
        _continueLabel.alignment = TextAlignmentOptions.Center;
        Stretch(_continueLabel.rectTransform);
        continueRect.gameObject.SetActive(false);
    }

    private void SetText(string title, string body, int step, int total)
    {
        _titleKey = title;
        _bodyKey = body;
        _step = step;
        _total = total;
        RefreshText();
    }

    private void RefreshText()
    {
        if (_title == null || string.IsNullOrEmpty(_titleKey))
            return;

        LocalizationBinder.Set(_title, _titleKey);
        LocalizationBinder.Set(_body, _bodyKey);
        LocalizationBinder.Set(_progress, "LEARN TO PLAY  {0}/{1}", _step, _total);
        LocalizationBinder.Set(_skip, "Skip tutorial");
        LocalizationBinder.Set(_continueLabel, _continueKey);
        _title.enableAutoSizing = false;
        _body.enableAutoSizing = false;
    }

    private bool TryGetSpriteRect(SpriteRenderer renderer, out Rect result)
    {
        const int highlightPadding = 9;

        result = default;
        Camera camera = Camera.main;

        if (renderer == null || renderer.gameObject.activeInHierarchy == false || camera == null || renderer.sprite == null || renderer.enabled == false)
            return false;

        if (_vertices.TryGetValue(renderer.sprite, out Vector2[] vertices) == false)
        {
            vertices = renderer.sprite.vertices;
            _vertices.Add(renderer.sprite, vertices);
        }

        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;

        foreach (Vector2 vertex in vertices)
        {
            float pointX;

            if (renderer.flipX)
            {
                pointX = -vertex.x;
            }
            else
            {
                pointX = vertex.x;
            }

            float pointY;

            if (renderer.flipY)
            {
                pointY = -vertex.y;
            }
            else
            {
                pointY = vertex.y;
            }

            Vector3 point = new Vector3(pointX, pointY);
            Vector3 screen = camera.WorldToScreenPoint(renderer.transform.TransformPoint(point));

            if (screen.z <= 0)
                return false;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, UICamera, out Vector2 local);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        min -= Vector2.one * highlightPadding;
        max += Vector2.one * highlightPadding;
        Rect bounds = _root.rect;
        result = Rect.MinMaxRect(
            Mathf.Max(bounds.xMin, min.x),
            Mathf.Max(bounds.yMin, min.y),
            Mathf.Min(bounds.xMax, max.x),
            Mathf.Min(bounds.yMax, max.y));
        return result.width > 0 && result.height > 0;
    }

    private Rect SafeRect()
    {
        const int safeAreaPadding = 14;

        _safeRoot.GetWorldCorners(_corners);
        Vector2 min = _root.InverseTransformPoint(_corners[0]);
        Vector2 max = _root.InverseTransformPoint(_corners[2]);
        return Rect.MinMaxRect(min.x + safeAreaPadding, min.y + safeAreaPadding, max.x - safeAreaPadding, max.y - safeAreaPadding);
    }

    private void LayoutCard(Rect safe)
    {
        const int textHorizontalPadding = 36;
        const int sidewaysHandThreshold = 90;
        const int handBoundsWidth = 120;
        const int handBoundsHeight = 140;
        const int handBoundsVerticalOffset = 120;

        Rect placement = FindCardPlacement(safe);
        float width = placement.width;
        float bodyHeight = _body.GetPreferredValues(_body.text, width - textHorizontalPadding, 0).y;
        float titleHeight = _title.GetPreferredValues(_title.text, width - textHorizontalPadding, 0).y;
        float height = placement.height;
        _card.sizeDelta = placement.size;
        _card.anchoredPosition = placement.center;
        Place(_progress.rectTransform, new Vector2(18, -12), new Vector2(width - 150, 28));
        Place(_skip.transform.parent as RectTransform, new Vector2(width - 122, -10), new Vector2(108, 32));
        Place(_title.rectTransform, new Vector2(18, -46), new Vector2(width - textHorizontalPadding, titleHeight));
        Place(_body.rectTransform, new Vector2(18, -50 - titleHeight), new Vector2(width - textHorizontalPadding, bodyHeight));
        Place((RectTransform)_continueButton.transform, new Vector2(18, -height + 50), new Vector2(width - textHorizontalPadding, 36));

        if (_holes.Count > 0)
        {
            Vector2 focus = _holes[0].center;
            var right = new Rect(focus + new Vector2(0, -handBoundsVerticalOffset), new Vector2(handBoundsWidth, handBoundsHeight));
            var left = new Rect(focus + new Vector2(-handBoundsWidth, -handBoundsVerticalOffset), new Vector2(handBoundsWidth, handBoundsHeight));
            float rightCost = (right.width * right.height) - OverlapArea(right, _root.rect);
            float leftCost = (left.width * left.height) - OverlapArea(left, _root.rect);

            for (int i = 1; i < _holes.Count; i++)
            {
                rightCost += OverlapArea(right, _holes[i]);
                leftCost += OverlapArea(left, _holes[i]);
            }

            if (leftCost < rightCost)
            {
                _handFromLeft = true;
            }
            else
            {
                _handFromLeft = false;
            }

            if (_focusUI != null && focus.y - _root.rect.yMin < sidewaysHandThreshold)
            {
                _handSideways = true;
            }
            else
            {
                _handSideways = false;
            }
        }
    }

    private Rect FindCardPlacement(Rect safe)
    {
        const int maximumCardWidth = 520;
        const int wideCardWidth = 440;
        const int mediumCardWidth = 360;
        const int compactCardWidth = 278;
        const int minimumCardWidth = 240;
        const int headerPadding = 64;
        const int textHorizontalPadding = 36;
        const int continueFooterHeight = 48;
        const float widthPenaltyWeight = 0.08f;
        const int verticalPositionPenaltyWeight = 10;
        const int focusOverlapPenaltyWeight = 1000;
        const int catOverlapPenaltyWeight = 30;

        const float gap = 12;
        float widest = Mathf.Min(maximumCardWidth, safe.width);
        Rect best = default;
        float bestScore = float.PositiveInfinity;
        var cats = new List<Rect>();

        foreach (Subscriber cat in _cats)
        {
            if (cat != null && cat.Parking.isActiveAndEnabled && cat.Parking.IsAtBorderCell == false &&
                TryGetSpriteRect(cat.CatSprite, out Rect bounds))
                cats.Add(bounds);
        }

        foreach (float requestedWidth in new[] { widest, wideCardWidth, mediumCardWidth, compactCardWidth, minimumCardWidth })
        {
            float width = Mathf.Min(widest, requestedWidth);
            float height = headerPadding + _title.GetPreferredValues(_title.text, width - textHorizontalPadding, 0).y
                + _body.GetPreferredValues(_body.text, width - textHorizontalPadding, 0).y;
            float footerHeight;

            if (_continueButton.gameObject.activeSelf)
            {
                footerHeight = continueFooterHeight;
            }
            else
            {
                footerHeight = 0;
            }

            height += footerHeight;

            if (height > safe.height)
                continue;

            var xs = new List<float> { safe.center.x - (width * 0.5f), safe.xMin, safe.xMax - width };
            var ys = new List<float> { safe.yMin, safe.yMax - height, safe.center.y - (height * 0.5f) };

            foreach (Rect hole in _holes)
            {
                xs.Add(Mathf.Clamp(hole.xMin - gap - width, safe.xMin, safe.xMax - width));
                xs.Add(Mathf.Clamp(hole.xMax + gap, safe.xMin, safe.xMax - width));
                ys.Add(Mathf.Clamp(hole.yMin - gap - height, safe.yMin, safe.yMax - height));
                ys.Add(Mathf.Clamp(hole.yMax + gap, safe.yMin, safe.yMax - height));
            }

            foreach (float y in ys)
            {
                foreach (float x in xs)
                {
                    var card = new Rect(x, y, width, height);
                    var padded = new Rect(x - gap, y - gap, width + (gap * 2), height + (gap * 2));
                    float score = ((widest - width) * widthPenaltyWeight) + ((y - safe.yMin) / safe.height * verticalPositionPenaltyWeight)
                        + (Mathf.Abs(card.center.x - safe.center.x) / safe.width);

                    foreach (Rect hole in _holes)
                        score += OverlapArea(padded, hole) * focusOverlapPenaltyWeight;

                    foreach (Rect cat in cats)
                        score += OverlapArea(card, cat) / (cat.width * cat.height) * catOverlapPenaltyWeight;

                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    best = card;
                }
            }
        }

        return best;
    }

    private bool CanTap(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || _hasHole == false || CanInteract == false)
            return false;

        if (_target != null && _target.Clicks.InteractionAllowed == false)
            return false;

        if (_targetButton != null && _targetButton.IsInteractable() == false)
            return false;

        if (_target == null && _targetButton == null && (_continueButton.gameObject.activeSelf == false || _continueButton.interactable == false))
            return false;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, data.position, UICamera, out Vector2 local);

        foreach (Rect hole in _holes)
        {
            if (TutorialSpotlightGraphic.Contains(hole, local))
                return true;
        }

        return false;
    }
}
