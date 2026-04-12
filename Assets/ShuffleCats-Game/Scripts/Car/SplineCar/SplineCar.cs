using System;
using System.Collections;
using Unity.Mathematics;
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

    public event Action SplineAnimateResumed;
    public event Action SplineAnimatePaused;

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
        //EnableSplineAnimate();
        _splineOperator.EnableSplineAnimate(_track, _interpolatedSplinePosition, _speed);
    }

    private void OnMoverFinishedMove()
    {
        _isMoving = false;
        _mover.FinishedMoving -= OnMoverFinishedMove;
    }

    // private void EnableSplineAnimate()
    // {
    //     _splineAnimate.enabled = true;
    //     _splineAnimate.Container = _track;
    //     _splineAnimate.StartOffset = _interpolatedSplinePosition;
    //     _splineAnimate.Duration = CalculateDuration(_track.Spline);
    //     _splineAnimate.Play();
    // }

    private float CalculateDuration(ISpline spline)
    {
        return spline.GetLength() / _speed;
    }

    //bump detector methods

    public void OnBumped(float distance)
    {
        //JumpForwardOneStep();
        float stepSize = 0.01f;
        _splineOperator.JumpForward(stepSize);
    }

    // private void JumpForwardOneStep()
    // {
    //     float stepSize = 0.01f;
    //     _splineAnimate.NormalizedTime = Mathf.Min(1f, _splineAnimate.NormalizedTime + stepSize);
    // }

    //passenger car methods below

    public void OnStopFound()
    {
        Debug.Log("Spline Car pausing spline animate");
        StartCoroutine(PauseSplineAnimateForTime());
    }

    public void OnPassengerReleased()
    {

    }

    private IEnumerator PauseSplineAnimateForTime()
    {
        float time = 0.8f;
        SplineAnimatePaused?.Invoke();
        //_splineAnimate.Pause();
        _splineOperator.Pause();
        yield return new WaitForSeconds(time);
        //_splineAnimate.Play();
        _splineOperator.Play();
        SplineAnimateResumed?.Invoke();
        Debug.Log("Spline Car resumed spline animate");
    }

    public void OnFinishedPassengers()
    {
        Debug.Log("Spline Car received exit, changing track");
        //SplineAnimatePaused?.Invoke();
        StartCoroutine(WaitUntilSpecificKnotReached());
    }

    private IEnumerator WaitUntilSpecificKnotReached()
    {
        float specificKnot = 0.38f; //t of track spline to switch where

        //while (_splineAnimate.NormalizedTime < specificKnot)
        while (_splineOperator.GetNormalizedTime() < specificKnot)
        {
            yield return null;
        }

        Debug.Log("Spline Car specific knot reached!");
        // _splineAnimate.Pause();
        // _splineAnimate.Container = _exitTrack;

        // Vector3 localPosition = _exitTrack.transform.InverseTransformPoint(
        //                                             _carHead.position);

        // SplineUtilityExtension.GetNearestPoint(_exitTrack.Spline, localPosition,
        //                                 out float3 nearest, out float t);
        // //_splineAnimate.StartOffset = 0.25f; //t of exit spline to enter
        // _splineAnimate.NormalizedTime = t; //t of exit spline to enter

        // _splineAnimate.Duration = CalculateDuration(_exitTrack.Spline);
        // //_splineAnimate.Loop = SplineAnimate.LoopMode.Once;
        // _splineAnimate.Play();
        _splineOperator.SwitchSpline(_exitTrack, _carHead, _speed);
    }

    public void Deactivate()
    {
        Debug.Log("Spline Car Received inquiry for deactivate");
        //_splineAnimate.Pause();
        _splineOperator.Pause();
    }
}
