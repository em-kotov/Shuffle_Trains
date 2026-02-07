using System;
using System.Collections;
using UnityEngine;

public class Mover : MonoBehaviour
{
    [SerializeField] private float _slideDuration = 0.3f;

    public event Action FinishedMoving;

    public void MoveTo(Vector3 target)
    {
        StartCoroutine(SmoothMoveTo(target));
    }

    private IEnumerator SmoothMoveTo(Vector3 target)
    {
        float elapsed = 0f;
        Vector3 start = transform.position;

        while (elapsed < _slideDuration)
        {
            elapsed += Time.deltaTime;
            float time = elapsed / _slideDuration;
            transform.position = Vector3.Lerp(start, target, time);
            yield return null;
        }

        transform.position = target;
        FinishedMoving?.Invoke();
    }
}
