using UnityEngine;

[DefaultExecutionOrder(210)]
[DisallowMultipleComponent]
public sealed class CatTrackIndicator : MonoBehaviour
{
    public enum IndicatorState { Hidden, Waiting, Ready, Parked, Bumped, Exiting }
    public enum ExitReaction { Heart, HeartEyes, ThumbsUp }

    private SpriteRenderer _cat;
    private Vector2 _faceAnchor;
    private Camera _camera;
    private RectTransform _bubble;
    private CatTrackBubbleGraphic _graphic;
    private Canvas _canvas;
    private CanvasGroup _group;
    private float _shownAt;
    private float _stateChangedAt;
    private float _phase;
    private System.Random _visualRandom;
    private float _nextThoughtAt;
    private float _thoughtUntil;
    private bool _thoughtVisible;
    private float _bumpUntil;
    private IndicatorState _stateBeforeBump;

    public IndicatorState State { get; private set; }
    public ExitReaction Reaction { get; private set; }

    public void Initialize(SpriteRenderer cat, Vector2 faceAnchor)
    {
        _cat = cat;
        _faceAnchor = faceAnchor;
        _camera = Camera.main;
        // Keep cosmetic animation from consuming the gameplay random sequence.
        _phase = Mathf.Abs(GetInstanceID() % 997) * 0.071f;
        _visualRandom = new System.Random(GetInstanceID());
        Hide();
        ShowParked();
        _nextThoughtAt = Time.time + IdleRange(0.5f, 4.5f);
    }

    public void ShowParked() => Show(IndicatorState.Parked);
    public void ShowWaiting() => Show(IndicatorState.Waiting);
    public void ShowReady() => Show(IndicatorState.Ready);

    public void ShowExitReaction()
    {
        if (_cat == null || State == IndicatorState.Exiting)
            return;

        Reaction = (ExitReaction)_visualRandom.Next(3);
        Show(IndicatorState.Exiting);
    }

    public void ShowBump()
    {
        // A reaction must not overwrite a gameplay state changed while the coroutine was waiting.
        if (State == IndicatorState.Hidden || State == IndicatorState.Exiting || _cat == null)
            return;

        if (State != IndicatorState.Bumped)
            _stateBeforeBump = State;

        _bumpUntil = Time.time + 0.9f;
        _stateChangedAt = Time.time;
        Show(IndicatorState.Bumped);
    }

    public void Hide()
    {
        State = IndicatorState.Hidden;
        _thoughtVisible = false;

        if (_bubble != null)
            _bubble.gameObject.SetActive(false);

        enabled = false;
    }

    private void Show(IndicatorState state)
    {
        if (_cat == null || State == state)
            return;

        State = state;
        _stateChangedAt = Time.time;
        _thoughtVisible = false;
        enabled = true;

        if (state == IndicatorState.Parked)
        {
            if (_bubble != null)
                _bubble.gameObject.SetActive(false);

            _nextThoughtAt = Time.time + IdleRange(3f, 6f);
            return;
        }

        RevealBubble(state == IndicatorState.Ready || state == IndicatorState.Bumped);
        LateUpdate();
    }

    private float IdleRange(float min, float max) =>
        Mathf.Lerp(min, max, (float)_visualRandom.NextDouble());

    private void RevealBubble(bool immediate)
    {
        if (_bubble == null)
            CreateBubble();

        if (_bubble.gameObject.activeSelf == false)
            _shownAt = Time.time;

        if (immediate)
            _shownAt = Time.time - 0.16f;

        _bubble.gameObject.SetActive(true);
    }

