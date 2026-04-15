using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class TrackRegistrator : MonoBehaviour
{
    private SplineContainer _track;
    private CounterUI _counter;
    private List<(float start, float end, Transform car)> _segments;
    private List<(float length, Transform car)> _newSegments;
    private List<Transform> _carHeads;
    private int _segmentsCount = 10;
    // private int _minSegmentsCount = 10;
    // private float _minPosition = 0f;
    // private float _maxPosition = 1f;
    // private float _minPositionException = -0.1f;
    private int _currentCarsCount;
    private int _maxCarsCount;
    // private float _carLength = 0.1f;
    private float _trackLength;
    [SerializeField] private SplineContainer _gizmoTrack;

    public void Initialize(SplineContainer track, CounterUI counter,
                        int segmentsCount, int maxCarsCount)
    {
        _track = track;
        _counter = counter;
        _segmentsCount = segmentsCount;
        _currentCarsCount = 0;
        _maxCarsCount = maxCarsCount;
        _trackLength = _track.Spline.GetLength();

        _counter.Initialize(_maxCarsCount);

        InitializeSegments();
        _carHeads = new();
    }

    public bool IsCountAllows()
    {
        if (_currentCarsCount >= _maxCarsCount)
        {
            Debug.Log("cars count reached maximum, cannot add more car now");
            return false;
        }

        return true;
    }

    public bool IsSegmentFreeToEnter(float carEntryPoint, int wagonCount)
    {
        if (_currentCarsCount >= _maxCarsCount)
        {
            Debug.Log("cars count reached maximum, cannot add more car now");
            return false;
        }

        int entrySegmentIndex = FindSegmentIndex(carEntryPoint);

        UpdatePositions();

        for (int i = 0; i < wagonCount; i++)
        {
            int index = (entrySegmentIndex - i + _newSegments.Count) % _newSegments.Count;

            if (_newSegments[index].car != null)
            {
                Debug.Log("checked segment is occupied, cannot add car there now");
                return false;
            }
        }

        Debug.Log("segment is free, can add car");
        return true;
    }

    public void Register(Transform carHead)
    {
        _carHeads.Add(carHead);

        _currentCarsCount++;
        _counter.UpdateCurrent(_currentCarsCount);
    }

    public void Unregister()
    {
        _currentCarsCount--;
        _counter.UpdateCurrent(_currentCarsCount);
    }

    public Vector3 GetEntryPoint(Vector3 position, float searchMin, float searchMax,
                                out float interpolatedSplinePosition)
    {
        Vector3 localPosition = _track.transform.InverseTransformPoint(position);

        SplineUtilityExtension.GetNearestPoint(_track.Spline, localPosition,
                                    out float3 nearest, out interpolatedSplinePosition,
                                    searchMin, searchMax);

        return _track.transform.TransformPoint(nearest);
    }

    private void UpdatePositions()
    {
        //for each carhead of carheads get current t, find index in segments and make car not null.

        NullifySegments();

        for (int i = 0; i < _carHeads.Count; i++)
        {
            // Vector3 localPosition = _track.transform.InverseTransformPoint(_carHeads[i].position);

            // SplineUtilityExtension.GetNearestPoint(_track.Spline, localPosition,
            //                         out float3 nearest1, out float carTPoint);

            float tPoint = SplineUtility.GetNearestPoint(_track.Spline, _carHeads[i].position, out float3 nearest, out float t);

            // Debug.Log("track registrator Update Positions float t: " + interpolatedSplinePosition);

            int index = FindSegmentIndex(tPoint);

            _newSegments[index] = (_newSegments[index].length, _carHeads[i]);
        }
    }

    private void NullifySegments()
    {
        for (int i = 0; i < _newSegments.Count; i++)
        {
            _newSegments[i] = (_newSegments[i].length, null);
        }
    }

    private void OnDrawGizmos()
    {
        float trackLength = _gizmoTrack.Spline.GetLength();
        int segmentCount = 46;
        float segmentLength = trackLength / segmentCount;

        Gizmos.color = Color.red;

        for (int i = 0; i <= segmentCount; i++)
        {
            float distance = segmentLength * i;

            float t = distance / trackLength;
            Vector3 localPosition = _gizmoTrack.Spline.EvaluatePosition(t);
            Vector3 worldPosition = _gizmoTrack.transform.TransformPoint(localPosition);

            Gizmos.DrawSphere(worldPosition, 0.1f);
        }
    }

    private void InitializeSegments()
    {
        float segmentLength = _trackLength / _segmentsCount;

        _newSegments = new List<(float length, Transform car)>();

        for (int i = 1; i <= _segmentsCount; i++)
        {
            _newSegments.Add((segmentLength * i, null));
        }
    }

    private int FindSegmentIndex(float carTPoint)
    {
        float lengthPoint = carTPoint * _trackLength;
        int index = _newSegments.FindLastIndex(segment => segment.length <= lengthPoint);

        if (index == -1)
        {
            index = 0;
        }

        Debug.Log("found segment index: " + index + "");
        return index;
    }

    // private float Map(float from, float fromMin, float fromMax, float toMin, float toMax)
    // {
    //     float fromAbs = from - fromMin;
    //     float fromMaxAbs = fromMax - fromMin;
    //     float normal = fromAbs / fromMaxAbs;

    //     float toMaxAbs = toMax - toMin;
    //     float toAbs = toMaxAbs * normal;
    //     float to = toAbs + toMin;
    //     return to;
    // }
}
