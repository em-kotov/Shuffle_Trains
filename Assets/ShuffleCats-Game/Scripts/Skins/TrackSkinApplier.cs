using UnityEngine;

public class TrackSkinApplier : MonoBehaviour
{
    [SerializeField] private ShopCatalog _catalog;
    [SerializeField] private Material _trackMaterial;

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
        if (_trackMaterial == null)
        {
            return;
        }

        SkinItem equipped = SkinResolver.GetEquipped(_catalog, SkinCategory.Track);
        if (equipped == null || equipped.AppliedSprite == null)
        {
            return;
        }

        _trackMaterial.mainTexture = equipped.AppliedSprite.texture;
    }
}
