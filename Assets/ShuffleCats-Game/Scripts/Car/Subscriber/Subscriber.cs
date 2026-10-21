using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Splines;

public class Subscriber : MonoBehaviour
{
    [SerializeField] private CarOrientation _orientation = CarOrientation.Horizontal;
    [SerializeField] private float _signDirection = 1;
    [SerializeField] private int _length;

    [Header("References")]
    [SerializeField] private ParkingCar _parkingCar;
    [SerializeField] private Transform _carHead;
    [SerializeField] private Mover _mover;
    [SerializeField] private SplineCar _splineCar;
    [SerializeField] private SorterCar _sorterCar;
    [SerializeField] private TrackSwitcher _trackSwitcher;
    [SerializeField] private SplineAnimate _splineAnimate;
    [SerializeField] private ClickDetector _clickDetector;
    [SerializeField] private Scanner _scanner;
    [SerializeField] private SplineOperator _splineOperator;
    [SerializeField] private StationOperator _stationOperator;
    [SerializeField] private PassengerTracker _passengerTracker;
    [SerializeField] private SpriteRenderer _catTexture;
    [SerializeField] private ScaleAnimation _scaleAnimation;
    [SerializeField] private BumpAnimation _bumpAnimation;
    [SerializeField] private NudgeAnimation _nudgeAnimation;
    [SerializeField] private RotationFixer _rotationFixer;
    [SerializeField] private SpriteFacingLock _spriteFacingLock;

    [Header("Passengers Setup")]
    [SerializeField] private int _totalPassengersCount;
    [SerializeField] private CatColor _carColor;
    [SerializeField] private List<CatColor> _colors;
    [SerializeField] private List<Transform> _holdPoints;

    private ParkingRegistrator _parkingRegistrator;
    private TrackRegistrator _trackRegistrator;
    private SorterRegistrator _sorterRegistrator;
    private SplineContainer _trackSpline;
    private SplineContainer _exitSpline;
    private PhysicsRaycaster _raycaster;
    private float _trackSpeed = 16f;
    private float _slideDuration = 0.3f;
    private float _searchMin;
    private float _searchMax;
    private float _waitTime;
    private bool _isWaitingForTrack = false;
    private List<Passenger> _passengers;
    private CatRunningTrail _runningTrail;
    private CatTrackIndicator _trackIndicator;

    public event Action<Subscriber> ReachedEnd;

    public ParkingCar Parking => _parkingCar;
    public SorterCar Sorter => _sorterCar;
    public SpriteRenderer CatSprite => _catTexture;
    public CatColor Color => _carColor;
    public int SeatCount => _holdPoints.Count;
    public PassengerTracker Passengers => _passengerTracker;
    public ClickDetector Clicks => _clickDetector;
    public bool IsOnTrack { get; private set; }

    private void OnEnable()
    {
        _clickDetector.Initialize(_raycaster);
    }

    // Applies the per-level placement data that varies between instances of the
    // same cat prefab. Body color and length come from the prefab itself.
    public void Configure(CarOrientation orientation, float signDirection,
                        bool invertFlip, bool trackFlipSeparate, bool invertFlipTrack,
                        bool applyRotationFix, bool rotateFixLeft,
                        List<CatColor> passengers)
    {
        _orientation = orientation;
        _signDirection = signDirection;
        _colors = passengers != null ? new List<CatColor>(passengers) : new List<CatColor>();
        _totalPassengersCount = _colors.Count;

        // Prefer the manually-wired reference (lets the lock live on the root);
        // fall back to the legacy lookup on the sprite for older prefabs.
        SpriteFacingLock facingLock = _spriteFacingLock != null
            ? _spriteFacingLock
            : (_catTexture != null ? _catTexture.GetComponent<SpriteFacingLock>() : null);

        if (facingLock != null)
        {
            _spriteFacingLock = facingLock;
            facingLock.SetInvertFlip(invertFlip);
            facingLock.SetTrackFlip(trackFlipSeparate, invertFlipTrack);
            if (_length == 1)
            {
                Vector3 direction = orientation == CarOrientation.Horizontal
                    ? Vector3.right * signDirection
                    : Vector3.forward * signDirection;
                // The green single-cell texture is drawn facing right; all
                // other single-cell textures face left.
                facingLock.SetParkingDirection(direction, _carColor == CatColor.Green);
            }
        }

        if (_rotationFixer != null)
            _rotationFixer.SetConfig(applyRotationFix, rotateFixLeft);
    }

