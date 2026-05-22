using System;
using UnityEngine;

public class Shop : MonoBehaviour
{
    [SerializeField] private ShopCatalog _catalog;

    private IShopRepository _repository;
    private IWalletSpender _wallet;

    public event Action Changed;

    public ShopCatalog Catalog => _catalog;
    public IWalletSpender Wallet => _wallet;

    private void Awake()
    {
        _repository = new ShopRepository(_catalog);
        _wallet = FindObjectOfType<Wallet>();
    }

    public SlotStatus GetStatus(SkinItem item)
    {
        if (item == null || _catalog == null)
        {
            return SlotStatus.Locked;
        }

        if (!_catalog.TryGetCategoryOf(item, out SkinCategory category))
        {
            return SlotStatus.Locked;
        }

        if (_repository.GetEquipped(category) == item.Id)
        {
            return SlotStatus.Equipped;
        }

        if (_repository.IsOwned(item.Id))
        {
            return SlotStatus.Owned;
        }

        return SlotStatus.Locked;
    }

    public bool TryBuy(SkinItem item)
    {
        if (item == null || _wallet == null)
        {
            return false;
        }

        if (_repository.IsOwned(item.Id))
        {
            return false;
        }

        if (!_wallet.TrySpend(item.Price))
        {
            return false;
        }

        _repository.MarkOwned(item.Id);
        Changed?.Invoke();
        return true;
    }

    public void Equip(SkinItem item)
    {
        if (item == null || _catalog == null)
        {
            return;
        }

        if (!_repository.IsOwned(item.Id))
        {
            return;
        }

        if (!_catalog.TryGetCategoryOf(item, out SkinCategory category))
        {
            return;
        }

        _repository.SetEquipped(category, item.Id);
        Changed?.Invoke();
    }
}
