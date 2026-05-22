using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThemeApplier : MonoBehaviour
{
    [Serializable]
    public class StationEntry
    {
        public SpriteRenderer Target;
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
    [SerializeField] private float _playAreaRotationZ;

    [Header("Grid")]
    [SerializeField] private SpriteRenderer _gridTarget;

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
        ApplyUIColors(theme);
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
    }

    private void ApplyStations(Theme theme)
    {
        foreach (StationEntry entry in _stations)
        {
            if (entry == null || entry.Target == null)
            {
                continue;
            }

            entry.Target.sprite = theme.StationSprite;
            entry.Target.flipX = entry.FlipX;
            entry.Target.flipY = entry.FlipY;
        }
    }

    private void ApplyPlayArea(Theme theme)
    {
        if (_playAreaTarget == null)
        {
            return;
        }

        _playAreaTarget.sprite = theme.PlayAreaSprite;
        _playAreaTarget.transform.localScale = theme.PlayAreaSize;

        Vector3 euler = _playAreaTarget.transform.localEulerAngles;
        euler.z = _playAreaRotationZ;
        _playAreaTarget.transform.localEulerAngles = euler;
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
