using UnityEngine;

[CreateAssetMenu(fileName = "Theme", menuName = "ShuffleCats/Shop/Theme")]
public class Theme : ScriptableObject
{
    [Header("Catalog")]
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField] private int _price;
    [SerializeField] private Sprite _slotIcon;
    [SerializeField] private bool _ownedByDefault;
    [SerializeField] private bool _equippedByDefault;

    [Header("Background")]
    [SerializeField] private Sprite _backgroundSprite;
    [SerializeField] private Vector3 _backgroundSize = Vector3.one;
    [SerializeField] private Vector2 _backgroundTileSize = Vector2.one;

    [Header("Stations")]
    [SerializeField] private Sprite _stationSpriteA;
    [SerializeField] private Sprite _stationSpriteB;
    [SerializeField] private Vector3 _stationSpriteScale = Vector3.one;

    [Header("Play Area")]
    [SerializeField] private Sprite _playAreaSprite;
    [SerializeField] private Vector3 _playAreaSize = Vector3.one;
    [SerializeField] private float _playAreaRotationZ;

    [Header("Grid")]
    [SerializeField] private Sprite _gridSprite;
    [SerializeField] private Vector2 _gridTileSize = Vector2.one;
    [SerializeField] private Color _gridColor = Color.white;

    [Header("Gradient")]
    [SerializeField] private bool _gradientActive;
    [SerializeField] private Color _gradientColor = Color.white;

    [Header("UI Background Colors")]
    [SerializeField] private Color _bgPrimary = Color.white;
    [SerializeField] private Color _bgSecondary = Color.white;
    [SerializeField] private Color _bgContour = Color.black;
    [SerializeField] private Color _bgShadow = Color.gray;

    [Header("UI Text Colors")]
    [SerializeField] private Color _textPrimary = Color.black;
    [SerializeField] private Color _textSecondary = Color.gray;
    [SerializeField] private Color _textAdditional = Color.white;

    public string Id => _id;

    public string DisplayName => _displayName;

    public int Price => _price;

    public Sprite SlotIcon => _slotIcon;

    public bool OwnedByDefault => _ownedByDefault;

    public bool EquippedByDefault => _equippedByDefault;

    public Sprite BackgroundSprite => _backgroundSprite;

    public Vector3 BackgroundSize => _backgroundSize;

    public Vector2 BackgroundTileSize => _backgroundTileSize;

    public Sprite StationSpriteA => _stationSpriteA;

    public Sprite StationSpriteB => _stationSpriteB;

    public Vector3 StationSpriteScale => _stationSpriteScale;

    public Sprite PlayAreaSprite => _playAreaSprite;

    public Vector3 PlayAreaSize => _playAreaSize;

    public float PlayAreaRotationZ => _playAreaRotationZ;

    public Sprite GridSprite => _gridSprite;

    public Vector2 GridTileSize => _gridTileSize;

    public Color GridColor => _gridColor;

    public bool GradientActive => _gradientActive;

    public Color GradientColor => _gradientColor;

    public Color BgPrimary => _bgPrimary;

    public Color BgSecondary => _bgSecondary;

    public Color BgContour => _bgContour;

    public Color BgShadow => _bgShadow;

    public Color TextPrimary => _textPrimary;

    public Color TextSecondary => _textSecondary;

    public Color TextAdditional => _textAdditional;
}
