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
    private float _waitTime;
    private int _wagonCount;

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
        _waitTime = waitTime;
        _wagonCount = wagonCount;

        _mover.FinishedMoving += OnPausedMoving;
    }

    public void OnBorder()
    {
        if (_isMoving)
            return;

        _currentPosition = _carHead.position;

        _enterPoint = _trackRegistrator.GetEntryPoint(_currentPosition, _searchMin,
                                            _searchMax, out _interpolatedSplinePosition);
        StartCoroutine(SmoothMoveTo(_enterPoint));
    }

    public Vector3 GetPosition()
    {
        return _carHead.position;
    }

    private void OnPausedMoving()
    {
        _isMoving = false;
        _mover.FinishedMoving -= OnPausedMoving;

        StartCoroutine(WaitFreeEntrance());
    }

    private IEnumerator SmoothMoveTo(Vector3 target)
    {
        _isMoving = true;
        _mover.MoveToPercent(target, 0.1f);
        _carHead.LookAt(target);

        yield return new WaitUntil(() => _isMoving == false);
    }

    private IEnumerator WaitFreeEntrance()
    {
        // WaitForSeconds wait = new(_waitTime);
        // bool isFree = _trackRegistrator.IsSegmentFreeToEnter(_interpolatedSplinePosition, _wagonCount);

        // while (isFree == false)
        // {
        //     yield return wait;
        //     isFree = _trackRegistrator.IsSegmentFreeToEnter( _interpolatedSplinePosition, _wagonCount);
        // }

        _isMoving = true;
        _mover.FinishedMoving -= MovedToTheTarget;
        _mover.FinishedMoving -= OnPausedMoving;
        _mover.FinishedMoving += MovedToTheTarget;
        _mover.MoveTo(_enterPoint);
        _carHead.LookAt(_enterPoint);

        yield return new WaitUntil(() => _isMoving == false);

        // _trackRegistrator.Register(_splineAnimate, _carHead);
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
        _splineAnimate.Container = _track;
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
}
