using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteRendererSkinApplier : MonoBehaviour
{
    // Preserve serialized data on legacy scene/prefab components; ThemeApplier now handles skins.
    [SerializeField] private ShopCatalog _catalog;
    [SerializeField] private SkinCategory _category;
    [SerializeField] private SpriteRenderer _target;

    private void Reset()
    {
        _target = GetComponent<SpriteRenderer>();
    }
}
