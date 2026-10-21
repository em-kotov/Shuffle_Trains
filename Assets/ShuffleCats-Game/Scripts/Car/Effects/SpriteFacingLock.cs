using UnityEngine;

[DefaultExecutionOrder(100)]
public class SpriteFacingLock : MonoBehaviour
{
    [Tooltip("Enable to lock the sprite's facing. Off leaves the sprite untouched.")]
    [SerializeField] private bool _apply;
    [Tooltip("Sprite transform whose world rotation is held constant (usually CatTexture). Wire this manually so the component can live on the root.")]
    [SerializeField] private Transform _target;
    [Tooltip("Transform whose forward direction decides left/right flipping (usually CarHead).")]
    [SerializeField] private Transform _directionSource;
    [Tooltip("Sprite to flip horizontally based on movement direction.")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [Tooltip("Enable if your sprite art faces the opposite way at rest. Used while parked (and on the track too, unless Track Flip Separate is on).")]
    [SerializeField] private bool _invertFlip;
    [Tooltip("Give the track its own flip instead of reusing the parking flip above.")]
    [SerializeField] private bool _trackFlipSeparate;
    [Tooltip("Flip applied once the cat is on the track. Only used when Track Flip Separate is on.")]
    [SerializeField] private bool _invertFlipTrack;
    [Tooltip("Movement is ignored when |forward.x| is below this, so the cat doesn't flicker at near-vertical headings.")]
    [SerializeField] private float _directionDeadZone = 0.05f;

    private Quaternion _restWorldRotation;
    private bool _onTrack;
    private bool _hasParkingFacing;
    private bool _parkingFlip;

    // A single-cell cat's artwork faces along its sprite X axis, while its
    // CarHead.forward can be perpendicular to the parking movement axis.
    // Resolve the flip from the grid direction, then keep it during the slide
    // onto the track (where CarHead starts turning toward the spline).
    public void SetParkingDirection(Vector3 direction, bool artFacesRight)
    {
        if (!_apply || _target == null || _spriteRenderer == null)
            return;

        Vector3 artDirection = _restWorldRotation * (artFacesRight ? Vector3.right : Vector3.left);
        _parkingFlip = Vector3.Dot(artDirection, direction) < 0f;
        _hasParkingFacing = true;
        _spriteRenderer.flipX = _parkingFlip;
    }

    // Set by the level builder from captured per-instance layout data, so a cat
    // reproduces the facing it had in the authored scene.
    public void SetInvertFlip(bool invert)
    {
        _invertFlip = invert;
    }

    // Set by the level builder from captured per-instance layout data, so a built
    // cat reproduces the parking-vs-track flip split it was authored with.
    public void SetTrackFlip(bool trackFlipSeparate, bool invertFlipTrack)
    {
        _trackFlipSeparate = trackFlipSeparate;
        _invertFlipTrack = invertFlipTrack;
    }

    // Called once the cat has entered the track, so the flip can switch from the
    // parking value to the track value (only matters when Track Flip Separate is on).
    public void SwitchToTrackFlip()
    {
        _onTrack = true;
    }

    // Spins the locked facing about world up. The target's world rotation is
    // pinned to _restWorldRotation every frame, so rotating any transform has no
    // visible effect — re-orienting the sprite has to go through this. Used by
    // RotationFixer to snap a vertically-parked cat back to a horizontal look on
    // track entry. Safe to call after Awake (the rest snapshot already exists).
    public void ApplyRestSpin(float yDegrees)
    {
        _restWorldRotation = Quaternion.AngleAxis(yDegrees, Vector3.up) * _restWorldRotation;
    }

    private void Awake()
    {
        if (_apply == false)
            return;

        if (_target == null)
        {
            Debug.LogWarning($"{nameof(SpriteFacingLock)} on '{name}': Target is not " +
                "assigned. Wire the cat's sprite transform in the inspector.", this);
            return;
        }

        // Snapshot the target's world rotation once. We re-apply it every frame so
        // the parent's rotation (from SplineAnimate / LookAt) never affects it.
        _restWorldRotation = _target.rotation;
    }

    private void LateUpdate()
    {
        if (_apply == false || _target == null) return;

        // Lock world rotation. Do NOT derive this from localRotation — setting
        // world rotation back-solves a new localRotation, and reading that mutated
        // value next frame creates a compounding feedback loop that makes the
        // sprite spin once the parent's rotation differs from rest.
        _target.rotation = _restWorldRotation;

        if (!_onTrack && _hasParkingFacing)
        {
            _spriteRenderer.flipX = _parkingFlip;
            return;
        }

        if (_directionSource == null || _spriteRenderer == null) return;

        float dirX = _directionSource.forward.x;
        if (Mathf.Abs(dirX) < _directionDeadZone) return;

        // While off the track, only a horizontally-dominant heading (a parked/idle
        // cat) may set the facing. The brief slide onto the track points almost
        // straight at the entry point, so its tiny sideways component — whose sign
        // depends only on which side the nearest track point falls — must not mirror
        // the sprite (that read as an upside-down cat for columns entering from -X).
        if (_onTrack == false && Mathf.Abs(dirX) < Mathf.Abs(_directionSource.forward.z))
            return;

        // Track keeps the parking flip unless it was given its own.
        bool invert = (_onTrack && _trackFlipSeparate) ? _invertFlipTrack : _invertFlip;
        _spriteRenderer.flipX = invert ? dirX > 0f : dirX < 0f;
    }
}