    public void Initialize(ParkingRegistrator parkingRegistrator, SplineContainer trackSpline,
                        PhysicsRaycaster raycaster, TrackRegistrator trackRegistrator,
                        float trackSpeed, float slideDuration, float waitTime,
                        SplineContainer exitSpline, Passenger passengerPrefab,
                        int stationCount, SorterRegistrator sorterRegistrator,
                        float searchMin = 0f, float searchMax = 1f)
    {
        _parkingRegistrator = parkingRegistrator;
        _trackRegistrator = trackRegistrator;
        _sorterRegistrator = sorterRegistrator;
        _trackSpline = trackSpline;
        _exitSpline = exitSpline;
        _raycaster = raycaster;
        _trackSpeed = trackSpeed;
        _slideDuration = slideDuration;
        _waitTime = waitTime;
        _searchMin = searchMin;
        _searchMax = searchMax;
        IsOnTrack = false;

        ActivateParkingCar();
        _mover.Initialize(_carHead, _slideDuration);
        List<Passenger> pass = CreatePassengers(passengerPrefab);
        _sorterCar.Initialize(_stationOperator, _passengerTracker, pass, _holdPoints, _carColor, stationCount, sorterRegistrator);

        _scaleAnimation.Activate(_catTexture.transform);
        _runningTrail = GetComponent<CatRunningTrail>();
        if (_runningTrail == null)
            _runningTrail = gameObject.AddComponent<CatRunningTrail>();
        Vector3 parkingDirection = _orientation == CarOrientation.Horizontal
            ? Vector3.right * _signDirection : Vector3.forward * _signDirection;
        _runningTrail.Initialize(_catTexture, _carHead, _carColor, parkingDirection);
        _trackIndicator = GetComponent<CatTrackIndicator>();
        if (_trackIndicator == null)
            _trackIndicator = gameObject.AddComponent<CatTrackIndicator>();
        // Normalized face locations in the source art. Long cats have their face
        // near the top; the green short cat is the one drawn facing right.
        Vector2 faceAnchor = _length > 1 ? new Vector2(0.53f, 0.68f) :
            new Vector2(_carColor == CatColor.Green ? 0.65f : 0.38f, 0.55f);
        _trackIndicator.Initialize(_catTexture, faceAnchor);
    }

    private IEnumerator OnBorderArrivalRoutine()
    {
        WaitForSeconds wait = new(_waitTime);
        bool canEnterTrack = _trackRegistrator.IsCountAllows();

        while (canEnterTrack == false)
        {
            yield return wait;
            canEnterTrack = _trackRegistrator.IsCountAllows();
        }

        // Reserve the slot synchronously with the passing check above — there is
        // no yield between IsCountAllows() and Reserve(), so concurrent border
        // arrivals can't all slip past a stale count and overflow the limit.
        _trackRegistrator.Reserve();
        DeactivateParkingCar();
        ActivateSplineCar();
    }

    private void OnBorderArrival()
    {
        // Guard re-clicks while already queued for the track: a second routine
        // would double-reserve the slot and double-activate the spline car.
        if (_isWaitingForTrack)
            return;

        _isWaitingForTrack = true;
        _trackIndicator?.ShowWaiting();
        StartCoroutine(OnBorderArrivalRoutine());
    }

    private void ActivateSplineCar()
    {
        _splineCar.enabled = true;
        _splineCar.Initialize(_mover, _carHead, _trackSwitcher,
                            _splineAnimate, _trackSpline, _trackSpeed,
                            _searchMin, _searchMax, _trackRegistrator,
                            _exitSpline, _splineOperator, _parkingRegistrator);

        _scanner.BlockedAhead += OnBlockedAhead;
        _scanner.PathCleared += OnPathCleared;
        _scanner.StationFound += OnStationFound;
        _scanner.ResetFound += OnResetFound;
        _splineCar.TrackEntered += OnTrackEntered;
        _splineCar.TrackEntryStarted += OnTrackEntryStarted;

        _runningTrail?.Begin();
        _splineCar.GoToTrack();
    }

    private void OnTrackEntered()
    {
        IsOnTrack = true;
        _splineCar.TrackEntered -= OnTrackEntered;
        _splineCar.TrackEntryStarted -= OnTrackEntryStarted;
        _trackIndicator?.Hide();
        _sorterRegistrator.MarkOnTrack(_sorterCar);
        _rotationFixer.ApplyFix();

        if (_spriteFacingLock != null)
            _spriteFacingLock.SwitchToTrackFlip();

        _scanner.Initialize();
    }

    private void OnTrackEntryStarted()
    {
        _trackIndicator?.ShowReady();
    }

    private void OnParkingDepartureStarted()
    {
        // A repeated click at a full track must keep the waiting dots visible.
        if (!_isWaitingForTrack)
            _trackIndicator?.ShowReady();
    }

