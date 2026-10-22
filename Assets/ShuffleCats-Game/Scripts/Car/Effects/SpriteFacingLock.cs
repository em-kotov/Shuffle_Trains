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

    // CarHead.forward can be perpendicular to the parking axis; use the grid direction.
    public void SetParkingDirection(Vector3 direction, bool artFacesRight)
    {
        if (_apply == false || _target == null || _spriteRenderer == null)
            return;

        Vector3 facingAxis;

        if (artFacesRight)
        {
            facingAxis = Vector3.right;
        }
        else
        {
            facingAxis = Vector3.left;
        }

        Vector3 artDirection = _restWorldRotation * facingAxis;

        if (Vector3.Dot(artDirection, direction) < 0f)
        {
            _parkingFlip = true;
        }
        else
        {
            _parkingFlip = false;
        }

        _hasParkingFacing = true;
        _spriteRenderer.flipX = _parkingFlip;
    }

    public void SetInvertFlip(bool invert)
    {
        _invertFlip = invert;
    }

    public void SetTrackFlip(bool trackFlipSeparate, bool invertFlipTrack)
    {
        _trackFlipSeparate = trackFlipSeparate;
        _invertFlipTrack = invertFlipTrack;
    }

    public void SwitchToTrackFlip()
    {
        _onTrack = true;
    }

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

        _restWorldRotation = _target.rotation;
    }

    private void LateUpdate()
    {
        if (_apply == false || _target == null)
            return;

        // Deriving from localRotation would feed the previous world-space correction back into itself.
        _target.rotation = _restWorldRotation;

        if (_onTrack == false && _hasParkingFacing)
        {
            _spriteRenderer.flipX = _parkingFlip;
            return;
        }

        if (_directionSource == null || _spriteRenderer == null)
            return;

        float dirX = _directionSource.forward.x;

        if (Mathf.Abs(dirX) < _directionDeadZone)
            return;

        // Ignore the entry slide's small lateral component; its sign can spuriously mirror the sprite.
        if (_onTrack == false && Mathf.Abs(dirX) < Mathf.Abs(_directionSource.forward.z))
            return;

        bool invert;

        if ((_onTrack && _trackFlipSeparate))
        {
            invert = _invertFlipTrack;
        }
        else
        {
            invert = _invertFlip;
        }

        if (invert)
        {
            if (dirX > 0f)
            {
                _spriteRenderer.flipX = true;
            }
            else
            {
                _spriteRenderer.flipX = false;
            }
        }
        else
        {
            if (dirX < 0f)
            {
                _spriteRenderer.flipX = true;
            }
            else
            {
                _spriteRenderer.flipX = false;
            }
        }
    }
}
