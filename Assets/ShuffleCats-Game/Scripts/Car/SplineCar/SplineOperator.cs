using System.Collections;
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

    public void JumpForward()
    {
        float stepSize = 0.01f;
        _splineAnimate.NormalizedTime = Mathf.Min(1f, _splineAnimate.NormalizedTime + stepSize);
    }

    public void SwitchSpline(SplineContainer newSplineContainer, Transform car, float speed)
    {
        _splineAnimate.Pause();
        _splineAnimate.Container = newSplineContainer;

        Vector3 localPosition = newSplineContainer.transform.InverseTransformPoint(
                                                    car.position);

        SplineUtilityExtension.GetNearestPoint(newSplineContainer.Spline, localPosition,
                                        out float3 nearest, out float t);
        _splineAnimate.NormalizedTime = t; //t of exit spline to enter

        _splineAnimate.Duration = CalculateDuration(newSplineContainer.Spline, speed);
        //_splineAnimate.Play();
    }

    public void Pause()
    {
        _splineAnimate.Pause();
    }

    public void Play()
    {
        _splineAnimate.Play();
    }

    public void SetNormalizedTime(float time)
    {
        _splineAnimate.NormalizedTime = time;
    }

    private float CalculateDuration(ISpline spline, float speed)
    {
        return spline.GetLength() / speed;
    }
}
