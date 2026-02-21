using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Splines;

public class Subscriber : MonoBehaviour
{
    [SerializeField] private Car _car;
    [SerializeField] private Mover _mover;
    [SerializeField] private TrackEnter _trackEnter;
    [SerializeField] private TrackSwitcher _trackSwitcher;
    [SerializeField] private SplineAnimate _splineAnimate;
    [SerializeField] private BumpHandler _bumpHandler;
    [SerializeField] private List<ClickDetector> _clickDetectors;

    private ParkingSlotsHandler _parkingHandler;
    private SplineContainer _origSpline;
    private PhysicsRaycaster _raycaster;

    private void OnEnable()
    {
        SubscribeClickDetectors();
        _mover.FinishedMoving += _car.OnFinishedMoving;

        if (_trackEnter != null)
        {
            _car.IsOnBorder += OnEnterTrack;
            _car.IsOnBorder += _trackEnter.OnBorder;
        }
    }

    private void OnDisable()
    {
        UnsubscribeClickDetectors();
        _mover.FinishedMoving -= _car.OnFinishedMoving;

        if (_trackEnter != null)
        {
            _car.IsOnBorder -= _trackEnter.OnBorder;
            _car.IsOnBorder -= OnEnterTrack;
        }
    }

    public void Initialize(ParkingSlotsHandler parkingHandler, SplineContainer origSpline, PhysicsRaycaster raycaster)
    {
        _parkingHandler = parkingHandler;
        _origSpline = origSpline;
        _raycaster = raycaster;

        _car.Initialize(_parkingHandler, _mover);
        _mover.Initialize(_car.transform);
        _bumpHandler.Initialize(_car);
        _trackSwitcher.Initialize(_origSpline);
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
        _trackEnter.enabled = true;
        _trackEnter.Initialize(_mover, _car.transform, _trackSwitcher, _splineAnimate, _origSpline);
    }
}
