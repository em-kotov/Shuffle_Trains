using System;
using UnityEngine;

public class BumpAnimation : MonoBehaviour
{
    [Header("Bump")]
    [Tooltip("Duration of one bump in seconds.")]
    [SerializeField] private float _duration = 0.3f;
    [Tooltip("Peak squash as a fraction of rest scale (0.20 = ±20%).")]
    [SerializeField]
    [Range(0f, 0.5f)]
    private float _squashAmount = 0.18f;
    [Tooltip("Shape of the bump over normalized time 0→1. Y=+1 squashes along impact axis (stretches perpendicular). Negative dips create an overshoot before settling.")]
    [SerializeField] private AnimationCurve _bumpCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.18f, 1f),
        new Keyframe(0.55f, -0.35f),
        new Keyframe(1f, 0f));

    private Transform _target;
    private Vector3 _restScale;
    private bool _isActive;
    private float _elapsed;
    private float _axisBias;

    public event Action Finished;

    public void Activate(Transform target, Vector2 impactDir)
    {
        const float minimumImpactSqrMagnitude = 1e-6f;

        if (_isActive)
            Deactivate();

        _target = target;
        _restScale = _target.localScale;

        if (impactDir.sqrMagnitude > minimumImpactSqrMagnitude)
            impactDir.Normalize();

        // Axis bias is +1 horizontally, -1 vertically, and zero at 45 degrees (no squash).
        _axisBias = (impactDir.x * impactDir.x) - (impactDir.y * impactDir.y);

        _elapsed = 0f;
        _isActive = true;
    }

    public void Deactivate()
    {
        if (_isActive == false)
            return;

        _isActive = false;

        if (_target != null)
        {
            _target.localScale = _restScale;
        }
    }

    private void LateUpdate()
    {
        if (_isActive == false || _target == null)
            return;

        _elapsed += Time.deltaTime;
        float t = _elapsed / _duration;

        if (t >= 1f)
        {
            _target.localScale = _restScale;
            _isActive = false;
            Finished?.Invoke();
            return;
        }

        float s = _bumpCurve.Evaluate(t) * _squashAmount * _axisBias;
        float sx = 1f - s;
        float sy = 1f + s;
        _target.localScale = new Vector3(_restScale.x * sx, _restScale.y * sy, _restScale.z);
    }

    private void Reset()
    {
        for (int i = 0; i < _bumpCurve.length; i++)
            _bumpCurve.SmoothTangents(i, 0f);
    }
}
