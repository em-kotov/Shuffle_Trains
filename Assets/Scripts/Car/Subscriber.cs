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
    [SerializeField] private Car _car;
    [SerializeField] private Mover _mover;
    [SerializeField] private SplineCar _splineCar;
    [SerializeField] private TrackSwitcher _trackSwitcher;
    [SerializeField] private SplineAnimate _splineAnimate;
    [SerializeField] private BumpHandler _bumpHandler;
    [SerializeField] private List<ClickDetector> _clickDetectors;

    private ParkingRegistrator _parkingRegistrator;
    private SplineContainer _origSpline;
    private PhysicsRaycaster _raycaster;
    private float _trackSpeed = 16f;
    private float _slideDuration = 0.3f;
    private float _searchMin;
    private float _searchMax;

    private void OnEnable()
    {
        SubscribeClickDetectors();
        _mover.FinishedMoving += _car.OnFinishedMoving;

        if (_splineCar != null)
        {
            _car.IsOnBorder += OnEnterTrack;
            _car.IsOnBorder += _splineCar.OnBorder;
        }
    }

    private void OnDisable()
    {
        UnsubscribeClickDetectors();
        _mover.FinishedMoving -= _car.OnFinishedMoving;

        if (_splineCar != null)
        {
            _car.IsOnBorder -= _splineCar.OnBorder;
            _car.IsOnBorder -= OnEnterTrack;
        }
    }

    public void Initialize(ParkingRegistrator parkingRegistrator, SplineContainer origSpline,
                        PhysicsRaycaster raycaster, float trackSpeed, float slideDuration,
                        float searchMin = 0f, float searchMax = 1f)
    {
        _parkingRegistrator = parkingRegistrator;
        _origSpline = origSpline;
        _raycaster = raycaster;
        _trackSpeed = trackSpeed;
        _slideDuration = slideDuration;
        _searchMin = searchMin;
        _searchMax = searchMax;

        _car.Initialize(_parkingRegistrator, _mover, _orientation, _signDirection, _length);
        _mover.Initialize(_car.transform, _slideDuration);
        _bumpHandler.Initialize(_car);
    }

    private void SubscribeClickDetectors()
    {
        foreach (ClickDetector clickDetector in _clickDetectors)
        {
            clickDetector.Initialize(_raycaster);
            clickDetector.Clicked += _car.OnClick;
        }
    }

    private void UnsubscribeClickDetectors()
    {
        foreach (ClickDetector clickDetector in _clickDetectors)
        {
            clickDetector.Clicked -= _car.OnClick;
        }
    }

    private void OnEnterTrack()
    {
        _car.enabled = false;
        _splineCar.enabled = true;
        _splineCar.Initialize(_mover, _car.transform, _trackSwitcher, _splineAnimate,
                            _origSpline, _trackSpeed, _searchMin, _searchMax);
    }
}
