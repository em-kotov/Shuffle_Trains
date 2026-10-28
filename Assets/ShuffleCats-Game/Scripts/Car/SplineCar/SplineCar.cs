using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class SplineCar : MonoBehaviour
{
    private const float TrackEntrySpeedMultiplier = 1.5f;

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

    public event Action TrackEntered;

    public event Action TrackEntryStarted;

    public bool IsOnExit { get; private set; }

    public void Initialize(
        Mover mover,
        Transform carHead,
        TrackSwitcher trackSwitcher,
        SplineAnimate splineAnimate,
        SplineContainer track,
        float speed,
        float searchMin,
        float searchMax,
        TrackRegistrator trackRegistrator,
        SplineContainer exitTrack,
        SplineOperator splineOperator,
        ParkingRegistrator parkingRegistrator)
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

        _splineOperator.Initialize(splineAnimate, parkingRegistrator);
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

    public bool IsReachedEnd()
    {
        const float normalizedSplineEnd = 1.0f;
        const float endReachedDistance = 1f;

        SplineUtility.Evaluate(_exitTrack.Spline, normalizedSplineEnd, out float3 splineEndPos, out _, out _);
        Vector3 endPosition = _exitTrack.transform.TransformPoint(splineEndPos);
        return Vector3.Distance(_carHead.position, endPosition) <= endReachedDistance;
    }

    public void SwitchSplineToNearest()
    {
        StartCoroutine(SwitchSplineRoutine());
    }

    public void Unregister()
    {
        _trackRegistrator.Unregister();
    }

    private IEnumerator SwitchSplineRoutine()
    {
        Vector3 target = _splineOperator.PrepareSwitchToNearest(_exitTrack, _carHead.position, _speed, out float targetT);

        _isMoving = true;
        _mover.FinishedMoving -= OnMoverFinishedMove;
        _mover.FinishedMoving += OnMoverFinishedMove;
        _mover.MoveTo(target);

        yield return new WaitUntil(() => _isMoving == false);

        // Setting NormalizedTime updates the transform; do it only after reaching the exit spline.
        _splineOperator.ApplyNormalizedTime(targetT);

        IsOnExit = true;
    }

    private IEnumerator EnterTrackWithPause()
    {
        const float clearanceCheckIntervalSeconds = 0.1f;
        const float entryApproachFraction = 0.1f;

        _currentPosition = _carHead.position;
        _enterPoint = _trackRegistrator.GetEntryPoint(
            _currentPosition,
            _searchMin,
            _searchMax,
            out _interpolatedSplinePosition);

        WaitForSeconds clearanceWait = new WaitForSeconds(clearanceCheckIntervalSeconds);

        while (IsEntryClear() == false)
        {
            yield return clearanceWait;
            _enterPoint = _trackRegistrator.GetEntryPoint(
                _currentPosition,
                _searchMin,
                _searchMax,
                out _interpolatedSplinePosition);
        }

        _isMoving = true;
        _mover.FinishedMoving -= OnMoverFinishedMove;
        _mover.FinishedMoving += OnMoverFinishedMove;
        TrackEntryStarted?.Invoke();
        _mover.MoveToPercent(_enterPoint, entryApproachFraction, speedMultiplier: TrackEntrySpeedMultiplier);
        _carHead.LookAt(_enterPoint);

        yield return new WaitUntil(() => _isMoving == false);

        _isMoving = false;

        _isMoving = true;
        _mover.FinishedMoving -= OnMoverFinishedMove;
        _mover.FinishedMoving += OnMoverFinishedMove;
        _mover.MoveTo(_enterPoint, speedMultiplier: TrackEntrySpeedMultiplier);
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
        const float minimumTangentSqrMagnitude = 0.0001f;

        if (_carHead.TryGetComponent<BoxCollider>(out BoxCollider col) == false)
            return true;

        SplineUtility.Evaluate(
            _track.Spline,
            _interpolatedSplinePosition,
            out float3 _,
            out float3 tangent3,
            out float3 _);
        Vector3 worldTangent = _track.transform.TransformDirection((Vector3)tangent3);

        if (worldTangent.sqrMagnitude < minimumTangentSqrMagnitude)
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
