using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(240)]
public sealed class TutorialController : MonoBehaviour
{
    private const int LevelTwoSteps = 9;
    private static bool _resumePractice;
    private readonly Dictionary<ClickDetector, bool> _inputBefore = new();
    private IReadOnlyList<Subscriber> _cats;
    private CatHintController _hints;
    private TutorialOverlay _overlay;
    private TrackRegistrator _track;
    private SorterRegistrator _sorter;

    private ParkingRegistrator _parking;
    private AutoLoose _autoLoose;
    private OptionsUI _optionsUI;
    private Station[] _stations;
    private UIPanel[] _panels;
    private RectTransform _capacity;
    private SpriteRenderer[] _baskets;
    private Subscriber _target;
    private int _level;
    private int _completed;
    private float _retryAt;
    private bool _running;
    private bool _shown;

    public enum Stage
    {
        FirstCat,
        Baskets,
        LastCat,
        Capacity,
        DemoFirst,
        DemoSecond,
        WaitingForDeadlock,
        FullTrack,
        Blocked,
        OpenSettings,
        Restart,
        PracticeFirst,
        PracticeBlue,
        PracticeLast,
        Hint,
        Finished,
    }

    public Subscriber Target => _target;

    public int CompletedSteps => _completed;

    public bool IsRunning => _running;

    public bool IsReadingBaskets => CurrentStage == Stage.Baskets;

    public Stage CurrentStage { get; private set; }

