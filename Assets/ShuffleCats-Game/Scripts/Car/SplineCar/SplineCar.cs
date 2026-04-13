using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class SplineCar : MonoBehaviour
{
    private Mover _mover;
    private Transform _carHead;
    private TrackRegistrator _trackRegistrator;
    private SplineAnimate _splineAnimate;
    private SplineContainer _track;
    private SplineContainer _exitTrack;
    private SplineOperator _splineOperator;
    private Vector3 _currentPosition;
    private Vector3 _enterPoint;
    private bool _isMoving = false;
    private float _interpolatedSplinePosition;
    private float _speed;
    private float _searchMin;
    private float _searchMax;

    public void Initialize(Mover mover, Transform carHead, TrackSwitcher trackSwitcher,
                        SplineAnimate splineAnimate, SplineContainer track, float speed,
                        float searchMin, float searchMax, TrackRegistrator trackRegistrator,
                        float waitTime, SplineContainer exitTrack, SplineOperator splineOperator,
                        int wagonCount = 1)
    {
        _mover = mover;
        _carHead = carHead;
        _trackRegistrator = trackRegistrator;
        _splineAnimate = splineAnimate;
        _track = track;
        _exitTrack = exitTrack;
        _splineOperator = splineOperator;
        _speed = speed;
        _searchMin = searchMin;
        _searchMax = searchMax;

        _splineOperator.Initialize(splineAnimate);

        Debug.Log("Spline Car initialized");
    }

    public void GoToTrack()
    {
        Debug.Log("Spline Car goes to track");

        _currentPosition = _carHead.position;
        _enterPoint = _trackRegistrator.GetEntryPoint(_currentPosition, _searchMin,
                                               _searchMax, out _interpolatedSplinePosition);
        StartCoroutine(EnterTrackWithPause());
    }

    public Vector3 GetPosition()
    {
        return _carHead.position;
    }

    public void Jump()
    {
        _splineOperator.JumpForward();
    }

    public void Stop()
    {
        _splineOperator.Pause();
    }

    public void Play()
    {
        _splineOperator.Play();
    }

    public void Exit()
    {
        Debug.Log("Spline Car received exit, changing track");
        _splineOperator.SwitchSpline(_exitTrack, _carHead, _speed);
    }

    public void SetNormalizedTime(float t)
    {
        _splineOperator.SetNormalizedTime(t);
    }

    private IEnumerator EnterTrackWithPause()
    {
        _currentPosition = _carHead.position;
        _enterPoint = _trackRegistrator.GetEntryPoint(_currentPosition, _searchMin,
                                              _searchMax, out _interpolatedSplinePosition);

        _isMoving = true;
        _mover.FinishedMoving -= OnMoverFinishedMove;
        _mover.FinishedMoving += OnMoverFinishedMove;
        _mover.MoveToPercent(_enterPoint, 0.1f);
        _carHead.LookAt(_enterPoint);

        yield return new WaitUntil(() => _isMoving == false);

        _isMoving = false;

        _isMoving = true;
        _mover.FinishedMoving -= OnMoverFinishedMove;
        _mover.FinishedMoving += OnMoverFinishedMove;
        _mover.MoveTo(_enterPoint);
        _carHead.LookAt(_enterPoint);

        yield return new WaitUntil(() => _isMoving == false);

        _trackRegistrator.Register(_splineAnimate, _carHead);
        _splineOperator.EnableSplineAnimate(_track, _interpolatedSplinePosition, _speed);
    }

    private void OnMoverFinishedMove()
    {
        _isMoving = false;
        _mover.FinishedMoving -= OnMoverFinishedMove;
    }
}
