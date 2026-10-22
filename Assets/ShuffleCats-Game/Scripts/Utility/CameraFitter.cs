using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class CameraFitter : MonoBehaviour
{
    [Tooltip("The authored orthographic size. Wide viewports keep exactly this, so the desktop framing you tuned stays untouched.")]
    [SerializeField] private float _baseSize = 7f;
    [Tooltip("World-space X extent that must stay visible (the playfield's width, background sprite included). The camera only widens once the viewport can no longer show this.")]
    [SerializeField] private float _minVisibleWidth = 12f;
    [Tooltip("Extra margin around the fitted width, as a fraction. 0.1 leaves 10% breathing room so cats never sit flush against the screen edge.")]
    [SerializeField] private float _padding = 0.08f;

    private Camera _camera;
    private int _lastWidth;
    private int _lastHeight;
    private Vector3 _authoredPosition;
    private Rect _gameplayArea = new Rect(0, 0, 1, 1);
    private float _verticalAlignment = 0.5f;
    private ThemeApplier _theme;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _authoredPosition = transform.position;
    }

    private void OnEnable()
    {
        // Invalidate cached resolution so re-enabling always refits the camera.
        _lastWidth = 0;
        _lastHeight = 0;
    }

    private void Update()
    {
        if (Screen.width == _lastWidth && Screen.height == _lastHeight)
            return;

        _lastWidth = Screen.width;
        _lastHeight = Screen.height;
        Fit();
    }

    private void Fit()
    {
        if (_camera == null)
            _camera = GetComponent<Camera>();

        if (_camera.orthographic == false)
        {
            Debug.LogWarning($"{nameof(CameraFitter)} on '{name}': camera is not " +
                "orthographic, so there is no orthographic size to fit. Component does nothing.", this);
            return;
        }

        if (_camera.aspect <= 0f)
            return;

        float sizeForWidth = (_minVisibleWidth * (1f + _padding) * 0.5f)
            / (_camera.aspect * Mathf.Max(0.01f, _gameplayArea.width));

        _camera.orthographicSize = Mathf.Max(_baseSize / Mathf.Max(0.01f, _gameplayArea.height), sizeForWidth);

        if (Application.isPlaying)
        {
            Vector2 offset = _gameplayArea.center - Vector2.one * 0.5f;
            float spareHeight = Mathf.Max(0, _gameplayArea.height - _baseSize / _camera.orthographicSize);
            offset.y += (_verticalAlignment - 0.5f) * spareHeight;
            transform.position = _authoredPosition
                - transform.right * (offset.x * 2 * _camera.orthographicSize * _camera.aspect)
                - transform.up * (offset.y * 2 * _camera.orthographicSize);

            if (_theme == null)
                _theme = FindObjectOfType<ThemeApplier>();

            if (_theme != null)
                _theme.FitBackgroundToCamera(_camera);
        }
    }

    public void SetGameplayArea(Rect area, float verticalAlignment = 0.5f)
    {
        _gameplayArea = area;
        _verticalAlignment = Mathf.Clamp01(verticalAlignment);
        Fit();
    }

    private void OnValidate()
    {
        _baseSize = Mathf.Max(0.01f, _baseSize);
        _minVisibleWidth = Mathf.Max(0.01f, _minVisibleWidth);
        _padding = Mathf.Max(0f, _padding);

        if (isActiveAndEnabled)
            Fit();
    }
}
