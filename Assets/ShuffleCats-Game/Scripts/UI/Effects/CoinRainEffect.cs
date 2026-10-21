using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Eye-candy coin rain played when a level is won. Subscribes to AutoWin.WinLevel
// and spawns a burst of UI coin Images that fall from just above the top of the
// spawn area down past the bottom, with a little horizontal drift and spin.
//
// Setup: put this on a full-screen (stretched) RectTransform under the Canvas,
// assign the AutoWin in the scene. The coin sprite auto-loads from
// Resources/Textures/Icons/coin-icon-1, or assign an override below.
// Coins are pure decoration: raycastTarget is off, so they never block the Win UI.
[RequireComponent(typeof(RectTransform))]
public class CoinRainEffect : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("Fires the rain when this raises WinLevel. Leave empty and call Play() manually if you prefer.")]
    [SerializeField] private AutoWin _autoWin;

    [Header("Coin")]
    [Tooltip("Optional override. If empty, loads Resources/Textures/Icons/coin-icon-1.")]
    [SerializeField] private Sprite _coinSprite;
    [Tooltip("Area the coins spawn/fall in. If empty, uses this object's RectTransform (make it full-screen).")]
    [SerializeField] private RectTransform _spawnArea;

    [Header("Amounts & size")]
    [SerializeField, Min(1)] private int _coinCount = 30;
    [Tooltip("Coin width/height in canvas pixels (a random size in this range is picked per coin).")]
    [SerializeField] private Vector2 _sizeRange = new Vector2(70f, 100f);
    [Tooltip("How long the whole burst takes to spawn. 0 = all at once.")]
    [SerializeField, Min(0f)] private float _spawnDuration = 0.6f;

    [Header("Fall")]
    [Tooltip("Initial downward speed in pixels/second (random per coin).")]
    [SerializeField] private Vector2 _fallSpeedRange = new Vector2(600f, 1100f);
    [Tooltip("Downward acceleration in pixels/second^2 (gravity feel).")]
    [SerializeField] private float _gravity = 1200f;

    [Header("Drift & spin")]
    [Tooltip("Horizontal sway amplitude in pixels.")]
    [SerializeField] private float _driftAmplitude = 40f;
    [Tooltip("Horizontal sway speed in radians/second.")]
    [SerializeField] private float _driftFrequency = 3f;
    [Tooltip("Spin speed in degrees/second (random per coin, random direction).")]
    [SerializeField] private Vector2 _spinSpeedRange = new Vector2(90f, 360f);

    [Header("Audio")]
    [SerializeField] private bool _playCoinSound = true;

    private const string CoinResourcePath = "Textures/Icons/coin-icon-1";

    private void Awake()
    {
        if (_spawnArea == null)
            _spawnArea = (RectTransform)transform;

        if (_coinSprite == null)
            _coinSprite = Resources.Load<Sprite>(CoinResourcePath);
    }

    private void OnEnable()
    {
        if (_autoWin != null)
            _autoWin.WinLevel += Play;
    }

    private void OnDisable()
    {
        if (_autoWin != null)
            _autoWin.WinLevel -= Play;
    }

    // Public so it can also be fired from the inspector/other scripts or for testing.
    public void Play()
    {
        if (_coinSprite == null)
        {
            Debug.LogWarning($"CoinRainEffect - no coin sprite (expected at Resources/{CoinResourcePath}).");
            return;
        }

        if (_playCoinSound)
            AudioManager.Instance?.PlayCoinBonus();

        StartCoroutine(SpawnBurst());
    }

    private IEnumerator SpawnBurst()
    {
        float halfWidth = _spawnArea.rect.width * 0.5f;
        float halfHeight = _spawnArea.rect.height * 0.5f;
        float gap = _coinCount > 1 ? _spawnDuration / (_coinCount - 1) : 0f;

        for (int i = 0; i < _coinCount; i++)
        {
            SpawnCoin(halfWidth, halfHeight);

            if (gap > 0f)
                yield return new WaitForSeconds(gap);
        }
    }

    private void SpawnCoin(float halfWidth, float halfHeight)
    {
        float size = Random.Range(_sizeRange.x, _sizeRange.y);

        var go = new GameObject("Coin", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(_spawnArea, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);

        var image = go.GetComponent<Image>();
        image.sprite = _coinSprite;
        image.raycastTarget = false; // decoration only — never intercept clicks on the Win UI
        image.preserveAspect = true;

        // Start just above the top edge at a random column; fall to just below the bottom.
        float startX = Random.Range(-halfWidth, halfWidth);
        float startY = halfHeight + size;
        float endY = -halfHeight - size;
        rect.anchoredPosition = new Vector2(startX, startY);

        float fallSpeed = Random.Range(_fallSpeedRange.x, _fallSpeedRange.y);
        float spin = Random.Range(_spinSpeedRange.x, _spinSpeedRange.y) * (Random.value < 0.5f ? -1f : 1f);
        float driftPhase = Random.Range(0f, Mathf.PI * 2f);

        StartCoroutine(FallRoutine(rect, startX, startY, endY, fallSpeed, spin, driftPhase));
    }

    private IEnumerator FallRoutine(RectTransform coin, float startX, float startY, float endY,
                                    float fallSpeed, float spinSpeed, float driftPhase)
    {
        float y = startY;
        float velocity = fallSpeed;
        float elapsed = 0f;

        while (y > endY)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            velocity += _gravity * dt;
            y -= velocity * dt;

            float x = startX + _driftAmplitude * Mathf.Sin(_driftFrequency * elapsed + driftPhase);
            coin.anchoredPosition = new Vector2(x, y);
            coin.Rotate(0f, 0f, spinSpeed * dt);

            yield return null;
        }

        Destroy(coin.gameObject);
    }
}
