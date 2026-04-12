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

    public void JumpForward(float distance)
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
        //_splineAnimate.StartOffset = 0.25f; //t of exit spline to enter
        _splineAnimate.NormalizedTime = t; //t of exit spline to enter

        _splineAnimate.Duration = CalculateDuration(newSplineContainer.Spline, speed);
        //_splineAnimate.Loop = SplineAnimate.LoopMode.Once;
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

    public void PauseForTime(float time)
    {
        StartCoroutine(PauseSplineAnimateForTime(time));
    }

    public bool IsActive()
    {
        return _splineAnimate.IsPlaying;
    }

    public float GetNormalizedTime()
    {
        return _splineAnimate.NormalizedTime;
    }

    private IEnumerator PauseSplineAnimateForTime(float timeA)
    {
        float time = 0.8f;
        //SplineAnimatePaused?.Invoke();
        _splineAnimate.Pause();
        yield return new WaitForSeconds(time);
        _splineAnimate.Play();
        //SplineAnimateResumed?.Invoke();
        Debug.Log("Spline Car resumed spline animate");
    }

    private float CalculateDuration(ISpline spline, float speed)
    {
        return spline.GetLength() / speed;
    }
}
