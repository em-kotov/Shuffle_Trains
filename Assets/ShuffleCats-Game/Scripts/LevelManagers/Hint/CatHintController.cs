using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(220)]
public sealed class CatHintController : MonoBehaviour
{
    private readonly List<Subscriber> _trackOrder = new List<Subscriber>();
    [SerializeField]
    [Min(1f)]
    private float _idleSeconds = 8f;
    [SerializeField]
    [Min(1f)]
    private float _lastCatIdleSeconds = 5f;
    [SerializeField]
    [Min(1f)]
    private float _highlightSeconds = 5f;

    private IReadOnlyList<Subscriber> _cats;
    private IReadOnlyList<Station> _stations;
    private ParkingRegistrator _parking;
    private TrackRegistrator _track;
    private UIPanel[] _panels;
    private Canvas _canvas;
    private RectTransform _root;
    private RectTransform _highlight;
    private CanvasGroup _highlightGroup;
    private CatHintGraphic _highlightGraphic;
    private UnityEngine.UI.Button _button;
    private TMPro.TMP_Text _feedback;
    private Subscriber _suggestion;
    private float _idle;
    private float _shownAt;
    private float _nextAttempt;
    private float _feedbackUntil;
    private int _requestedFrame = -1;
    private int _revision;
    private bool _pendingRequest;

    public bool TutorialActive { get; set; }

    public UnityEngine.UI.Button HintButton => _button;

    private void Update()
    {
        const float hintRetryDelaySeconds = 0.5f;

        if (_cats == null)
            return;

        bool allowed = CanShowHints();
        int waiting = 0;

        foreach (Subscriber cat in _cats)
        {
            if (cat == null || cat.gameObject.activeInHierarchy == false)
                continue;

            if (cat.Parking.isActiveAndEnabled && cat.Parking.IsAtBorderCell == false)
                waiting++;

            if (cat.IsOnTrack && _trackOrder.Contains(cat) == false)
                _trackOrder.Add(cat);
        }

        _button.gameObject.SetActive(true);
        _button.interactable = HintSettings.Enabled;

        if (allowed == false || waiting == 0)
        {
            ClearHint();
            _pendingRequest = false;
            _idle = 0;
            _feedback.gameObject.SetActive(false);
            return;
        }

        if ((Input.anyKeyDown || Input.GetMouseButtonDown(0) || TouchBegan()) && _requestedFrame != Time.frameCount)
        {
            ClearHint();
            _pendingRequest = false;
            _idle = 0;
            _feedback.gameObject.SetActive(false);
        }

        _idle += Time.deltaTime;
        _feedback.gameObject.SetActive(Time.unscaledTime < _feedbackUntil);

        if (_suggestion != null)
        {
            if (_suggestion.Parking.isActiveAndEnabled == false || _suggestion.Parking.IsMoving ||
                _suggestion.Parking.IsAtBorderCell || Time.time - _shownAt >= _highlightSeconds ||
                Revision() != _revision)
            {
                ClearHint();
                _idle = 0;
            }

            return;
        }

        if (_pendingRequest == false)
        {
            float idleThreshold;

            if (waiting == 1)
            {
                idleThreshold = _lastCatIdleSeconds;
            }
            else
            {
                idleThreshold = _idleSeconds;
            }

            if (_idle < idleThreshold)
                return;
        }

        if (Time.time < _nextAttempt)
            return;

        _nextAttempt = Time.time + hintRetryDelaySeconds;

        if (_parking.HasMovingCars || _track.IsCountAllows() == false || IsInTransition())
        {
            if (_pendingRequest)
                ShowFeedback("Wait for the cats…");

            return;
        }

        Subscriber next = ChooseCat();

        if (next != null)
        {
            _suggestion = next;
            _shownAt = Time.time;
            _revision = Revision();
            _highlight.gameObject.SetActive(true);
            _feedbackUntil = 0;
        }
        else if (_pendingRequest)
        {
            ShowFeedback("No clear hint — try another approach");
        }

        _pendingRequest = false;
        _idle = 0;
    }

