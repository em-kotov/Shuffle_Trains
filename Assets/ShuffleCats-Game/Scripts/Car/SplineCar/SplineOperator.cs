using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class SplineOperator : MonoBehaviour
{
    private SplineAnimate _splineAnimate;

    public void Initialize(SplineAnimate splineAnimate)
    {
        _splineAnimate = splineAnimate;
    }

    public void EnableSplineAnimate(SplineContainer splineContainer, float startOffset, float speed)
    {
        _splineAnimate.enabled = true;
        _splineAnimate.Container = splineContainer;
        _splineAnimate.StartOffset = startOffset;
        _splineAnimate.Duration = CalculateDuration(splineContainer.Spline, speed);
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
        Vector3 localPosition = newSplineContainer.transform.InverseTransformPoint(worldPosition);
        SplineUtilityExtension.GetNearestPoint(newSplineContainer.Spline, localPosition,
                                        out float3 nearest, out float t, resolution: 32, iterations: 8);
        targetT = t;
        // Configure container/offset/duration but do NOT touch NormalizedTime yet — setting
        // NormalizedTime calls UpdateTransform() inside SplineAnimate which teleports the cat.
        _splineAnimate.Container = newSplineContainer;
        _splineAnimate.StartOffset = 0f;
        _splineAnimate.Duration = CalculateDuration(newSplineContainer.Spline, speed);
        return newSplineContainer.transform.TransformPoint(nearest);
    }

    public void ApplyNormalizedTime(float t)
    {
        _splineAnimate.NormalizedTime = t;
    }

    private float CalculateDuration(ISpline spline, float speed)
    {
        return spline.GetLength() / speed;
    }
}
