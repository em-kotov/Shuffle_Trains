using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class TrackSwitcher : MonoBehaviour
{
    private SplineContainer _origSpline;
    private const int _resolutiion = 4;
    private const int _iterations = 2;

    public void Initialize(SplineContainer origSpline)
    {
        _origSpline = origSpline;
    }

    public Vector3 GetEnterPoint(Vector3 position, out float _interpolatedSplinePosition)
    {
        Vector3 localPosition = _origSpline.transform.InverseTransformPoint(position);

        SplineUtility.GetNearestPoint(_origSpline.Spline, localPosition, out float3 nearest,
                                    out _interpolatedSplinePosition, _resolutiion, _iterations);

        return _origSpline.transform.TransformPoint(nearest);
    }
}
