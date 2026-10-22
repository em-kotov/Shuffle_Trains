using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteRendererSkinApplier : MonoBehaviour
{
    [SerializeField] private ShopCatalog _catalog;
    [SerializeField] private SkinCategory _category;
    [SerializeField] private SpriteRenderer _target;

    private Shop _shop;

    private void Reset()
    {
        _target = GetComponent<SpriteRenderer>();
    }

    public void Initialize(Shop shop)
    {
        if (_target == null)
        {
            _target = GetComponent<SpriteRenderer>();
        }

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
        SkinItem equipped = SkinResolver.GetEquipped(_catalog, _category);

        if (equipped == null)
        {
            return;
        }

        if (equipped.AppliedSprite != null)
        {
            _target.sprite = equipped.AppliedSprite;
        }

        _target.color = equipped.Tint;
    }
}
