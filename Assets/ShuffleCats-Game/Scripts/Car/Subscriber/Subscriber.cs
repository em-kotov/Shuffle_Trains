using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Splines;

public class Subscriber : MonoBehaviour
{
    [SerializeField] private CarOrientation _orientation = CarOrientation.Horizontal;
    [SerializeField] private float _signDirection = 1;
    [SerializeField] private int _length;

    [Header("References")]
    [SerializeField] private Car _car;
    [SerializeField] private Mover _mover;
    [SerializeField] private SplineCar _splineCar;
    [SerializeField] private PassengerCar _passengerCar;
    [SerializeField] private TrackSwitcher _trackSwitcher;
    [SerializeField] private SplineAnimate _splineAnimate;
    [SerializeField] private BumpHandler _bumpHandler;
    [SerializeField] private ClickDetector _clickDetector;
    [SerializeField] private BumpDetector _bumpDetector;

    private ParkingRegistrator _parkingRegistrator;
    private TrackRegistrator _trackRegistrator;
    private SplineContainer _origSpline;
    private PhysicsRaycaster _raycaster;
    private float _trackSpeed = 16f;
    private float _slideDuration = 0.3f;
    private float _searchMin;
    private float _searchMax;
    private float _waitTime;

    private void OnEnable()
    {
        _clickDetector.Initialize(_raycaster);
        _clickDetector.Clicked += _car.OnClick;

        _mover.FinishedMoving += _car.OnFinishedMoving;

        if (_splineCar != null)
        {
            _car.IsOnBorder += OnEnterTrack;
            _car.IsOnBorder += _splineCar.OnBorder;
        }

        if (_splineCar.enabled)
        {
            _passengerCar.StopFound += _splineCar.OnStopFound;
            _passengerCar.PassengerReleased += _splineCar.OnPassengerReleased;

            _splineCar.SplineAnimatePaused += _bumpDetector.OnSplinePaused;
            _splineCar.SplineAnimateResumed += _bumpDetector.OnSplineResumed;
        }
    }

    private void OnDisable()
    {
        _clickDetector.Clicked -= _car.OnClick;

        _bumpDetector.Bumped -= _splineCar.OnBumped;
        _bumpDetector.StationFound -= _passengerCar.StopAtStation;

        _mover.FinishedMoving -= _car.OnFinishedMoving;

        if (_splineCar != null)
        {
            _car.IsOnBorder -= _splineCar.OnBorder;
            _car.IsOnBorder -= OnEnterTrack;
        }

        if (_splineCar.enabled)
        {
            _passengerCar.StopFound -= _splineCar.OnStopFound;
            _passengerCar.PassengerReleased -= _splineCar.OnPassengerReleased;

            _splineCar.SplineAnimatePaused -= _bumpDetector.OnSplinePaused;
            _splineCar.SplineAnimateResumed -= _bumpDetector.OnSplineResumed;
        }
    }

    public void Initialize(ParkingRegistrator parkingRegistrator, SplineContainer origSpline,
                        PhysicsRaycaster raycaster, TrackRegistrator trackRegistrator,
                        float trackSpeed, float slideDuration, float waitTime,
                        float searchMin = 0f, float searchMax = 1f)
    {
        _parkingRegistrator = parkingRegistrator;
        _trackRegistrator = trackRegistrator;
        _origSpline = origSpline;
        _raycaster = raycaster;
        _trackSpeed = trackSpeed;
        _slideDuration = slideDuration;
        _waitTime = waitTime;
        _searchMin = searchMin;
        _searchMax = searchMax;

        _car.Initialize(_parkingRegistrator, _mover, _orientation, _signDirection, _length);
        _mover.Initialize(_car.transform, _slideDuration);
        _bumpHandler.Initialize(_car);
    }

    private void OnEnterTrack()
    {
        _car.enabled = false;
        _splineCar.enabled = true;
        _splineCar.Initialize(_mover, _car.transform, _trackSwitcher, _splineAnimate,
                            _origSpline, _trackSpeed, _searchMin, _searchMax, _trackRegistrator,
                            _waitTime, _length);

        _bumpDetector.Initialize();
        _bumpDetector.Bumped += _splineCar.OnBumped;
        _bumpDetector.StationFound += _passengerCar.StopAtStation;
    }
}
