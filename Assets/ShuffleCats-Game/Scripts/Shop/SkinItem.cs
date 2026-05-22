using UnityEngine;

[CreateAssetMenu(fileName = "SkinItem", menuName = "ShuffleCats/Shop/Skin Item")]
public class SkinItem : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField] private int _price;
    [SerializeField] private Sprite _slotIcon;
    [SerializeField] private Sprite _appliedSprite;
    [SerializeField] private Color _tint = Color.white;
    [SerializeField] private bool _ownedByDefault;
    [SerializeField] private bool _equippedByDefault;

    public string Id => _id;
    public string DisplayName => _displayName;
    public int Price => _price;
    public Sprite SlotIcon => _slotIcon;
    public Sprite AppliedSprite => _appliedSprite;
    public Color Tint => _tint;
    public bool OwnedByDefault => _ownedByDefault;
    public bool EquippedByDefault => _equippedByDefault;
}
