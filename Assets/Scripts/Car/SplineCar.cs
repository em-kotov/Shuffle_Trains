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
        _mover.FinishedMoving += OnFinishedMoving;
    }

    public void OnBorder()
    {
        if (_isMoving)
            return;

        _currentPosition = _carHead.position;

        Vector3 enterPoint = _trackSwitcher.GetEnterPoint(_currentPosition, _searchMin,
                                            _searchMax, out _interpolatedSplinePosition);
        StartCoroutine(SmoothMoveTo(enterPoint));
    }

    public void OnFinishedMoving()
    {
        _isMoving = false;
        _mover.FinishedMoving -= OnFinishedMoving;

        _splineAnimate.enabled = true;
        _splineAnimate.Container = _track;
        _splineAnimate.StartOffset = _interpolatedSplinePosition;
        _splineAnimate.Duration = CalculateDuration(_track.Spline);
        _splineAnimate.Play();
    }

    private IEnumerator SmoothMoveTo(Vector3 target)
    {
        _isMoving = true;
        _mover.MoveTo(target);
        _carHead.LookAt(target);

        yield return new WaitUntil(() => _isMoving == false);
    }

    private float CalculateDuration(ISpline spline)
    {
        return spline.GetLength() / _speed;
    }
}
