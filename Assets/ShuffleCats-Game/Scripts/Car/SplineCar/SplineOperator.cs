using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class SplineOperator : MonoBehaviour
{
    private SplineAnimate _splineAnimate;
    private ParkingRegistrator _parkingRegistrator;
    private float _baseDuration;
    private float _speedMultiplier = 1f;
    private bool _isFollowingSpline;

    public void Initialize(SplineAnimate splineAnimate, ParkingRegistrator parkingRegistrator)
    {
        _splineAnimate = splineAnimate;
        _parkingRegistrator = parkingRegistrator;
        _isFollowingSpline = false;
    }

    private void Update()
    {
        if (_isFollowingSpline == false)
            return;

        float multiplier = _parkingRegistrator.TrackSpeedMultiplier;

        if (Mathf.Approximately(multiplier, _speedMultiplier))
            return;

        float normalizedTime = _splineAnimate.NormalizedTime;
        _speedMultiplier = multiplier;
        _splineAnimate.Duration = _baseDuration / _speedMultiplier;
        _splineAnimate.NormalizedTime = normalizedTime;
    }

    private void ConfigureDuration(ISpline spline, float speed)
    {
        _baseDuration = CalculateDuration(spline, speed);
        _speedMultiplier = _parkingRegistrator.TrackSpeedMultiplier;
        _splineAnimate.Duration = _baseDuration / _speedMultiplier;
    }

    public void EnableSplineAnimate(SplineContainer splineContainer, float startOffset, float speed)
    {
        _splineAnimate.enabled = true;
        _splineAnimate.Container = splineContainer;
        _splineAnimate.StartOffset = startOffset;
        ConfigureDuration(splineContainer.Spline, speed);
        // OnEnable may restart with the old container/offset; restore entry before the trail samples it.
        _splineAnimate.NormalizedTime = 0f;
        _isFollowingSpline = true;
        _splineAnimate.Play();
    }

    public void Pause()
    {
        _splineAnimate.Pause();
    }

    public void Play()
    {
        _splineAnimate.Play();
    }

    public Vector3 PrepareSwitchToNearest(SplineContainer newSplineContainer, Vector3 worldPosition, float speed, out float targetT)
    {
        _isFollowingSpline = false;
        Vector3 localPosition = newSplineContainer.transform.InverseTransformPoint(worldPosition);
        SplineUtilityExtension.GetNearestPoint(newSplineContainer.Spline, localPosition,
                                        out float3 nearest, out float t, resolution: 32, iterations: 8);
        targetT = t;
        // Setting NormalizedTime here would invoke SplineAnimate.UpdateTransform and teleport the cat.
        _splineAnimate.Container = newSplineContainer;
        _splineAnimate.StartOffset = 0f;
        ConfigureDuration(newSplineContainer.Spline, speed);
        return newSplineContainer.transform.TransformPoint(nearest);
    }

    public void ApplyNormalizedTime(float t)
    {
        _splineAnimate.NormalizedTime = t;
        _isFollowingSpline = true;
    }

    private float CalculateDuration(ISpline spline, float speed)
    {
        return spline.GetLength() / speed;
    }
}