    private void CreateBubble()
    {
        GameObject visual = new GameObject("Track status bubble", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasGroup), typeof(CatTrackBubbleGraphic));
        visual.layer = _cat.gameObject.layer;
        _bubble = visual.GetComponent<RectTransform>();
        _bubble.SetParent(transform, false);
        _bubble.sizeDelta = new Vector2(128f, 100f);
        _bubble.pivot = new Vector2(26f / 128f, 10f / 100f);
        _canvas = visual.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.overrideSorting = true;
        _group = visual.GetComponent<CanvasGroup>();
        _group.interactable = false;
        _group.blocksRaycasts = false;
        _graphic = visual.GetComponent<CatTrackBubbleGraphic>();
        _graphic.raycastTarget = false;
        visual.SetActive(false);
    }

    private void LateUpdate()
    {
        if (State == IndicatorState.Hidden)
            return;

        if (_cat == null || _cat.enabled == false || _cat.gameObject.activeInHierarchy == false)
        {
            Hide();
            return;
        }

        if (State == IndicatorState.Bumped && Time.time >= _bumpUntil)
        {
            Show(_stateBeforeBump);
            return;
        }

        if (State == IndicatorState.Parked)
        {
            if (_thoughtVisible == false)
            {
                if (Time.time < _nextThoughtAt)
                    return;

                _thoughtVisible = true;
                _thoughtUntil = Time.time + IdleRange(1.8f, 2.6f);
                RevealBubble(false);
            }
            else if (Time.time >= _thoughtUntil)
            {
                _thoughtVisible = false;
                _bubble.gameObject.SetActive(false);
                _nextThoughtAt = Time.time + IdleRange(3f, 6f);
                return;
            }
        }

        if (_camera == null)
            _camera = Camera.main;

        if (_camera == null || _cat.sprite == null)
            return;

        Transform view = _camera.transform;
        Vector3 extents = _cat.sprite.bounds.extents;
        Vector3 spriteX = _cat.transform.TransformVector(Vector3.right * extents.x);
        Vector3 spriteY = _cat.transform.TransformVector(Vector3.up * extents.y);
        float bodyWidth = Mathf.Min(spriteX.magnitude, spriteY.magnitude) * 2f;
        float width = Mathf.Clamp(bodyWidth * 0.42f, 0.85f, 1.2f);
        float depth = Vector3.Dot(_cat.bounds.center - view.position, view.forward);
        float viewHeight;

        if (_camera.orthographic)
        {
            viewHeight = _camera.orthographicSize * 2f;
        }
        else
        {
            viewHeight = 2f * Mathf.Max(0f, depth) * Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
        }

        width = Mathf.Max(width, Mathf.Min(1.6f, viewHeight * 20f / Mathf.Max(1, _camera.pixelHeight)));

        // SpriteRenderer flips do not affect TransformPoint, so apply them explicitly.
        Vector2 facePixels = Vector2.Scale(_faceAnchor, _cat.sprite.rect.size) - _cat.sprite.pivot;
        Vector3 localFace = new Vector3(facePixels.x, facePixels.y, 0f) / _cat.sprite.pixelsPerUnit;

        if (_cat.flipX)
            localFace.x = -localFace.x;

        if (_cat.flipY)
            localFace.y = -localFace.y;

        Vector3 face = _cat.transform.TransformPoint(localFace);

        float age = Time.time - _shownAt;
        float stateAge = Time.time - _stateChangedAt;
        bool ready;

        if (State == IndicatorState.Ready)
        {
            ready = true;
        }
        else
        {
            ready = false;
        }

        float appear = Mathf.Clamp01(age / 0.16f);
        float scale = Mathf.Lerp(0.65f, 1f, Mathf.Sin(appear * Mathf.PI * 0.5f));

        if (ready)
            scale *= 1f + 0.16f * Mathf.Sin(Mathf.Clamp01(stateAge / 0.28f) * Mathf.PI);
        else if (State == IndicatorState.Bumped)
            scale *= 1f + 0.1f * Mathf.Sin(Mathf.Clamp01(stateAge / 0.24f) * Mathf.PI);
        else if (State == IndicatorState.Exiting)
            scale *= 1f + 0.12f * Mathf.Sin(Mathf.Clamp01(stateAge / 0.32f) * Mathf.PI);

        float bob = Mathf.Sin(Time.time * 3.2f + _phase) * width * 0.025f;
        _bubble.position = face + view.up * (width * 0.12f + bob)
            + view.right * width * 0.16f - view.forward * 0.15f;
        float tilt;

        if (ready)
        {
            tilt = 0f;
        }
        else
        {
            tilt = Mathf.Sin(Time.time * 2.2f + _phase) * 2f;
        }

        _bubble.rotation = view.rotation * Quaternion.Euler(0f, 0f, tilt);

        _bubble.localScale = Vector3.one;
        Vector3 inheritedScale = _bubble.lossyScale;
        float unit = width * scale / 128f;
        _bubble.localScale = new Vector3(unit / Mathf.Max(0.0001f, Mathf.Abs(inheritedScale.x)),
            unit / Mathf.Max(0.0001f, Mathf.Abs(inheritedScale.y)),
            unit / Mathf.Max(0.0001f, Mathf.Abs(inheritedScale.z)));
        _group.alpha = Mathf.SmoothStep(0f, 1f, appear);

        if (State == IndicatorState.Parked)
            _group.alpha *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_thoughtUntil - Time.time) / 0.3f));

        _canvas.worldCamera = _camera;
        _canvas.sortingLayerID = _cat.sortingLayerID;
        _canvas.sortingOrder = _cat.sortingOrder + 20;
        _graphic.SetAnimation(State, Time.time * 2.5f + _phase, Reaction);
    }

    private void OnDisable()
    {
        Hide();
    }
}
