using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class TrackRegistrator : MonoBehaviour
{
    [SerializeField] private SplineContainer _gizmoTrack;

    private SplineContainer _track;
    private Counter _counter;
    private List<Transform> _carHeads;

    public void Initialize(SplineContainer track, Counter counter,
                        int maxCarsCount)
    {
        _track = track;
        _counter = counter;
        _carHeads = new();

        _counter.Initialize(maxCarsCount);
        _counter.ShowCurrent();
    }

    public bool IsCountAllows()
    {
        return _counter.CurrentCount < _counter.MaxCount;
    }

    public void Register(Transform carHead)
    {
        _carHeads.Add(carHead);
        _counter.Add();
        _counter.ShowCurrent();
    }

    public void Unregister()
    {
        _counter.Remove();
        _counter.ShowCurrent();
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
