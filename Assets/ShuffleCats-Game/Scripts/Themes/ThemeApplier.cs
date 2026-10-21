using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThemeApplier : MonoBehaviour
{
    public enum StationSpriteVariant
    {
        A,
        B,
    }

    [Serializable]
    public class StationEntry
    {
        public SpriteRenderer Target;
        public StationSpriteVariant Sprite;
        public bool FlipX;
        public bool FlipY;
    }

    [SerializeField] private ShopCatalog _catalog;

    [Header("Background")]
    [SerializeField] private SpriteRenderer _backgroundTarget;

    [Header("Stations")]
    [SerializeField] private List<StationEntry> _stations = new List<StationEntry>();

    [Header("Play Area")]
    [SerializeField] private SpriteRenderer _playAreaTarget;

    [Header("Grid")]
    [SerializeField] private SpriteRenderer _gridTarget;

    [Header("Gradient")]
    [SerializeField] private SpriteRenderer _gradientTarget;

    [Header("UI Background Colors")]
    [SerializeField] private List<Image> _uiBgPrimaryTargets = new List<Image>();
    [SerializeField] private List<Image> _uiBgSecondaryTargets = new List<Image>();
    [SerializeField] private List<Image> _uiBgContourTargets = new List<Image>();
    [SerializeField] private List<Image> _uiBgShadowTargets = new List<Image>();

    [Header("UI Text Colors")]
    [SerializeField] private List<TMP_Text> _uiTextPrimaryTargets = new List<TMP_Text>();
    [SerializeField] private List<TMP_Text> _uiTextSecondaryTargets = new List<TMP_Text>();
    [SerializeField] private List<TMP_Text> _uiTextAdditionalTargets = new List<TMP_Text>();

    private Shop _shop;
    private Quaternion _playAreaBaseRotation;
    private bool _playAreaBaseCached;
    private Vector2 _backgroundBaseSize;

    // Used when stations are spawned at runtime (LevelBuilder) instead of being
    // hand-placed: the serialized _stations list points at the old scene
    // stations, so it must be rebuilt to target the freshly spawned ones.
    public void ClearStations()
    {
        _stations.Clear();
    }

    public void AddStation(SpriteRenderer target, StationSpriteVariant variant,
                        bool flipX, bool flipY)
    {
        _stations.Add(new StationEntry
        {
            Target = target,
            Sprite = variant,
            FlipX = flipX,
            FlipY = flipY
        });
    }

    public void RegisterStations(IEnumerable<Station> stations)
    {
        foreach (Station station in stations)
        {
            SpriteRenderer target = station != null ? station.ThemeTarget : null;
            if (target == null || _stations.Exists(entry => entry != null && entry.Target == target))
            {
                continue;
            }

            // Unlisted scene stations use the station prefab's A variant.
            // Keep their authored flips; upper stations retain explicit overrides.
            AddStation(target, StationSpriteVariant.A, target.flipX, target.flipY);
        }
    }

    public void Initialize(Shop shop)
    {
        Unsubscribe();
        _shop = shop;

        if (_shop != null)
        {
            _shop.Changed += Apply;
        }

        Apply();
    }

    private void Start()
    {
        if (_shop != null)
        {
            return;
        }

        Initialize(FindObjectOfType<Shop>());
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (_shop != null)
        {
            _shop.Changed -= Apply;
            _shop = null;
        }
    }

    private void Apply()
    {
        Theme theme = ThemeResolver.GetEquipped(_catalog);
        if (theme == null)
        {
            return;
        }

        ApplyBackground(theme);
        ApplyStations(theme);
        ApplyPlayArea(theme);
        ApplyGrid(theme);
        ApplyGradient(theme);
        ApplyUIColors(theme);
    }

    private void ApplyGradient(Theme theme)
    {
        if (_gradientTarget == null)
        {
            return;
        }

        _gradientTarget.enabled = theme.GradientActive;
        _gradientTarget.color = theme.GradientColor;
    }

    private void ApplyBackground(Theme theme)
    {
        if (_backgroundTarget == null)
        {
            return;
        }

        _backgroundTarget.drawMode = SpriteDrawMode.Tiled;
        _backgroundTarget.sprite = theme.BackgroundSprite;
        _backgroundTarget.size = theme.BackgroundTileSize;
        _backgroundTarget.transform.localScale = theme.BackgroundSize;
        _backgroundBaseSize = theme.BackgroundTileSize;
        FitBackgroundToCamera(Camera.main);
    }

    // Extend the tiled background instead of stretching the artwork or exposing
    // the camera clear color above/below the board on tall browser viewports.
    public void FitBackgroundToCamera(Camera camera)
    {
        if (_backgroundTarget == null || camera == null || !camera.orthographic) return;
        Transform background = _backgroundTarget.transform;
        var plane = new Plane(background.forward, background.position);
        Vector2 size = _backgroundBaseSize;
        for (int corner = 0; corner < 4; corner++)
        {
            Ray ray = camera.ViewportPointToRay(new Vector3(corner % 2, corner / 2, 0));
            if (!plane.Raycast(ray, out float distance)) continue;
            Vector3 local = background.InverseTransformPoint(ray.GetPoint(distance));
            size.x = Mathf.Max(size.x, Mathf.Abs(local.x) * 2.02f);
            size.y = Mathf.Max(size.y, Mathf.Abs(local.y) * 2.02f);
        }
        _backgroundTarget.size = size;
    }

    private void ApplyStations(Theme theme)
    {
        foreach (StationEntry entry in _stations)
        {
            if (entry == null || entry.Target == null)
            {
                continue;
            }

            entry.Target.sprite = entry.Sprite == StationSpriteVariant.B
                ? theme.StationSpriteB
                : theme.StationSpriteA;
            entry.Target.flipX = entry.FlipX;
            entry.Target.flipY = entry.FlipY;
            entry.Target.transform.localScale = theme.StationSpriteScale;
        }
    }

    private void ApplyPlayArea(Theme theme)
    {
        if (_playAreaTarget == null)
        {
            return;
        }

        if (!_playAreaBaseCached)
        {
            _playAreaBaseRotation = _playAreaTarget.transform.localRotation;
            _playAreaBaseCached = true;
        }

        _playAreaTarget.sprite = theme.PlayAreaSprite;
        _playAreaTarget.transform.localScale = theme.PlayAreaSize;
        _playAreaTarget.transform.localRotation =
            _playAreaBaseRotation * Quaternion.Euler(0f, 0f, theme.PlayAreaRotationZ);
    }

    private void ApplyGrid(Theme theme)
    {
        if (_gridTarget == null)
        {
            return;
        }

        _gridTarget.drawMode = SpriteDrawMode.Tiled;
        _gridTarget.sprite = theme.GridSprite;
        _gridTarget.size = theme.GridTileSize;
        _gridTarget.color = theme.GridColor;
    }

    private void ApplyUIColors(Theme theme)
    {
        SetImageColors(_uiBgPrimaryTargets, theme.BgPrimary);
        SetImageColors(_uiBgSecondaryTargets, theme.BgSecondary);
        SetImageColors(_uiBgContourTargets, theme.BgContour);
        SetImageColors(_uiBgShadowTargets, theme.BgShadow);

        SetTextColors(_uiTextPrimaryTargets, theme.TextPrimary);
        SetTextColors(_uiTextSecondaryTargets, theme.TextSecondary);
        SetTextColors(_uiTextAdditionalTargets, theme.TextAdditional);
    }

    private static void SetImageColors(List<Image> images, Color color)
    {
        foreach (Image image in images)
        {
            if (image != null)
            {
                image.color = color;
            }
        }
    }

    private static void SetTextColors(List<TMP_Text> texts, Color color)
    {
        foreach (TMP_Text text in texts)
        {
            if (text != null)
            {
                text.color = color;
            }
        }
    }
}