    private int TotalSteps
    {
        get
        {
            if (_level == 1)
            {
                return 3;
            }
            else if (_level == 2)
            {
                return LevelTwoSteps;
            }
            else
            {
                return 1;
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => _resumePractice = false;

    private void Update()
    {
        const float retryDelaySeconds = 0.2f;
        const int restartPromptStep = 6;
        const int capacityLessonStep = 3;
        const int basketLessonStep = 2;
        const int wrongOrderStep = 7;
        const int hintPromptStep = 8;
        const int completionStep = 9;

        if (_running == false)
            return;

        bool visible = GameplayVisible();
        _overlay.gameObject.SetActive(visible && _shown);
        Subscriber allowedTarget;

        if (visible)
        {
            allowedTarget = _target;
        }
        else
        {
            allowedTarget = null;
        }

        SetAllowedTarget(allowedTarget);

        if (visible == false || _target != null || Time.unscaledTime < _retryAt)
            return;

        _retryAt = Time.unscaledTime + retryDelaySeconds;

        if (CurrentStage == Stage.Hint)
        {
            if (_shown == false)
            {
                _shown = true;
                _overlay.gameObject.SetActive(true);

                if (HintSettings.Enabled)
                {
                    _overlay.ShowButton(_hints.HintButton, "Need a hint?", "Tap the light bulb to highlight a cat you can send next. Use it whenever you need help.", 1, TotalSteps);
                }
                else
                {
                    _overlay.ShowLesson(
                        "Need a hint?",
                        "The light bulb highlights a cat you can send next. Hints are off; you can turn them on in Settings.",
                        1,
                        TotalSteps,
                        _hints.HintButton.transform as RectTransform);
                }
            }

            return;
        }

        if (CurrentStage == Stage.OpenSettings)
        {
            if (_optionsUI.IsVisible)
            {
                CurrentStage = Stage.Restart;
                _overlay.ShowButton(_optionsUI.RestartButton, "Restart this level", "Tap Restart. Then we'll try an order that frees the track.", restartPromptStep, LevelTwoSteps);
            }

            return;
        }

        if (CurrentStage == Stage.Capacity)
        {
            if (_shown == false)
                ShowLesson("Read the track counter", "0/2 means no cats on the track, out of 2 allowed. The number on the right is the limit.", 1, _capacity);

            return;
        }

        if (CurrentStage == Stage.WaitingForDeadlock)
        {
            if (_track.CurrentCount == _track.MaxCount && _sorter.IsTrackDeadlocked(_track.CurrentCount))
            {
                CurrentStage = Stage.FullTrack;
                YandexSdk.SetTutorialOpen(true);
                ShowLesson("The track is full", "2/2: both places are occupied. Another cat can enter only after a cat on the track collects all its yarn and leaves.", capacityLessonStep, _capacity);
            }

            return;
        }

        if (CurrentStage == Stage.Baskets || CurrentStage == Stage.FullTrack || CurrentStage == Stage.Blocked || CurrentStage == Stage.Restart)
            return;

        bool demo;

        if (CurrentStage == Stage.DemoFirst || CurrentStage == Stage.DemoSecond)
        {
            demo = true;
        }
        else
        {
            demo = false;
        }

        Subscriber next;

        if (demo)
        {
            next = FindDemoCat();
        }
        else
        {
            next = _hints.SuggestTutorialCat();
        }

        if (next == null)
            return;

        _target = next;
        _target.Clicks.Clicked += OnTargetClicked;
        SetAllowedTarget(_target);
        string title, body;
        int step;
        switch (CurrentStage)
        {
            case Stage.FirstCat:
                title = "Match the yarn!";
                body = "Each cat needs yarn of its own color. Tap this cat: the path in front of it is clear.";
                step = 1;
                break;
            case Stage.LastCat:
                title = "Help the other cat";
                body = "Now send this cat to collect its own color. Clear every cat to finish the level.";
                step = capacityLessonStep;
                break;
            case Stage.DemoFirst:
                title = "Let's try an example";
                body = "Send this green cat. We'll try filling both places with green cats to see what happens.";
                step = basketLessonStep;
                break;
            case Stage.DemoSecond:
                title = "Fill the second place";
                body = "Send the other green cat too. Both track places will be occupied, leaving the blue cat here.";
                step = basketLessonStep;
                break;
            case Stage.PracticeFirst:
                title = "Try a different order";
                body = "Start with this green cat. This time, keep the second track place for the blue cat.";
                step = wrongOrderStep;
                break;
            case Stage.PracticeBlue:
                title = "Now send the blue cat";
                body = "The green cat needs green yarn to leave the track. Send the blue cat carrying that yarn.";
                step = hintPromptStep;
                break;
            default:
                title = "Send the last cat!";
                body = "A track space is free! Tap the last cat. Clear every cat to complete the level.";
                step = completionStep;
                break;
        }

        _shown = true;
        _overlay.gameObject.SetActive(true);
        _overlay.ShowTarget(_target, title, body, step, TotalSteps);
    }

    private void OnDisable() => Finish();

    public void Initialize(int level, IReadOnlyList<Subscriber> cats, CatHintController hints, Canvas canvas)
    {
        const int firstTutorialLevel = 1;
        const int lastTutorialLevel = 3;
        const int hintTutorialLevel = 3;

        if (level < firstTutorialLevel || level > lastTutorialLevel || cats == null || cats.Count == 0 || hints == null || canvas == null)
            return;

        _level = level;
        _cats = cats;
        _hints = hints;
        _panels = FindObjectsOfType<UIPanel>(true);
        _track = FindObjectOfType<TrackRegistrator>();
        _sorter = FindObjectOfType<SorterRegistrator>();
        _parking = FindObjectOfType<ParkingRegistrator>();
        _autoLoose = FindObjectOfType<AutoLoose>();
        _optionsUI = FindObjectOfType<OptionsUI>();
        _stations = FindObjectsOfType<Station>();
        _capacity = canvas.GetComponent<ResponsiveGameUI>().SafeRoot.Find("CatsCounterPanel/Counter/BgIconPrimary") as RectTransform;
        var baskets = new List<SpriteRenderer>();

        foreach (Station station in _stations)
        {
            if (station.ThemeTarget != null)
                baskets.Add(station.ThemeTarget);
        }

        _baskets = baskets.ToArray();

        foreach (Subscriber cat in cats)
        {
            if (cat != null && cat.Clicks != null)
                _inputBefore[cat.Clicks] = cat.Clicks.InteractionAllowed;
        }

        _overlay = TutorialOverlay.Create(canvas);
        _overlay.InteractionAllowed = () => _running && GameplayVisible();
        _overlay.SkipRequested += Finish;
        _overlay.ContinueRequested += ContinueLesson;
        _overlay.BeforeTargetButtonClick += BeforeTargetButtonClick;
        _running = true;
        _hints.TutorialActive = true;

        if (level == hintTutorialLevel)
        {
            CurrentStage = Stage.Hint;
        }
        else if (level == 1)
        {
            CurrentStage = Stage.FirstCat;
        }
        else if (_resumePractice)
        {
            CurrentStage = Stage.PracticeFirst;
        }
        else
        {
            CurrentStage = Stage.Capacity;
        }

        _resumePractice = false;

        if (_autoLoose != null)
            _autoLoose.TutorialOwnsDeadlock = true;

        SetAllowedTarget(null);
        _overlay.gameObject.SetActive(false);
    }

    public void Finish()
    {
        if (_running == false)
            return;

        _running = false;
        YandexSdk.SetTutorialOpen(false);
        CurrentStage = Stage.Finished;

        if (_target != null)
            _target.Clicks.Clicked -= OnTargetClicked;

        _target = null;

        foreach (var pair in _inputBefore)
        {
            if (pair.Key != null)
                pair.Key.InteractionAllowed = pair.Value;
        }

        if (_hints != null)
            _hints.TutorialActive = false;

        if (_autoLoose != null)
            _autoLoose.TutorialOwnsDeadlock = false;

        if (_overlay != null)
        {
            _overlay.SkipRequested -= Finish;
            _overlay.ContinueRequested -= ContinueLesson;
            _overlay.BeforeTargetButtonClick -= BeforeTargetButtonClick;
            _overlay.gameObject.SetActive(false);
            Destroy(_overlay.gameObject);
        }
    }

    private bool GameplayVisible()
    {
        bool guidingSettings;

        if ((CurrentStage == Stage.OpenSettings || CurrentStage == Stage.Restart) && _optionsUI.IsVisible)
        {
            guidingSettings = true;
        }
        else
        {
            guidingSettings = false;
        }

        bool readingExample;

        if ((CurrentStage == Stage.FullTrack || CurrentStage == Stage.Blocked) && YandexSdk.IsOnlyTutorialPaused)
        {
            readingExample = true;
        }
        else
        {
            readingExample = false;
        }

        if ((YandexSdk.IsPaused || Time.timeScale <= 0) && readingExample == false && (guidingSettings && YandexSdk.IsOnlyOptionsPaused) == false)
            return false;

        foreach (UIPanel panel in _panels)
        {
            if (panel != null && panel.BlocksGameplay && (guidingSettings && panel == _optionsUI) == false)
                return false;
        }

        return true;
    }

    private Subscriber FindParked(CatColor color)
    {
        foreach (Subscriber cat in _cats)
        {
            if (cat != null && cat.gameObject.activeInHierarchy && cat.Color == color && cat.Parking.isActiveAndEnabled && cat.Parking.IsAtBorderCell == false)
                return cat;
        }

        return null;
    }

    private Subscriber FindDemoCat()
    {
        if (_track.IsCountAllows() == false || _parking.HasMovingCars)
            return null;

        foreach (Subscriber cat in _cats)
        {
            if (cat != null && (cat.Sorter.IsSorting || (cat.Parking.enabled && cat.Parking.IsAtBorderCell)))
                return null;
        }

        foreach (Subscriber cat in _cats)
        {
            if (cat != null && cat.Color == CatColor.Green && cat.Parking.isActiveAndEnabled && cat.Parking.IsAtBorderCell == false && cat.Parking.GetBlockingCars().Count == 0)
                return cat;
        }

        return null;
    }

    private void OnTargetClicked()
    {
        const int basketLessonStep = 2;
        const float retryDelaySeconds = 0.2f;

        if (_running == false || _target == null || GameplayVisible() == false)
            return;

        if (_target.Parking.IsAtBorderCell == false && _target.Parking.isActiveAndEnabled)
            return;

        _target.Clicks.Clicked -= OnTargetClicked;
        _target = null;
        _completed++;
        SetAllowedTarget(null);
        _overlay.ClearTarget();
        switch (CurrentStage)
        {
            case Stage.FirstCat:
                CurrentStage = Stage.Baskets;
                ShowLesson("These are the baskets", "Here, cats leave other colors and collect their own. A cat leaves the track once all its yarn matches.", basketLessonStep, null, _baskets);
                break;
            case Stage.DemoFirst:
                CurrentStage = Stage.DemoSecond;
                break;
            case Stage.DemoSecond:
                CurrentStage = Stage.WaitingForDeadlock;

                // Changing the footer before tip 3 is ready would briefly move tip 2.
                break;
            case Stage.PracticeFirst:
                CurrentStage = Stage.PracticeBlue;
                break;
            case Stage.PracticeBlue:
                CurrentStage = Stage.PracticeLast;
                break;
            default:
                Finish();
                break;
        }

        _retryAt = Time.unscaledTime + retryDelaySeconds;
    }

    private void ShowLesson(string title, string body, int step, RectTransform ui = null, SpriteRenderer[] sprites = null, SpriteRenderer[][] groups = null)
    {
        _shown = true;
        _overlay.gameObject.SetActive(true);
        _overlay.ShowLesson(title, body, step, TotalSteps, ui, sprites, groups);
    }

    private void ContinueLesson()
    {
        const int restartLessonStep = 4;
        const int settingsPromptStep = 5;

        if (_running == false || GameplayVisible() == false)
            return;

        switch (CurrentStage)
        {
            case Stage.Hint:
                Finish();
                return;
            case Stage.Capacity:
                CurrentStage = Stage.DemoFirst;
                break;
            case Stage.Baskets:
                CurrentStage = Stage.LastCat;
                break;
            case Stage.FullTrack:
                CurrentStage = Stage.Blocked;
                var cats = new List<SpriteRenderer>();
                var blue = FindParked(CatColor.Blue);

                if (blue != null)
                    cats.Add(blue.CatSprite);

                foreach (Subscriber cat in _cats)
                {
                    if (cat != null && cat.Color == CatColor.Green && cat.IsOnTrack)
                        cats.Add(cat.CatSprite);
                }

                var stations = new List<SpriteRenderer[]>();

                foreach (Station station in _stations)
                {
                    if (station.HavePassengers(out _, out _))
                        stations.Add(station.GetComponentsInChildren<SpriteRenderer>());
                }

                ShowLesson(
                    "Now the track is stuck",
                    "The green cats need green yarn, but the basket holds blue yarn. Their yarn is on the blue cat, which cannot enter the full track. Let's restart.",
                    restartLessonStep,
                    null,
                    cats.ToArray(),
                    stations.ToArray());
                return;
            case Stage.Blocked:
                CurrentStage = Stage.OpenSettings;
                YandexSdk.SetTutorialOpen(false);
                _overlay.ShowButton(_optionsUI.OpenButton, "Open Settings", "Tap the gear. The Restart button is inside Settings.", settingsPromptStep, LevelTwoSteps);
                return;
            default:
                return;
        }

        _overlay.ShowPending("Waiting for track space...");
        _retryAt = 0;
    }

    private void BeforeTargetButtonClick()
    {
        if (_running == false || GameplayVisible() == false)
            return;

        if (CurrentStage == Stage.Restart)
            _resumePractice = true;

        // Release suppression before forwarding the click, or the real hint request is rejected.
        else if (CurrentStage == Stage.Hint)
            Finish();
    }

    private void SetAllowedTarget(Subscriber target)
    {
        foreach (var pair in _inputBefore)
        {
            if (pair.Key != null)
            {
                if (pair.Value && target != null && pair.Key == target.Clicks)
                {
                    pair.Key.InteractionAllowed = true;
                }
                else
                {
                    pair.Key.InteractionAllowed = false;
                }
            }
        }
    }
}
