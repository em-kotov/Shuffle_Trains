using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class SplineCar : MonoBehaviour
{
    private Mover _mover;
    private Transform _carHead;
    private TrackSwitcher _trackSwitcher;
    private SplineAnimate _splineAnimate;
    private SplineContainer _track;
    private Vector3 _currentPosition;
    private Vector3 _enterPoint;
    private bool _isMoving = false;
    private float _interpolatedSplinePosition;
    private float _speed;
    private float _searchMin;
    private float _searchMax;

    public void Initialize(Mover mover, Transform carHead, TrackSwitcher trackSwitcher,
                        SplineAnimate splineAnimate, SplineContainer track, float speed,
                        float searchMin, float searchMax)
    {
        _mover = mover;
        _carHead = carHead;
        _trackSwitcher = trackSwitcher;
        _splineAnimate = splineAnimate;
        _track = track;
        _speed = speed;
        _searchMin = searchMin;
        _searchMax = searchMax;

        _trackSwitcher.Initialize(track);
        _mover.FinishedMoving += OnPausedMoving;
    }

    public void OnBorder()
    {
        if (_isMoving)
            return;

        _currentPosition = _carHead.position;

        _enterPoint = _trackSwitcher.GetEnterPoint(_currentPosition, _searchMin,
                                            _searchMax, out _interpolatedSplinePosition);
        StartCoroutine(SmoothMoveTo(_enterPoint));
    }

    private void OnPausedMoving()
    {
        _isMoving = false;
        _mover.FinishedMoving -= OnPausedMoving;
        Debug.Log("on paused moving");

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
        WaitForSeconds wait = new(0.5f);
        bool isFree = _trackSwitcher.IsEntranceFree(_enterPoint);

        while (isFree == false)
        {
            yield return wait;
            isFree = _trackSwitcher.IsEntranceFree(_enterPoint);
        }

        _isMoving = true;
        _mover.FinishedMoving -= MovedToTheTarget;
        _mover.FinishedMoving -= OnPausedMoving;
        _mover.FinishedMoving += MovedToTheTarget;
        Debug.Log("wait free entrance - sending car to move");
        _mover.MoveTo(_enterPoint);
        _carHead.LookAt(_enterPoint);
        
        yield return new WaitUntil(() => _isMoving == false);

        EnterSpline();
    }

    private void MovedToTheTarget()
    {
        _isMoving = false;
        Debug.Log("moved to the target");
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
}
