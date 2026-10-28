using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Splines;

public class LevelManager : MonoBehaviour
{
    [Header("Level")]
    [SerializeField] private int _levelNumber;
    [SerializeField] private int _levelWinBonus;
    [SerializeField] private int _levelCarBonus;
    [SerializeField] private WalletUI _walletUI;
    [Tooltip("Shows the current level number in the in-game LevelCounter UI.")]
    [SerializeField] private TMP_Text _levelNumberText;

    [Header("Parking")]
    [SerializeField] private int _parkingGridWidth = 6;
    [SerializeField] private int _parkingGridHeight = 8;
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private int _startX = 0;
    [SerializeField] private int _startY = 0;
    [SerializeField] private float _axisYLevel = 0f;

    [Header("Track")]
    [SerializeField] private int _maxCarsOnTrackCount = 6;
    [SerializeField] private float _waitTime = 0.3f;
    [SerializeField] private int _stationCount;

    [Header("Cars")]
    [SerializeField] private float _trackSpeed = 16f;
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private float _searchMin = 0f;
    [SerializeField] private float _searchMax = 1f;

    [Header("References")]
    [SerializeField] private ParkingRegistrator _parkingRegistrator;
    [SerializeField] private GridCalculator _gridCalculator;
    [SerializeField] private TrackRegistrator _trackRegistrator;
    [SerializeField] private SorterRegistrator _sorterRegistrator;
    [SerializeField] private Counter _counterUI;
    [SerializeField] private SplineContainer _trackSpline;
    [SerializeField] private SplineContainer _exitSpline;
    [SerializeField] private PhysicsRaycaster _raycaster;
    [SerializeField] private Passenger _passengerPrefab;
    [SerializeField] private AutoLoose _autoLoose;
    [SerializeField] private AutoWin _autoWin;
    [SerializeField] private Shop _shop;
    [SerializeField] private ThemeApplier _themeApplier;
    [SerializeField] private List<Station> _stations;
    [SerializeField] private List<Subscriber> _carSubscribers;

    [Header("Config-Driven (optional)")]
    [Tooltip("Shared base: grid, track, movement, and the full station list.")]
    [SerializeField] private LevelConfig _levelConfig;
    [Tooltip("Ordered levels for progression. Used when no manual override below is set.")]
    [SerializeField] private LevelSequence _levelSequence;
    [Tooltip("Manual override: forces this layout, ignoring the sequence/progress. " +
            "Leave empty for the real game; assign one to test a specific level.")]
    [SerializeField] private LevelLayout _levelLayout;
    [SerializeField] private LevelBuilder _levelBuilder;

    private Wallet _wallet;

    private bool HasManualLayout
    {
        get
        {
#if UNITY_EDITOR
            if (LevelProgress.EditorPreviewIndex >= 0)
                return false;

#endif
            return _levelLayout != null;
        }
    }

    private void Awake()
    {
        const int firstTutorialLevel = 1;
        const int lastTutorialLevel = 3;

        // Register before filtering the station list so inactive stations also receive the theme.
        if (_themeApplier != null)
        {
            _themeApplier.RegisterStations(_stations);
        }

        if (_levelConfig != null)
        {
            ApplyConfig(_levelConfig, ResolveLayout());
        }

        if (_levelNumberText != null)
        {
            _levelNumberText.text = _levelNumber.ToString();
            ResponsiveGameUI.Attach(_levelNumberText.canvas.rootCanvas);
            LeaderboardUI.Attach(_levelNumberText.canvas.rootCanvas);
        }

        _parkingRegistrator.Initialize(
            _parkingGridWidth,
            _parkingGridHeight,
            _cellSize,
            _startX,
            _startY,
            _axisYLevel,
            _gridCalculator,
            _carSubscribers.Count);
        _trackRegistrator.Initialize(_trackSpline, _counterUI, _maxCarsOnTrackCount);
        _sorterRegistrator.Initialize(_stations);

        foreach (Subscriber subscriber in _carSubscribers)
        {
            subscriber.Initialize(
                _parkingRegistrator,
                _trackSpline,
                _raycaster,
                _trackRegistrator,
                _trackSpeed,
                _slideDuration,
                _waitTime,
                _exitSpline,
                _passengerPrefab,
                _stationCount,
                _sorterRegistrator,
                _searchMin,
                _searchMax);
            subscriber.ReachedEnd += OnCarReachedEnd;
        }

        foreach (Station station in _stations)
        {
            station.Initialize();
        }

        if (_levelNumberText != null)
        {
            CatHintController hints = gameObject.AddComponent<CatHintController>();
            hints.Initialize(
                _carSubscribers,
                _stations,
                _parkingRegistrator,
                _trackRegistrator,
                _levelNumberText.canvas.rootCanvas);

            if (_levelNumber >= firstTutorialLevel && _levelNumber <= lastTutorialLevel)
            {
                gameObject.AddComponent<TutorialController>().Initialize(
                    _levelNumber,
                    _carSubscribers,
                    hints,
                    _levelNumberText.canvas.rootCanvas);
            }
        }

        _autoLoose.Initialize(_parkingRegistrator, _sorterRegistrator, _trackRegistrator);
        _autoWin.Initialize(_carSubscribers.Count, _levelNumber, IsLastLevel());

        _wallet = FindObjectOfType<Wallet>();

        if (_wallet != null)
        {
            _wallet.Initialize(_levelWinBonus, _levelCarBonus, _walletUI);
        }

        if (_shop != null)
            _shop.Initialize(_wallet);

        _autoWin.WinLevel += OnWinLevel;

        if (_themeApplier != null)
        {
            _themeApplier.Initialize(_shop);
        }
    }

