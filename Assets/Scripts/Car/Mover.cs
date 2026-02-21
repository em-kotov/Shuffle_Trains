using System;
using System.Collections;
using UnityEngine;

public class Mover : MonoBehaviour
{
    [SerializeField] private float _slideDuration = 0.3f;

    private Transform _carHead;

    public event Action FinishedMoving;

    public void Initialize(Transform carHead)
    {
        _carHead = carHead;
    }

    public void MoveTo(Vector3 target)
    {
        StartCoroutine(SmoothMoveTo(target));
    }

    private IEnumerator SmoothMoveTo(Vector3 target)
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
}
