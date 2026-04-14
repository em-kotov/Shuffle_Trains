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
    [SerializeField] private ParkingCar _parkingCar;
    [SerializeField] private Transform _carHead;
    [SerializeField] private Mover _mover;
    [SerializeField] private SplineCar _splineCar;
    [SerializeField] private PassengerCar _passengerCar;
    [SerializeField] private TrackSwitcher _trackSwitcher;
    [SerializeField] private SplineAnimate _splineAnimate;
    [SerializeField] private ClickDetector _clickDetector;
    [SerializeField] private Scanner _scanner;
    [SerializeField] private SplineOperator _splineOperator;

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

        ActivateParkingCar();
        _mover.Initialize(_carHead, _slideDuration);
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

            DeactivateParkingCar();
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
        _splineCar.Initialize(_mover, _carHead, _trackSwitcher,
                            _splineAnimate, _trackSpline, _trackSpeed,
                            _searchMin, _searchMax, _trackRegistrator,
                            _waitTime, _exitSpline, _splineOperator, _length);

        _scanner.BumpFound += OnBumpFound;

        _scanner.StationFound += OnStationFound;
        _scanner.ResetFound += OnResetFound;

        _scanner.LiquidationFound += OnLiquidationFound;

        _scanner.Initialize();

        _splineCar.GoToTrack();
    }

    private void DeactivateSplineCar()
    {
        _splineCar.Stop();
        _splineCar.SetNormalizedTime(0.999f);

        _scanner.BumpFound -= OnBumpFound;

        _scanner.StationFound -= OnStationFound;
        _scanner.ResetFound -= OnResetFound;
    }

    private void ActivateParkingCar()
    {
        _parkingCar.Initialize(_parkingRegistrator, _mover, _orientation, _signDirection, _length);

        _clickDetector.Clicked += _parkingCar.OnClick;
        _mover.FinishedMoving += _parkingCar.OnFinishedMoving;
        _parkingCar.IsOnBorder += OnBorderArrival;
    }

    private void DeactivateParkingCar()
    {
        _clickDetector.Clicked -= _parkingCar.OnClick;
        _mover.FinishedMoving -= _parkingCar.OnFinishedMoving;
        _parkingCar.IsOnBorder -= OnBorderArrival;

        _parkingCar.Deactivate();
        _parkingCar.enabled = false;
    }

    private void OnLiquidationFound()
    {
        Debug.Log("Subscriber starting liquidation");
        _scanner.LiquidationFound -= OnLiquidationFound;
        _scanner.StopScanning();
        DeactivateSplineCar();
        _carHead.gameObject.GetComponent<Collider>().enabled = false;
    }

    private void OnBumpFound()
    {
        _splineCar.Jump();
    }

    private void OnStationFound(Station station)
    {
        if (_passengerCar.IsFinished)
            return;

        _passengerCar.TryStopAtStation(station);

        if (_passengerCar.IsStopping)
        {
            _splineCar.Stop();
            _scanner.StopScanning();
            StartCoroutine(WaitFinishSorting());
        }
    }

    private IEnumerator WaitFinishSorting()
    {
        yield return new WaitUntil(() => _passengerCar.IsSorting == false);

        if (_passengerCar.IsFinished)
        {
            _splineCar.SwitchSplineToExit();
        }

        _splineCar.Play();
        _scanner.StartScanning();
    }

    private void OnResetFound()
    {
        if (_passengerCar.IsFinished)
            return;

        _passengerCar.TryResetStationProgress();
    }
}
