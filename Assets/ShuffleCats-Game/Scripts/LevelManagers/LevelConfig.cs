using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "ShuffleCats/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Level")]
    [SerializeField] private int _levelNumber = 1;
    [SerializeField] private int _levelWinBonus = 100;
    [SerializeField] private int _levelCarBonus = 10;

    [Header("Parking Grid")]
    [SerializeField] private int _gridWidth = 4;
    [SerializeField] private int _gridHeight = 4;
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private int _startX = 0;
    [SerializeField] private int _startY = 0;
    [SerializeField] private float _axisYLevel = 0f;

    [Header("Track")]
    [SerializeField] private int _maxCarsOnTrackCount = 6;
    [SerializeField] private float _waitTime = 0.3f;

    [Header("Cars")]
    [SerializeField] private float _trackSpeed = 16f;
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private float _searchMin = 0f;
    [SerializeField] private float _searchMax = 1f;

    [Header("Layout")]
    [SerializeField] private List<CatPlacement> _cats = new();
    [SerializeField] private List<StationPlacement> _stations = new();

    public int LevelNumber => _levelNumber;
    public int LevelWinBonus => _levelWinBonus;
    public int LevelCarBonus => _levelCarBonus;

    public int GridWidth => _gridWidth;
    public int GridHeight => _gridHeight;
    public float CellSize => _cellSize;
    public int StartX => _startX;
    public int StartY => _startY;
    public float AxisYLevel => _axisYLevel;

    public int MaxCarsOnTrackCount => _maxCarsOnTrackCount;
    public float WaitTime => _waitTime;

    public float TrackSpeed => _trackSpeed;
    public float SlideDuration => _slideDuration;
    public float SearchMin => _searchMin;
    public float SearchMax => _searchMax;

    public IReadOnlyList<CatPlacement> Cats => _cats;
    public IReadOnlyList<StationPlacement> Stations => _stations;

    // The runtime passes this to each car so the sorter knows how many
    // stations exist this level. It is always the station count.
    public int StationCount => _stations.Count;

    public Vector3 CellToWorld(Vector2Int cell)
    {
        float worldX = (cell.x * _cellSize) + _startX;
        float worldZ = (cell.y * _cellSize) + _startY;
        return new Vector3(worldX, _axisYLevel, worldZ);
    }

#if UNITY_EDITOR
    // Used only by the editor capture tool to author a config from a scene.
    public void EditorSetGrid(int width, int height, float cellSize,
                            int startX, int startY, float axisYLevel)
    {
        _gridWidth = width;
        _gridHeight = height;
        _cellSize = cellSize;
        _startX = startX;
        _startY = startY;
        _axisYLevel = axisYLevel;
    }

    public void EditorSetLevel(int levelNumber, int winBonus, int carBonus)
    {
        _levelNumber = levelNumber;
        _levelWinBonus = winBonus;
        _levelCarBonus = carBonus;
    }

    public void EditorSetTrack(int maxCarsOnTrack, float waitTime)
    {
        _maxCarsOnTrackCount = maxCarsOnTrack;
        _waitTime = waitTime;
    }

    public void EditorSetCars(float trackSpeed, float slideDuration,
                            float searchMin, float searchMax)
    {
        _trackSpeed = trackSpeed;
        _slideDuration = slideDuration;
        _searchMin = searchMin;
        _searchMax = searchMax;
    }

    public void EditorSetLayout(List<CatPlacement> cats, List<StationPlacement> stations)
    {
        _cats = cats;
        _stations = stations;
    }
#endif
}

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

[System.Serializable]
public class StationPlacement
{
    [SerializeField] private Vector3 _position;
    [SerializeField] private Vector3 _eulerAngles;

    [Header("Theme")]
    [SerializeField] private ThemeApplier.StationSpriteVariant _spriteVariant;
    [SerializeField] private bool _flipX;
    [SerializeField] private bool _flipY;

    public Vector3 Position => _position;
    public Vector3 EulerAngles => _eulerAngles;
    public ThemeApplier.StationSpriteVariant SpriteVariant => _spriteVariant;
    public bool FlipX => _flipX;
    public bool FlipY => _flipY;

    public StationPlacement() { }

    public StationPlacement(Vector3 position, Vector3 eulerAngles,
                        ThemeApplier.StationSpriteVariant spriteVariant,
                        bool flipX, bool flipY)
    {
        _position = position;
        _eulerAngles = eulerAngles;
        _spriteVariant = spriteVariant;
        _flipX = flipX;
        _flipY = flipY;
    }
}
