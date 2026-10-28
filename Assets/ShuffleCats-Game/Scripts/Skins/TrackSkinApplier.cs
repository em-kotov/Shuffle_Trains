using UnityEngine;

public class TrackSkinApplier : MonoBehaviour
{
    // Preserve serialized data on legacy scene/prefab components; ThemeApplier now handles skins.
    [SerializeField] private ShopCatalog _catalog;
    [SerializeField] private Material _trackMaterial;
}
