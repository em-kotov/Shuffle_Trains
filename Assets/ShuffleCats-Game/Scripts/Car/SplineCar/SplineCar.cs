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

    public bool IsOnExit { get; private set; }

    public void Initialize(Mover mover, Transform carHead, TrackSwitcher trackSwitcher,
                        SplineAnimate splineAnimate, SplineContainer track, float speed,
                        float searchMin, float searchMax, TrackRegistrator trackRegistrator,
                        SplineContainer exitTrack, SplineOperator splineOperator)
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
        IsOnExit = false;

        Debug.Log("Spline Car initialized");
    }

    public void GoToTrack()
    {
        Debug.Log("Spline Car goes to track");
        StartCoroutine(EnterTrackWithPause());
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

    public Vector3 GetPosition()
    {
        return _carHead.position;
    }

    public bool IsReachedEnd()
    {
        SplineUtility.Evaluate(_exitTrack.Spline, 1.0f, out float3 splineEndPos, out _, out _);
        Vector3 endPosition = _exitTrack.transform.TransformPoint(splineEndPos);
        return Vector3.Distance(_carHead.position, endPosition) <= 1f;
    }

    public void SwitchSplineToNearest()
    {
        Debug.Log("Spline Car received exit, changing track");
        _splineOperator.SwitchSplineToNearest(_exitTrack, _carHead, _speed);
        IsOnExit = true;
    }

    public void Unregister()
    {
        _trackRegistrator.Unregister();
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

        _trackRegistrator.Register(_carHead);
        _splineOperator.EnableSplineAnimate(_track, _interpolatedSplinePosition, _speed);
    }

    private void OnMoverFinishedMove()
    {
        _isMoving = false;
        _mover.FinishedMoving -= OnMoverFinishedMove;
    }

    // private void OnDrawGizmos()
    // {
    //     SplineUtility.Evaluate(_exitTrack.Spline, 1.0f, out float3 splineEndPos, out _, out _);
    //     Vector3 endPosition = _exitTrack.transform.TransformPoint(splineEndPos);

    //     Gizmos.color = Color.magenta;
    //     Gizmos.DrawSphere(endPosition, 1f);
    // }
}
