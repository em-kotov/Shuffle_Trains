using UnityEngine;

// Some cats sit "vertically" on the parking grid while their sprite still faces
// horizontally. Once the cat successfully enters the track it should snap back to
// a normal horizontal look, so we spin it a quarter turn on the Y axis.
//
// Wire both fields manually so this can sit on the root: the spin is routed
// through SpriteFacingLock (which pins the sprite's world rotation every frame),
// falling back to rotating _target directly for cats with no facing lock.
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

    // Set by the level builder from captured per-instance layout data, so a built
    // cat reproduces the rotation fix it was authored with in the scene.
    public void SetConfig(bool applyRotation, bool rotateLeft)
    {
        _applyRotation = applyRotation;
        _rotateLeft = rotateLeft;
    }

    // Called once when the cat successfully entered the track.
    public void ApplyFix()
    {
        if (_applyRotation == false)
            return;

        float angle = _rotateLeft ? 90f : -90f;

        // Preferred path: spin the locked facing so the rotation actually shows
        // (rotating a transform directly is overridden by SpriteFacingLock).
        if (_facingLock != null)
        {
            _facingLock.ApplyRestSpin(angle);
            return;
        }

        // Fallback for cats with no facing lock: rotate the target transform.
        if (_target != null)
        {
            Vector3 euler = _target.localEulerAngles;
            euler.y = angle;
            _target.localEulerAngles = euler;
            return;
        }

        Debug.LogWarning($"{nameof(RotationFixer)} on '{name}': assign a SpriteFacingLock " +
            "or a Target in the inspector — there is nothing to rotate.", this);
    }
}