    private void LateUpdate()
    {
        const float pulseFrequencyMultiplier = 1.5f;
        const float fadeOutSeconds = 0.4f;
        const float minimumPulseOpacity = 0.8f;
        const float fadeInSeconds = 0.2f;
        const float pulseOpacityRange = 0.2f;

        if (_suggestion == null || _highlight.gameObject.activeSelf == false)
            return;

        Camera camera = Camera.main;

        if (camera == null)
            return;

        float age = Time.time - _shownAt;
        float pulse = 0.5f + (0.5f * Mathf.Sin(age * Mathf.PI * pulseFrequencyMultiplier));
        _highlightGraphic.Follow(_suggestion.CatSprite, camera, _canvas, age);
        _highlightGroup.alpha = Mathf.Clamp01(age / fadeInSeconds) * Mathf.Clamp01((_highlightSeconds - age) / fadeOutSeconds) * (minimumPulseOpacity + (pulseOpacityRange * pulse));
    }

    private void OnDisable()
    {
        ClearHint();

        if (_button != null)
            _button.gameObject.SetActive(false);

        if (_feedback != null)
            _feedback.gameObject.SetActive(false);
    }

    public void Initialize(
        IReadOnlyList<Subscriber> cats,
        IReadOnlyList<Station> stations,
        ParkingRegistrator parking,
        TrackRegistrator track,
        Canvas canvas)
    {
        const int hintButtonSize = 60;
        const int feedbackFontSize = 17;

        _cats = cats;
        _stations = stations;
        _parking = parking;
        _track = track;
        _canvas = canvas;
        _panels = FindObjectsOfType<UIPanel>(true);
        ResponsiveGameUI responsive = canvas.GetComponent<ResponsiveGameUI>();

        if (responsive != null)
        {
            _root = responsive.SafeRoot;
        }
        else
        {
            _root = canvas.transform as RectTransform;
        }

        _highlight = NewRect("CatHintHighlight", typeof(CanvasGroup), typeof(CatHintGraphic));
        _highlight.SetAsFirstSibling();
        _highlightGraphic = _highlight.GetComponent<CatHintGraphic>();
        _highlightGraphic.raycastTarget = false;
        _highlight.anchorMin = Vector2.zero;
        _highlight.anchorMax = Vector2.one;
        _highlight.offsetMin = _highlight.offsetMax = Vector2.zero;
        _highlightGroup = _highlight.GetComponent<CanvasGroup>();
        _highlightGroup.blocksRaycasts = false;
        _highlightGroup.interactable = false;
        _highlight.gameObject.SetActive(false);

        RectTransform button = NewRect("HintButton", typeof(HintLampGraphic), typeof(UnityEngine.UI.Button));
        button.SetAsFirstSibling();
        button.sizeDelta = Vector2.one * hintButtonSize;
        _button = button.GetComponent<UnityEngine.UI.Button>();
        _button.targetGraphic = button.GetComponent<HintLampGraphic>();
        var colors = _button.colors;
        colors.highlightedColor = new Color(1f, 0.97f, 0.84f);
        colors.pressedColor = new Color(0.88f, 0.79f, 0.60f);
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = HintLampGraphic.DisabledTint;
        _button.colors = colors;
        _button.onClick.AddListener(RequestHint);

        if (responsive != null)
            responsive.RegisterHintButton(button);

        RectTransform feedback = NewRect("HintFeedback", typeof(TMPro.TextMeshProUGUI));
        feedback.SetAsFirstSibling();
        feedback.anchorMin = feedback.anchorMax = new Vector2(0.5f, 0);
        feedback.pivot = new Vector2(0.5f, 0);
        feedback.anchoredPosition = new Vector2(0, 88);
        feedback.sizeDelta = new Vector2(320, 42);
        _feedback = feedback.GetComponent<TMPro.TMP_Text>();

        foreach (TMPro.TMP_Text existing in canvas.GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            if (existing != _feedback && existing.font != null)
            {
                _feedback.font = existing.font;
                break;
            }
        }

        _feedback.fontSize = feedbackFontSize;
        _feedback.alignment = TMPro.TextAlignmentOptions.Center;
        _feedback.color = new Color32(92, 66, 49, 255);
        _feedback.raycastTarget = false;
        feedback.gameObject.SetActive(false);
    }

    public void RequestHint()
    {
        if (CanShowHints() == false)
            return;

        AudioManager.PlayUIClickStatic();
        ClearHint();
        _requestedFrame = Time.frameCount;
        _pendingRequest = true;
        _idle = 0;
        _nextAttempt = 0;
    }

    public Subscriber SuggestTutorialCat()
    {
        if (_cats == null || _parking.HasMovingCars || _track.IsCountAllows() == false || IsInTransition())
            return null;

        return ChooseCat();
    }

