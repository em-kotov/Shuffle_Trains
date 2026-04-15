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

    public void JumpForward()
    {
        float stepSize = 0.01f;
        _splineAnimate.NormalizedTime = Mathf.Min(1f, _splineAnimate.NormalizedTime + stepSize);
    }

    public void SwitchSplineToNearest(SplineContainer newSplineContainer, Transform car, float speed)
    {
        Vector3 worldPosition = car.position;
        _splineAnimate.Container = newSplineContainer;
        _splineAnimate.StartOffset = 0f;
        Vector3 localPosition = newSplineContainer.transform.InverseTransformPoint(worldPosition);
        SplineUtilityExtension.GetNearestPoint(newSplineContainer.Spline, localPosition,
                                        out float3 nearest, out float t, resolution: 32, iterations: 8);
        _splineAnimate.NormalizedTime = t;
        car.position = newSplineContainer.transform.TransformPoint(nearest);
        _splineAnimate.Duration = CalculateDuration(newSplineContainer.Spline, speed);
    }

    private float CalculateDuration(ISpline spline, float speed)
    {
        return spline.GetLength() / speed;
    }
}