    private void ActivateParkingCar()
    {
        _parkingCar.Initialize(_parkingRegistrator, _mover, _orientation, _signDirection, _length);

        _clickDetector.Pressed += _parkingCar.OnPress;
        _clickDetector.Clicked += _parkingCar.OnClick;
        _mover.FinishedMoving += _parkingCar.OnFinishedMoving;
        _parkingCar.IsOnBorder += OnBorderArrival;
        _parkingCar.DepartureStarted += OnParkingDepartureStarted;
        _parkingCar.BumpReceived += OnBumpReceived;
        _parkingCar.BumpAttempted += OnBumpAttempted;
        _bumpAnimation.Finished += OnBumpFinished;
        _nudgeAnimation.Finished += OnNudgeFinished;
    }

    private void DeactivateParkingCar()
    {
        _clickDetector.Pressed -= _parkingCar.OnPress;
        _clickDetector.Clicked -= _parkingCar.OnClick;
        _mover.FinishedMoving -= _parkingCar.OnFinishedMoving;
        _parkingCar.IsOnBorder -= OnBorderArrival;
        _parkingCar.DepartureStarted -= OnParkingDepartureStarted;
        _parkingCar.BumpReceived -= OnBumpReceived;
        _parkingCar.BumpAttempted -= OnBumpAttempted;
        _bumpAnimation.Finished -= OnBumpFinished;
        _nudgeAnimation.Finished -= OnNudgeFinished;

        _parkingCar.Deactivate();
        _parkingCar.enabled = false;
    }

    private void OnBumpReceived(Vector2 impactDir)
    {
        _trackIndicator?.ShowBump();
        _scaleAnimation.Deactivate();
        _bumpAnimation.Activate(_catTexture.transform, impactDir);
    }

    private void OnBumpFinished()
    {
        _scaleAnimation.Activate(_catTexture.transform);
    }

    private void OnBumpAttempted(Vector2 impactDir)
    {
        _trackIndicator?.ShowBump();
        _scaleAnimation.Deactivate();
        _nudgeAnimation.Activate(_catTexture.transform, impactDir);
    }

    private void OnNudgeFinished()
    {
        _scaleAnimation.Activate(_catTexture.transform);
    }

    private void OnBlockedAhead()
    {
        _splineCar.Stop();
    }

    private void OnPathCleared()
    {
        _splineCar.Play();
    }

    private void OnResetFound()
    {
        if (_sorterCar.IsFinished)
            return;

        //Debug.Log("Subscriber - on reset found");
        _sorterCar.TryResetStationProgress();
    }

    private void OnStationFound(Station station)
    {
        if (_sorterCar.IsFinished)
            return;

        if (_sorterCar.IsNeedToSort(station) == false)
            return;

        _splineCar.Stop();
        _scanner.StopScanning();

        _sorterCar.Sort(station);

        StartCoroutine(WaitFinishSorting());
    }

    private IEnumerator WaitFinishSorting()
    {
        yield return new WaitUntil(() => _sorterCar.IsSorting == false);

        if (_sorterCar.IsFinished)
        {
            _trackIndicator?.ShowExitReaction();
            _splineCar.SwitchSplineToNearest();
            yield return new WaitUntil(() => _splineCar.IsOnExit == true);
            StartCoroutine(WaitCompletedExit());
        }

        _splineCar.Play();
        _scanner.StartScanning();
    }

    private IEnumerator WaitCompletedExit()
    {
        yield return new WaitUntil(() => _splineCar.IsReachedEnd() == true);

        _scanner.StopScanning();
        _scanner.BlockedAhead -= OnBlockedAhead;
        _scanner.PathCleared -= OnPathCleared;
        _scanner.StationFound -= OnStationFound;
        _scanner.ResetFound -= OnResetFound;

        _splineCar.Unregister();
        _splineCar.Stop();
        _carHead.gameObject.GetComponent<Collider>().enabled = false;

        _scaleAnimation.Deactivate();

        // The exit endpoint can be visible in wide or zoomed-out viewports.
        // Stop rendering the whole completed cat, including its passengers and
        // running trail, instead of relying on the endpoint being off camera.
        gameObject.SetActive(false);
        ReachedEnd?.Invoke(this);
    }

    private List<Passenger> CreatePassengers(Passenger passengerPrefab)
    {
        _passengers = new();

        if (_colors.Count < _totalPassengersCount)
        {
            Debug.LogWarning("Subscriber - missing colors for passengers");
        }

        for (int i = 0; i < _totalPassengersCount; i++)
        {
            Passenger passenger = Instantiate(passengerPrefab, transform.position, Quaternion.identity);
            passenger.AssignColor(_colors[i]);
            _passengers.Add(passenger);
        }

        return _passengers;
    }

    private void OnDrawGizmos()
    {
        if (_holdPoints.Count > 0)
        {
            for (int i = 0; i < _holdPoints.Count; i++)
            {
                Gizmos.color = ColorHelper.GetGizmoColor(_carColor);
                Gizmos.DrawWireSphere(_holdPoints[i].position, 0.15f);
            }
        }
    }
}
