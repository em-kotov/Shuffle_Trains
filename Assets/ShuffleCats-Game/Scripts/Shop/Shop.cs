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

    public SlotStatus GetStatus(Theme theme)
    {
        if (theme == null)
        {
            return SlotStatus.Locked;
        }

        if (_repository.GetEquipped() == theme.Id)
        {
            return SlotStatus.Equipped;
        }

        if (_repository.IsOwned(theme.Id))
        {
            return SlotStatus.Owned;
        }

        return SlotStatus.Locked;
    }

    public bool TryBuy(Theme theme)
    {
        if (theme == null || _wallet == null)
        {
            return false;
        }

        if (_repository.IsOwned(theme.Id))
        {
            return false;
        }

        if (!_wallet.TrySpend(theme.Price))
        {
            return false;
        }

        _repository.MarkOwned(theme.Id);
        Changed?.Invoke();
        return true;
    }

    public void Equip(Theme theme)
    {
        if (theme == null || !_repository.IsOwned(theme.Id))
        {
            return;
        }

        _repository.SetEquipped(theme.Id);
        Changed?.Invoke();
    }
}
