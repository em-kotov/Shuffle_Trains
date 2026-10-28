using UnityEngine;

[System.Serializable]
public class StationPlacement
{
    [SerializeField] private Vector3 _position;
    [SerializeField] private Vector3 _eulerAngles;

    [Header("Theme")]
    [SerializeField] private ThemeApplier.StationSpriteVariant _spriteVariant;
    [SerializeField] private bool _flipX;
    [SerializeField] private bool _flipY;

    public StationPlacement()
    {
    }

    public StationPlacement(
        Vector3 position,
        Vector3 eulerAngles,
        ThemeApplier.StationSpriteVariant spriteVariant,
        bool flipX,
        bool flipY)
    {
        _position = position;
        _eulerAngles = eulerAngles;
        _spriteVariant = spriteVariant;
        _flipX = flipX;
        _flipY = flipY;
    }

    public Vector3 Position => _position;

    public Vector3 EulerAngles => _eulerAngles;

    public ThemeApplier.StationSpriteVariant SpriteVariant => _spriteVariant;

    public bool FlipX => _flipX;

    public bool FlipY => _flipY;
}
