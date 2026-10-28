using System;
using UnityEngine;

public class NudgeAnimation : MonoBehaviour
{
    [Header("Nudge")]
    [Tooltip("Duration of one nudge in seconds.")]
    [SerializeField] private float _duration = 0.15f;
    [Tooltip("Peak offset toward impact direction, in local units.")]
    [SerializeField]
    [Range(0f, 1f)]
    private float _offsetAmount = 0.2f;
    [Tooltip("Shape of the nudge over normalized time 0→1. Y=+1 = full lunge along impact axis.")]
    [SerializeField] private AnimationCurve _nudgeCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.35f, 1f),
        new Keyframe(1f, 0f));

    private Transform _target;
    private Vector3 _restPos;
    private bool _isActive;
    private float _elapsed;
    private Vector3 _offsetDir;

    public event Action Finished;

    public void Activate(Transform target, Vector2 impactDir)
    {
        const float minimumImpactSqrMagnitude = 1e-6f;

        if (_isActive)
            Deactivate();

        _target = target;
        _restPos = _target.localPosition;

        if (impactDir.sqrMagnitude > minimumImpactSqrMagnitude)
            impactDir.Normalize();

        // impactDir uses the grid plane: x = world X, y = world Z.
        Vector3 worldDir = new Vector3(impactDir.x, 0f, impactDir.y);

        if (_target.parent != null)
        {
            _offsetDir = _target.parent.InverseTransformDirection(worldDir);
        }
        else
        {
            _offsetDir = worldDir;
        }

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
            _target.localPosition = _restPos;
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
            _target.localPosition = _restPos;
            _isActive = false;
            Finished?.Invoke();
            return;
        }

        float s = _nudgeCurve.Evaluate(t) * _offsetAmount;
        _target.localPosition = _restPos + (_offsetDir * s);
    }

    private void Reset()
    {
        for (int i = 0; i < _nudgeCurve.length; i++)
            _nudgeCurve.SmoothTangents(i, 0f);
    }
}