    private static bool TouchBegan()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
                return true;
        }

        return false;
    }

    private RectTransform NewRect(string name, params System.Type[] components)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(_root, false);

        foreach (System.Type component in components)
            go.AddComponent(component);

        return rect;
    }

    private bool IsGameplayVisible()
    {
        if (_cats == null)
            return false;

        foreach (UIPanel panel in _panels)
        {
            if (panel != null && panel.BlocksGameplay)
                return false;
        }

        return true;
    }

    // Canvas focus may be false before the first WebGL click; browser/SDK signals own pausing.
    private bool CanShowHints() => TutorialActive == false && HintSettings.Enabled && IsGameplayVisible() && YandexSdk.IsPaused == false && Time.timeScale > 0;

    private bool IsInTransition()
    {
        foreach (Subscriber cat in _cats)
        {
            if (cat == null || cat.gameObject.activeInHierarchy == false)
                continue;

            if (cat.Sorter.IsSorting || (cat.Parking.enabled && cat.Parking.IsAtBorderCell) ||
                (cat.Parking.enabled == false && cat.IsOnTrack == false && cat.Sorter.IsFinished == false))
                return true;
        }

        return false;
    }

    private Subscriber ChooseCat()
    {
        const int maximumSupportedCats = 63;

        if (_cats.Count > maximumSupportedCats)
            return null;

        var data = new CatHintPlanner.Cat[_cats.Count];
        ulong parked = 0;

        for (int i = 0; i < _cats.Count; i++)
        {
            Subscriber cat = _cats[i];
            data[i] = new CatHintPlanner.Cat
            {
                Color = (int)cat.Color,
                Capacity = cat.SeatCount,
                Right = cat.Passengers.RightPassengerCount,
                Wrong = cat.Passengers.CopyWrongPassengerColors(),
            };

            if (cat.Parking.isActiveAndEnabled == false)
                continue;

            parked |= 1UL << i;

            foreach (ParkingCar blocker in cat.Parking.GetBlockingCars())
            {
                for (int b = 0; b < _cats.Count; b++)
                {
                    if (_cats[b].Parking == blocker)
                        data[i].Blockers |= 1UL << b;
                }
            }
        }

        var active = new List<int>();

        foreach (Subscriber cat in _trackOrder)
        {
            if (cat != null && cat.gameObject.activeInHierarchy && cat.Sorter.IsFinished == false)
            {
                for (int i = 0; i < _cats.Count; i++)
                {
                    if (_cats[i] == cat)
                        active.Add(i);
                }
            }
        }

        var stations = new CatHintPlanner.StationState[_stations.Count];

        for (int s = 0; s < _stations.Count; s++)
        {
            _stations[s].HavePassengers(out int count, out CatColor color);
            stations[s] = new CatHintPlanner.StationState
            {
                Color = (int)color,
                Count = count,
                Capacity = count + _stations[s].GetFreeSeats().Count,
            };
        }

        int index = new CatHintPlanner(data, _track.MaxCount).FindNext(parked, active.ToArray(), stations);

        if (index >= 0)
        {
            return _cats[index];
        }
        else
        {
            return null;
        }
    }

    private int Revision()
    {
        const int hashMultiplier = 31;
        const int stationCountHashMultiplier = 10;

        unchecked
        {
            int value = _track.CurrentCount;

            foreach (Subscriber cat in _cats)
            {
                int activeFlag;

                if (cat.Parking.isActiveAndEnabled)
                {
                    activeFlag = 1;
                }
                else
                {
                    activeFlag = 0;
                }

                value = (value * hashMultiplier) + activeFlag;
                int movingFlag;

                if (cat.Parking.IsMoving || cat.Parking.IsAtBorderCell)
                {
                    movingFlag = 1;
                }
                else
                {
                    movingFlag = 0;
                }

                value = (value * hashMultiplier) + movingFlag;
                value = (value * hashMultiplier) + cat.Passengers.RightPassengerCount;

                foreach (int color in cat.Passengers.CopyWrongPassengerColors())
                    value = (value * hashMultiplier) + color + 1;
            }

            foreach (Station station in _stations)
            {
                station.HavePassengers(out int count, out CatColor color);
                value = (value * hashMultiplier) + (count * stationCountHashMultiplier) + (int)color;
            }

            return value;
        }
    }

    private void ShowFeedback(string text)
    {
        const float feedbackDurationSeconds = 2.5f;

        LocalizationBinder.Set(_feedback, text);
        _feedbackUntil = Time.unscaledTime + feedbackDurationSeconds;
        _feedback.gameObject.SetActive(true);
    }

    private void ClearHint()
    {
        _suggestion = null;

        if (_highlight != null)
            _highlight.gameObject.SetActive(false);
    }
}
