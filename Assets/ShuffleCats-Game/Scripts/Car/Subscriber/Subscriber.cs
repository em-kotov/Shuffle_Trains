using System.Collections;
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
    private SplineContainer _trackSpline;
    private SplineContainer _exitSpline;
    private PhysicsRaycaster _raycaster;
    private float _trackSpeed = 16f;
    private float _slideDuration = 0.3f;
    private float _searchMin;
    private float _searchMax;
    private float _waitTime;
    private bool _isCarOnBorder = false;

    private void OnEnable()
    {
        _clickDetector.Initialize(_raycaster);
    }

    // private void OnDisable()
    // {
    //     //_bumpDetector.Bumped -= _splineCar.OnBumped;
    //     _bumpDetector.StationFound -= _passengerCar.StopAtStation;
    //     _bumpDetector.ResetFound -= _passengerCar.OnResetFound;

    //     if (_splineCar.enabled)
    //     {
    //         _passengerCar.StopFound -= _splineCar.OnStopFound;
    //         _passengerCar.PassengerReleased -= _splineCar.OnPassengerReleased;
    //         _passengerCar.FinishedSorting -= _splineCar.OnFinishedSorting;

    //         //_splineCar.SplineAnimatePaused -= _bumpDetector.OnSplinePaused;
    //         //_splineCar.SplineAnimateResumed -= _bumpDetector.OnSplineResumed;
    //     }
    // }

    public void Initialize(ParkingRegistrator parkingRegistrator, SplineContainer trackSpline,
                        PhysicsRaycaster raycaster, TrackRegistrator trackRegistrator,
                        float trackSpeed, float slideDuration, float waitTime,
                        SplineContainer exitSpline, float searchMin = 0f, float searchMax = 1f)
    {
        _parkingRegistrator = parkingRegistrator;
        _trackRegistrator = trackRegistrator;
        _trackSpline = trackSpline;
        _exitSpline = exitSpline;
        _raycaster = raycaster;
        _trackSpeed = trackSpeed;
        _slideDuration = slideDuration;
        _waitTime = waitTime;
        _searchMin = searchMin;
        _searchMax = searchMax;

        ActivateCar();
        _mover.Initialize(_car.transform, _slideDuration);
        _bumpHandler.Initialize(_car);
        _passengerCar.Initialize();
    }

    private IEnumerator OnBorderArrivalRoutine()
    {
        if (_isCarOnBorder)
        {
            WaitForSeconds wait = new(_waitTime);
            bool canEnterTrack = _trackRegistrator.IsCountAllows();

            while (canEnterTrack == false)
            {
                yield return wait;
                canEnterTrack = _trackRegistrator.IsCountAllows();
            }

            DeactivateCar();
            ActivateSplineCar();
        }
    }

    private void OnBorderArrival()
    {
        Debug.Log("Subscriber - starting On border arrival coroutine");
        _isCarOnBorder = true;
        StartCoroutine(OnBorderArrivalRoutine());
        _isCarOnBorder = false;
    }

    private void ActivateSplineCar()
    {
        _splineCar.enabled = true;
        _splineCar.Initialize(_mover, _car.transform, _trackSwitcher,
                            _splineAnimate, _trackSpline, _trackSpeed,
                            _searchMin, _searchMax, _trackRegistrator,
                            _waitTime, _exitSpline, _length);

        _splineCar.SplineAnimatePaused += _bumpDetector.OnSplinePaused;
        _splineCar.SplineAnimateResumed += _bumpDetector.OnSplineResumed;

        _bumpDetector.Bumped += _splineCar.OnBumped;

        _bumpDetector.StationFound += _passengerCar.StopAtStation;
        _bumpDetector.ResetFound += _passengerCar.OnResetFound;

        _bumpDetector.LiquidationFound += OnLiquidationFound;

        _passengerCar.StopFound += _splineCar.OnStopFound;
        _passengerCar.PassengerReleased += _splineCar.OnPassengerReleased;
        _passengerCar.FinishedSorting += _splineCar.OnFinishedSorting;

        _bumpDetector.Initialize();

        _splineCar.GoToTrack();
    }

    private void DeactivateSplineCar()
    {
        _splineCar.Deactivate();

        _splineCar.SplineAnimatePaused -= _bumpDetector.OnSplinePaused;
        _splineCar.SplineAnimateResumed -= _bumpDetector.OnSplineResumed;

        _bumpDetector.Bumped -= _splineCar.OnBumped;

        _bumpDetector.StationFound -= _passengerCar.StopAtStation;
        _bumpDetector.ResetFound -= _passengerCar.OnResetFound;

        _passengerCar.StopFound -= _splineCar.OnStopFound;
        _passengerCar.PassengerReleased -= _splineCar.OnPassengerReleased;
        _passengerCar.FinishedSorting -= _splineCar.OnFinishedSorting;
    }

    private void ActivateCar()
    {
        _car.Initialize(_parkingRegistrator, _mover, _orientation, _signDirection, _length);

        _clickDetector.Clicked += _car.OnClick;
        _mover.FinishedMoving += _car.OnFinishedMoving;
        _car.IsOnBorder += OnBorderArrival;
    }

    private void DeactivateCar()
    {
        _clickDetector.Clicked -= _car.OnClick;
        _mover.FinishedMoving -= _car.OnFinishedMoving;
        _car.IsOnBorder -= OnBorderArrival;

        _car.Deactivate();
        _car.enabled = false;
    }

    private void OnLiquidationFound()
    {
        _bumpDetector.LiquidationFound -= OnLiquidationFound;
        DeactivateSplineCar();
        _bumpDetector.Deactivate();
    }
}
