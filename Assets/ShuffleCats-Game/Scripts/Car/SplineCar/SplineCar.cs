using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class SplineCar : MonoBehaviour
{
    public event Action TrackEntered;

    private Mover _mover;
    private Transform _carHead;
    private TrackRegistrator _trackRegistrator;
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
        _track = track;
        _exitTrack = exitTrack;
        _splineOperator = splineOperator;
        _speed = speed;
        _searchMin = searchMin;
        _searchMax = searchMax;

        _splineOperator.Initialize(splineAnimate);
        IsOnExit = false;
    }

    public void GoToTrack()
    {
        StartCoroutine(EnterTrackWithPause());
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
        StartCoroutine(SwitchSplineRoutine());
    }

    private IEnumerator SwitchSplineRoutine()
    {
        Vector3 target = _splineOperator.PrepareSwitchToNearest(_exitTrack, _carHead.position, _speed, out float targetT);

        _isMoving = true;
        _mover.FinishedMoving -= OnMoverFinishedMove;
        _mover.FinishedMoving += OnMoverFinishedMove;
        _mover.MoveTo(target);

        yield return new WaitUntil(() => _isMoving == false);

        // Cat is now at the exit-spline target world position. Setting NormalizedTime now
        // is safe because SplineAnimate's internal UpdateTransform() will evaluate to
        // exactly this same world position — no visible snap.
        _splineOperator.ApplyNormalizedTime(targetT);

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

        WaitForSeconds clearanceWait = new WaitForSeconds(0.1f);
        while (IsEntryClear() == false)
        {
            yield return clearanceWait;
            _enterPoint = _trackRegistrator.GetEntryPoint(_currentPosition, _searchMin,
                                                  _searchMax, out _interpolatedSplinePosition);
        }

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
        TrackEntered?.Invoke();
    }

    private void OnMoverFinishedMove()
    {
        _isMoving = false;
        _mover.FinishedMoving -= OnMoverFinishedMove;
    }

    private bool IsEntryClear()
    {
        if (_carHead.TryGetComponent<BoxCollider>(out BoxCollider col) == false)
            return true;

        SplineUtility.Evaluate(_track.Spline, _interpolatedSplinePosition,
                               out float3 _, out float3 tangent3, out float3 _);
        Vector3 worldTangent = _track.transform.TransformDirection((Vector3)tangent3);

        if (worldTangent.sqrMagnitude < 0.0001f)
            return true;

        worldTangent = worldTangent.normalized;

        float scale = _carHead.lossyScale.z;
        float halfSize = col.size.z * 0.5f * scale;
        float centerZ = col.center.z * scale;
        float forwardExtent = centerZ + halfSize;
        float backwardExtent = halfSize - centerZ;
        float safety = 0.3f;

        int carsMask = LayerMask.GetMask("Cars");

        if (Physics.Raycast(_enterPoint, worldTangent, forwardExtent + safety, carsMask))
            return false;
        if (Physics.Raycast(_enterPoint, -worldTangent, backwardExtent + safety, carsMask))
            return false;

        return true;
    }
}
