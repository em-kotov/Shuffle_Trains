using System;
using System.Collections;
using UnityEngine;

public class Mover : MonoBehaviour
{
    private float _slideDuration = 0.3f;
    private Transform _carHead;

    public event Action FinishedMoving;

    public void Initialize(Transform carHead, float duration = 0.3f)
    {
        _carHead = carHead;
        _slideDuration = duration;
    }

    public void MoveTo(Vector3 target, float percent = 1f)
    {
        StartCoroutine(SmoothMoveTo(target, percent));
    }

    public void MoveToPercent(Vector3 target, float percent = 1f)
    {
        StartCoroutine(SmoothMoveToPercent(target, percent));
    }

    private IEnumerator SmoothMoveTo(Vector3 target, float percent = 1f)
    {
        float elapsed = 0f;
        Vector3 start = _carHead.position;

        while (elapsed < _slideDuration)
        {
            elapsed += Time.deltaTime;
            float time = elapsed / _slideDuration;
            _carHead.position = Vector3.Lerp(start, target, time);
            yield return null;
        }

        _carHead.position = target;
        FinishedMoving?.Invoke();
    }

    private IEnumerator SmoothMoveToPercent(Vector3 target, float percent = 1f)
    {
        float elapsed = 0f;
        Vector3 start = _carHead.position;
        Vector3 newTarget;

        newTarget = Vector3.Lerp(start, target, percent);

        while (elapsed < _slideDuration)
        {
            elapsed += Time.deltaTime;
            float time = elapsed / _slideDuration;
            _carHead.position = Vector3.Lerp(start, newTarget, time);
            yield return null;
        }

        _carHead.position = newTarget;
        FinishedMoving?.Invoke();
    }
}
