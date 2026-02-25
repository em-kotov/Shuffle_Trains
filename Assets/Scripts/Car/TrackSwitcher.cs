using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class TrackSwitcher : MonoBehaviour
{
    private SplineContainer _track;
    private const int _resolutiion = 4;
    private const int _iterations = 2;

    public void Initialize(SplineContainer track)
    {
        _track = track;
    }

    public Vector3 GetEnterPoint(Vector3 position, out float _interpolatedSplinePosition)
    {
        Vector3 localPosition = _track.transform.InverseTransformPoint(position);

        float searchMin = 0f;
        float searchMax = 0.4f; // should receive in method
        SplineUtilityExtension.GetNearestPoint(_track.Spline, localPosition, out float3 nearest,
                                    out _interpolatedSplinePosition, searchMin, searchMax, _resolutiion, _iterations);
        return _track.transform.TransformPoint(nearest);
    }
}
