using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class TrackRegistrator : MonoBehaviour
{
    [SerializeField] private SplineContainer _gizmoTrack;

    private SplineContainer _track;
    private CounterUI _counter;
    private List<Transform> _carHeads;
    private int _currentCarsCount;
    private int _maxCarsCount;

    public void Initialize(SplineContainer track, CounterUI counter,
                        int maxCarsCount)
    {
        _track = track;
        _counter = counter;
        _currentCarsCount = 0;
        _maxCarsCount = maxCarsCount;
        _carHeads = new();

        _counter.Initialize(_maxCarsCount);
    }

    public bool IsCountAllows()
    {
        return _currentCarsCount < _maxCarsCount;
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
}
