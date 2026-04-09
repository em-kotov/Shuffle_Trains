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
                        float waitTime, int wagonCount = 1)
    {
        _mover = mover;
        _carHead = carHead;
        _trackRegistrator = trackRegistrator;
        _splineAnimate = splineAnimate;
        _track = track;
        _speed = speed;
        _searchMin = searchMin;
        _searchMax = searchMax;

        Debug.Log("Spline Car initialized");
    }

    public void GoToTrack()
    {
        Debug.Log("Spline Car goes to track");
        _currentPosition = _carHead.position;

        _enterPoint = _trackRegistrator.GetEntryPoint(_currentPosition, _searchMin,
                                               _searchMax, out _interpolatedSplinePosition);
        StartCoroutine(MoveWithPause());
    }

    public Vector3 GetPosition()
    {
        return _carHead.position;
    }

    private IEnumerator MoveWithPause()
    {
        _currentPosition = _carHead.position;
        _enterPoint = _trackRegistrator.GetEntryPoint(_currentPosition, _searchMin,
                                              _searchMax, out _interpolatedSplinePosition);

        _isMoving = true;

        _mover.FinishedMoving -= MovedToTheTarget;
        _mover.FinishedMoving += MovedToTheTarget;

        _mover.MoveToPercent(_enterPoint, 0.1f);
        _carHead.LookAt(_enterPoint);

        yield return new WaitUntil(() => _isMoving == false);

        _isMoving = false;

        _isMoving = true;
        _mover.FinishedMoving -= MovedToTheTarget;
        _mover.FinishedMoving += MovedToTheTarget;
        _mover.MoveTo(_enterPoint);
        _carHead.LookAt(_enterPoint);

        yield return new WaitUntil(() => _isMoving == false);

        _trackRegistrator.Register(_splineAnimate, _carHead);
        EnterSpline();
    }

    private void MovedToTheTarget()
    {
        _isMoving = false;
        _mover.FinishedMoving -= MovedToTheTarget;
    }

    private void EnterSpline()
    {
        _splineAnimate.enabled = true;
        //Debug.Log("Spline Car container splines: " + _track.Splines[0]);
        _splineAnimate.Container = _track;
       // _splineAnimate.Container.Spline = _track.Splines[0];
        _splineAnimate.StartOffset = _interpolatedSplinePosition;
        _splineAnimate.Duration = CalculateDuration(_track.Spline);
        _splineAnimate.Play();
    }

    private float CalculateDuration(ISpline spline)
    {
        return spline.GetLength() / _speed;
    }

    //spherecast methods
    //

    public void OnBumped(float distance)
    {
        MoveOneStepForward();
    }

    private void MoveOneStepForward()
    {
        float stepSize = 0.01f;
        _splineAnimate.NormalizedTime = Mathf.Min(1f, _splineAnimate.NormalizedTime + stepSize);
    }

    //passenger car methods below

    public void OnStopFound()
    {
        Debug.Log("Spline Car pausing spline animate");
        StartCoroutine(PauseSplineAnimate());
    }

    public void OnPassengerReleased()
    {

    }

    private IEnumerator PauseSplineAnimate()
    {
        SplineAnimatePaused?.Invoke();
        _splineAnimate.Pause();
        yield return new WaitForSeconds(0.8f);
        _splineAnimate.Play();
        SplineAnimateResumed?.Invoke();
        Debug.Log("Spline Car resumed spline animate");
    }
}
