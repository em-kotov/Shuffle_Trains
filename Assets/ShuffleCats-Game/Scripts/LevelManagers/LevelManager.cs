using System.Collections;
using System.Collections.Generic;
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
    //[SerializeField] private Wallet _wallet;
    [SerializeField] private List<Station> _stations;
    [SerializeField] private List<Subscriber> _carSubscribers;

    private Wallet _wallet;

    private void Awake()
    {
        _parkingRegistrator.Initialize(_parkingGridWidth, _parkingGridHeight,
                                        _cellSize, _startX, _startY, _axisYLevel,
                                        _gridCalculator, _carSubscribers.Count);
        _trackRegistrator.Initialize(_trackSpline, _counterUI, _maxCarsOnTrackCount);
        _sorterRegistrator.Initialize(_stations);

        foreach (Subscriber subscriber in _carSubscribers)
        {
            subscriber.Initialize(_parkingRegistrator, _trackSpline, _raycaster,
                                    _trackRegistrator, _trackSpeed, _slideDuration,
                                    _waitTime, _exitSpline, _passengerPrefab,
                                    _stationCount, _sorterRegistrator, _searchMin, _searchMax);
            subscriber.ReachedEnd += OnCarReachedEnd;
        }

        foreach (Station station in _stations)
        {
            station.Initialize();
        }

        _autoLoose.Initialize(_parkingRegistrator, _sorterRegistrator);
        _autoWin.Initialize(_carSubscribers.Count, _levelNumber);

        _wallet = FindObjectOfType<Wallet>();

        if (_wallet != null)
        {
            _wallet.Initialize(_levelWinBonus, _levelCarBonus, _walletUI);
        }

        //_autoWin.WinLevel += OnWinLevel;
    }

    private void OnCarReachedEnd(Subscriber carSubscriber)
    {
        carSubscriber.ReachedEnd -= OnCarReachedEnd;
        _autoWin.AddCar();

        if (_wallet != null)
        {
            _wallet.ReceiveCarBonus();
        }
    }

    // private void OnWinLevel()
    // {
    //     _autoWin.WinLevel -= OnWinLevel;
    //     StartCoroutine(ShowWin());
    // }

    // private IEnumerator ShowWin()
    // {
    //     WaitForSeconds wait = new WaitForSeconds(2f);

    //     yield return wait;

    //     // LevelLoader.Instance.LoadNextLevel();
    // }
}
