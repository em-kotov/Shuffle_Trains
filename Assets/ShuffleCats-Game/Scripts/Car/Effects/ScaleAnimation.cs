using System.Collections;
using UnityEngine;

public class ScaleAnimation : MonoBehaviour
{
    [SerializeField] private float _minScaleX = 0.8f; // Minimum scale (e.g., 80%)
    [SerializeField] private float _minScaleY = 0.8f; // Minimum scale (e.g., 80%)
    [SerializeField] private float _maxScale = 1.2f; // Maximum scale (e.g., 120%)
    [SerializeField] private float _duration = 1.0f; // Time to go from-to min to max

    private Vector3 originalScale;
    private bool _isActive = false;
    private Coroutine _pulseRoutine;
    private Transform _target;

    public void Activate(Transform target)
    {
        _target = target;
        originalScale = _target.localScale;

        if (_pulseRoutine != null)
        {
            Deactivate();
        }

        _isActive = true;
        _pulseRoutine = StartCoroutine(StartPulse());
    }

    public void Deactivate()
    {
        _isActive = false;
        StopCoroutine(StartPulse());
        _pulseRoutine = null;
        _target.localScale = originalScale;
    }

    private IEnumerator StartPulse()
    {
        while (_isActive)
        {
            // Pulse X
            yield return PulseAxis(true, false);
            // Pulse Y
            yield return PulseAxis(false, true);
        }

        _pulseRoutine = null;
    }

    private IEnumerator PulseAxis(bool scaleX, bool scaleY)
    {
        float t = 0;
        Vector3 start = _target.localScale;

        Vector3 target = new Vector3(
            scaleX ? start.x * _minScaleX : start.x,
            scaleY ? start.y * _minScaleY : start.y,
            start.z
        );

        // Shrink
        while (t < _duration)
        {
            t += Time.deltaTime;
            _target.localScale = Vector3.Lerp(start, target, t / _duration);
            yield return null;
        }

        // Grow back
        t = 0;
        while (t < _duration)
        {
            t += Time.deltaTime;
            _target.localScale = Vector3.Lerp(target, start, t / _duration);
            yield return null;
        }
    }
}