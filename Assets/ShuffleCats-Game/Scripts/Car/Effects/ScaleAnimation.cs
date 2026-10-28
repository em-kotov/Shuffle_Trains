using UnityEngine;

public class ScaleAnimation : MonoBehaviour
{
    [Header("Breath")]
    [Tooltip("Breaths per second.")]
    [SerializeField] private float _frequency = 0.5f;
    [Tooltip("Peak squash/stretch as a fraction of rest scale (0.10 = ±10%).")]
    [SerializeField]
    [Range(0f, 0.5f)]
    private float _squashAmount = 0.10f;
    [Tooltip("Shape of one breath cycle. X: 0→1 normalized phase. Y: -1 (squash X / stretch Y) to +1 (stretch X / squash Y). Asymmetric keys give quick squash, slow release.")]
    [SerializeField] private AnimationCurve _breathCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.18f, 1f),
        new Keyframe(0.5f, 0f),
        new Keyframe(0.68f, -1f),
        new Keyframe(1f, 0f));

    [Header("Per-Instance Variation")]
    [Tooltip("Randomizes frequency ±this fraction per instance so cats don't pulse in sync.")]
    [SerializeField]
    [Range(0f, 0.3f)]
    private float _frequencyJitter = 0.12f;
    [Tooltip("Randomizes amplitude ±this fraction per instance.")]
    [SerializeField]
    [Range(0f, 0.3f)]
    private float _amplitudeJitter = 0.12f;

    private Transform _target;
    private Vector3 _restScale;
    private bool _isActive;

    private float _phaseOffset;
    private float _freqMul;
    private float _ampMul;

    public void Activate(Transform target)
    {
        if (_isActive)
            Deactivate();

        _target = target;
        _restScale = _target.localScale;

        _phaseOffset = Random.value;
        _freqMul = 1f + Random.Range(-_frequencyJitter, _frequencyJitter);
        _ampMul = 1f + Random.Range(-_amplitudeJitter, _amplitudeJitter);

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

        float cycle = Mathf.Repeat((Time.time * _frequency * _freqMul) + _phaseOffset, 1f);
        float s = _breathCurve.Evaluate(cycle);
        float amp = _squashAmount * _ampMul;

        float sx = 1f - (s * amp);
        float sy = 1f + (s * amp);
        _target.localScale = new Vector3(_restScale.x * sx, _restScale.y * sy, _restScale.z);
    }

    private void Reset()
    {
        for (int i = 0; i < _breathCurve.length; i++)
            _breathCurve.SmoothTangents(i, 0f);
    }
}