    private bool IsLastLevel()
    {
        if (HasManualLayout)
            return false;

        if (_levelSequence == null || _levelSequence.Count == 0)
            return false;

        return LevelProgress.CurrentIndex >= _levelSequence.Count - 1;
    }

    private LevelLayout ResolveLayout()
    {
        if (HasManualLayout)
            return _levelLayout;

        if (_levelSequence != null && _levelSequence.Count > 0)
        {
            int index = Mathf.Clamp(LevelProgress.CurrentIndex, 0, _levelSequence.Count - 1);
            return _levelSequence.Get(index);
        }

        return null;
    }

    private void ApplyConfig(LevelConfig baseConfig, LevelLayout layout)
    {
        _parkingGridWidth = baseConfig.GridWidth;
        _parkingGridHeight = baseConfig.GridHeight;
        _cellSize = baseConfig.CellSize;
        _startX = baseConfig.StartX;
        _startY = baseConfig.StartY;
        _axisYLevel = baseConfig.AxisYLevel;

        _maxCarsOnTrackCount = baseConfig.MaxCarsOnTrackCount;
        _waitTime = baseConfig.WaitTime;

        _trackSpeed = baseConfig.TrackSpeed;
        _slideDuration = baseConfig.SlideDuration;
        _searchMin = baseConfig.SearchMin;
        _searchMax = baseConfig.SearchMax;

        if (layout != null)
        {
            _levelNumber = layout.LevelNumber;
            _levelWinBonus = layout.LevelWinBonus;
            _levelCarBonus = layout.LevelCarBonus;

            if (layout.MaxCarsOnTrackCount > 0)
                _maxCarsOnTrackCount = layout.MaxCarsOnTrackCount;
        }
        else
        {
            _levelNumber = baseConfig.LevelNumber;
            _levelWinBonus = baseConfig.LevelWinBonus;
            _levelCarBonus = baseConfig.LevelCarBonus;
        }

        if (_levelBuilder == null)
        {
            Debug.LogError("LevelManager - level config assigned but no LevelBuilder");
            return;
        }

        ApplyStationMask(layout);
        _stationCount = _stations.Count;

        DisableAuthoringCats();

        IReadOnlyList<CatPlacement> cats;

        if (layout != null && layout.Cats.Count > 0)
        {
            cats = layout.Cats;
        }
        else
        {
            cats = baseConfig.Cats;
        }

        _carSubscribers = _levelBuilder.BuildCats(baseConfig, cats);
    }

    private void DisableAuthoringCats()
    {
        if (_carSubscribers == null)
            return;

        foreach (Subscriber cat in _carSubscribers)
        {
            if (cat != null)
                cat.gameObject.SetActive(false);
        }
    }

    private void ApplyStationMask(LevelLayout layout)
    {
        List<Station> sceneStations = _stations;
        List<Station> active = new();
        bool hasMask;

        if (layout != null && layout.ActiveStations.Count > 0)
        {
            hasMask = true;
        }
        else
        {
            hasMask = false;
        }

        for (int i = 0; i < sceneStations.Count; i++)
        {
            Station station = sceneStations[i];

            if (station == null)
                continue;

            bool on;

            if (hasMask == false || (i < layout.ActiveStations.Count && layout.ActiveStations[i]))
            {
                on = true;
            }
            else
            {
                on = false;
            }

            station.gameObject.SetActive(on);

            if (on)
                active.Add(station);
        }

        _stations = active;
    }

    private void OnCarReachedEnd(Subscriber carSubscriber)
    {
        carSubscriber.ReachedEnd -= OnCarReachedEnd;

        if (_wallet != null)
        {
            _wallet.ReceiveCarBonus();
        }

        _autoWin.AddCar();
    }

    private void OnWinLevel()
    {
        _autoWin.WinLevel -= OnWinLevel;

        if (HasManualLayout == false)
            LevelProgress.RecordCompletion(_levelNumber);

        if (_wallet != null)
        {
            _wallet.ReceiveWinBonus();
        }
    }
}
