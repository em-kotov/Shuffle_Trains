using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CatPlacement
{
    [SerializeField] private CatColor _color = CatColor.Blue;
    [SerializeField] private int _length = 1;
    [SerializeField] private Vector2Int _cell;
    [SerializeField] private Vector3 _eulerAngles;
    [SerializeField] private CarOrientation _orientation = CarOrientation.Horizontal;
    [SerializeField] private float _signDirection = 1f;
    [SerializeField] private bool _invertFlip;
    [SerializeField] private bool _trackFlipSeparate;
    [SerializeField] private bool _invertFlipTrack;
    [SerializeField] private bool _applyRotationFix;
    [SerializeField] private bool _rotateFixLeft;
    [SerializeField] private List<CatColor> _passengers = new();

    public CatColor Color => _color;
    public int Length => _length;
    public Vector2Int Cell => _cell;
    public Vector3 EulerAngles => _eulerAngles;
    public CarOrientation Orientation => _orientation;
    public float SignDirection => _signDirection;
    public bool InvertFlip => _invertFlip;
    public bool TrackFlipSeparate => _trackFlipSeparate;
    public bool InvertFlipTrack => _invertFlipTrack;
    public bool ApplyRotationFix => _applyRotationFix;
    public bool RotateFixLeft => _rotateFixLeft;
    public List<CatColor> Passengers => _passengers;

    public CatPlacement() { }

    public CatPlacement(CatColor color, int length, Vector2Int cell, Vector3 eulerAngles,
                        CarOrientation orientation, float signDirection,
                        bool invertFlip, bool trackFlipSeparate, bool invertFlipTrack,
                        bool applyRotationFix, bool rotateFixLeft,
                        List<CatColor> passengers)
    {
        _color = color;
        _length = length;
        _cell = cell;
        _eulerAngles = eulerAngles;
        _orientation = orientation;
        _signDirection = signDirection;
        _invertFlip = invertFlip;
        _trackFlipSeparate = trackFlipSeparate;
        _invertFlipTrack = invertFlipTrack;
        _applyRotationFix = applyRotationFix;
        _rotateFixLeft = rotateFixLeft;
        _passengers = passengers;
    }
}
