using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class TrackSwitcher : MonoBehaviour
{
    // private SplineContainer _track;
    // private SplineCar _splineCar;
    // private TrackRegistrator _registrator;
    // private const int _resolutiion = 4;
    // private const int _iterations = 2;

    // public void Initialize(SplineContainer track, TrackRegistrator registrator, SplineCar car)
    // {
    //     _track = track;
    //     _registrator = registrator;
    //     _splineCar = car;
    // }

    // public Vector3 GetEnterPoint(Vector3 position, float searchMin, float searchMax,
    //                             out float _interpolatedSplinePosition)
    // {
    //     Vector3 localPosition = _track.transform.InverseTransformPoint(position);

    //     SplineUtilityExtension.GetNearestPoint(_track.Spline, localPosition,
    //                                 out float3 nearest, out _interpolatedSplinePosition,
    //                                 searchMin, searchMax, _resolutiion, _iterations);

    //     return _track.transform.TransformPoint(nearest);
    // }

    // public bool IsEntranceFree(Vector3 enterPoint, float position)
    // {
    //     bool isFree = _registrator.IsSegmentFreeToEnter(position,1);
    //     Debug.Log(isFree);
    //     return isFree;
    // }

    // public float GetPosition()
    // {
    //     float position;

    //     SplineUtilityExtension.GetNearestPoint(_track.Spline, _splineCar.GetPosition(),
    //                                  out float3 nearest, out position);

    //     return position;
    // }
}
