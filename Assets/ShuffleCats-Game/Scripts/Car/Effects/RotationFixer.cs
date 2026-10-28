using UnityEngine;

public class RotationFixer : MonoBehaviour
{
    [Tooltip("Sprite transform to rotate when no SpriteFacingLock is wired (usually CatTexture).")]
    [SerializeField] private Transform _target;
    [Tooltip("The cat's SpriteFacingLock. The spin goes through it so it survives the per-frame rotation lock. Wire manually.")]
    [SerializeField] private SpriteFacingLock _facingLock;
    [Tooltip("Enable to rotate the target on track entry. Disable to leave it untouched.")]
    [SerializeField] private bool _applyRotation;
    [Tooltip("Rotate +90 on Y when on, -90 when off.")]
    [SerializeField] private bool _rotateLeft;

    public void SetConfig(bool applyRotation, bool rotateLeft)
    {
        _applyRotation = applyRotation;
        _rotateLeft = rotateLeft;
    }

    public void ApplyFix()
    {
        const float quarterTurnDegrees = 90f;

        if (_applyRotation == false)
            return;

        float angle;

        if (_rotateLeft)
        {
            angle = quarterTurnDegrees;
        }
        else
        {
            angle = -quarterTurnDegrees;
        }

        // Direct transform rotation would be overwritten by SpriteFacingLock.
        if (_facingLock != null)
        {
            _facingLock.ApplyRestSpin(angle);
            return;
        }

        if (_target != null)
        {
            Vector3 euler = _target.localEulerAngles;
            euler.y = angle;
            _target.localEulerAngles = euler;
            return;
        }

        Debug.LogWarning(
            $"{nameof(RotationFixer)} on '{name}': assign a SpriteFacingLock " +
            "or a Target in the inspector — there is nothing to rotate.",
            this);
    }
}
